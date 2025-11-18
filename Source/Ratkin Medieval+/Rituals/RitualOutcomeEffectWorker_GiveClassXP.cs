using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace RkM
{
    /// <summary>
    /// Ritual outcome worker that gives (or removes) class XP depending on the chosen outcome.
    /// Uses RitualOutcomeEffectWorker_FromQuality to obtain the concrete outcome possibility.
    /// </summary>
    public class RitualOutcomeEffectWorker_GiveClassXP : RitualOutcomeEffectWorker_FromQuality
    {
        // Parameterless ctor required for def/serializer to instantiate the worker when loading saved games / defs.
        public RitualOutcomeEffectWorker_GiveClassXP()
        {
        }

        public RitualOutcomeEffectWorker_GiveClassXP(RitualOutcomeEffectDef def) : base(def)
        {
        }

        // Use vanilla OutcomeQualityBreakdownDesc - attendance is handled by RitualOutcomeComp_Attendance
        public override string OutcomeQualityBreakdownDesc(float quality, float progress, LordJob_Ritual jobRitual)
        {
            return base.OutcomeQualityBreakdownDesc(quality, progress, jobRitual);
        }

        // Called by the base after an outcome is selected. We apply XP/inspiration according to
        // the outcome label (terrible, boring, fun, unforgettable) and configured props.
        protected override void ApplyExtraOutcome(Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual, RitualOutcomePossibility outcome, out string extraOutcomeDesc, ref LookTargets letterLookTargets)
        {
            extraOutcomeDesc = null;
            var props = def.GetModExtension<RitualGiveClassXPProps>();
            if (props == null)
            {
                Log.Error("RitualGiveClassXPProps not found on ritual outcome def " + def?.defName);
                return;
            }

            // teacher role (if present). Accept common role ids used by speech/lectern rituals
            string[] teacherRoleIds = new[] { "teacher", "leader", "speaker" };
            Pawn teacher = null;
            if (jobRitual?.assignments != null)
            {
                foreach (var id in teacherRoleIds)
                {
                    var p = jobRitual.assignments.FirstAssignedPawn(id);
                    if (p != null)
                    {
                        teacher = p;
                        break;
                    }
                }
            }

            // Determine the teaching theme skill: prefer teacher's highest non-social skill; fallback to props.skill
            SkillDef teachingSkillDef = props.skill;
            int teacherSkillLevel = 0;
            try
            {
                if (teacher != null)
                {
                    var highest = teacher.skills?.skills.Where(s => s.def != SkillDefOf.Social).MaxBy(s => s.Level);
                    if (highest != null && highest.def != null)
                    {
                        teachingSkillDef = highest.def;
                        teacherSkillLevel = highest.Level;
                    }
                }
            }
            catch { }

            // No XP bonus from blackboards; they only influence quality comps now.
            bool blackboardNearby = false;

            string outcomeLabel = outcome?.label?.ToLower() ?? string.Empty;

            // Choose a player-facing message for this outcome. This will be returned via extraOutcomeDesc.
            if (outcomeLabel.Contains("terrible") || (outcome.positivityIndex < 0 && outcomeLabel.Contains("terrible")))
            {
                extraOutcomeDesc = "Audience members were discouraged by the lesson; it damaged learning for many.";
            }
            else if (outcomeLabel.Contains("boring") || (outcome.positivityIndex < 0 && !outcomeLabel.Contains("terrible")))
            {
                extraOutcomeDesc = "The lesson was boring; participants gained little and some felt worse.";
            }
            else if (outcomeLabel.Contains("fun") || (outcome.positivityIndex >= 0 && outcomeLabel.Contains("fun")))
            {
                extraOutcomeDesc = "Participants enjoyed the lesson and picked up useful knowledge.";
            }
            else if (outcomeLabel.Contains("unforgettable") || outcome.positivityIndex > 0)
            {
                extraOutcomeDesc = "The class was unforgettable: participants learned a lot and some may feel inspired.";
            }
            else
            {
                extraOutcomeDesc = null;
            }

            foreach (var kv in totalPresence)
            {
                var pawn = kv.Key;
                if (pawn == null || pawn.Destroyed) continue;

                var role = jobRitual?.assignments?.RoleForPawn(pawn);
                bool isTeacher = false;
                if (role != null)
                {
                    // treat leader/speaker as equivalent to teacher for XP purposes
                    isTeacher = teacherRoleIds.Contains(role.id);
                }
                float roleMult = isTeacher ? props.teacherMultiplier : props.studentMultiplier;

                float baseAmount = props.baseXp * roleMult;

                try
                {
                    // Determine amount to Learn (positive to gain XP, negative to lose)
                    float amount = 0f;
                    bool apply = true; // whether to call Learn on the skill

                    if (outcomeLabel.Contains("terrible") || (outcome.positivityIndex < 0 && outcomeLabel.Contains("terrible")))
                    {
                        if (!isTeacher)
                            amount = -Math.Abs(baseAmount * props.terribleLossFraction);
                        else
                            apply = false;
                    }
                    else if (outcomeLabel.Contains("boring") || (outcome.positivityIndex < 0 && !outcomeLabel.Contains("terrible")))
                    {
                        if (!isTeacher)
                            amount = -Math.Abs(baseAmount * props.boringLossFraction);
                        else
                            apply = false;
                    }
                    else if (outcomeLabel.Contains("fun") || (outcome.positivityIndex >= 0 && outcomeLabel.Contains("fun")))
                    {
                        amount = baseAmount * props.funMultiplier;
                    }
                    else if (outcomeLabel.Contains("unforgettable") || outcome.positivityIndex > 0)
                    {
                        amount = baseAmount * props.unforgettableMultiplier;
                    }
                    else
                    {
                        amount = baseAmount * 0.5f;
                    }

                    // Debug per-pawn info to verify XP awarding (attendance affects quality only, not XP)
                    try
                    {
                        Log.Message($"[RkM] Awarding XP to {pawn.NameShortColored}: skill={teachingSkillDef?.defName ?? "<null>"} roleMult={roleMult} baseXp={props.baseXp} blackboardNearby={blackboardNearby} amount={amount:F2}");
                    }
                    catch { }

                    if (apply)
                    {
                        var skillRec = pawn.skills?.GetSkill(teachingSkillDef);
                        if (skillRec == null)
                        {
                            Log.Message($"[RkM] SkillRecord NULL for {pawn.NameShortColored} skill={teachingSkillDef?.defName}");
                        }
                        else
                        {
                            // Check if student has higher skill than teacher (strictly greater)
                            if (!isTeacher && skillRec.Level > teacherSkillLevel)
                            {
                                Log.Message($"[RkM] {pawn.NameShortColored} has higher skill ({skillRec.Level}) than teacher ({teacherSkillLevel}) - no XP awarded");
                                continue; // Skip XP for this student
                            }
                            
                            int beforeLvl = skillRec.Level;
                            skillRec.Learn(amount);
                            int afterLvl = skillRec.Level;
                            Log.Message($"[RkM] {pawn.NameShortColored} skill {teachingSkillDef?.defName} level {beforeLvl} -> {afterLvl}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Error giving class XP in RkM ritual worker: " + ex, 172634);
                }
            }
        }

        // Simple line-of-sight sampling helper used if needed later by logic (kept inside class)
        private bool HasClearLineOfSight(IntVec3 from, IntVec3 to, Map map)
        {
            if (map == null) return true;
            int dx = to.x - from.x;
            int dz = to.z - from.z;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dz));
            if (steps <= 1) return true;
            for (int i = 1; i < steps; i++)
            {
                int x = from.x + (dx * i) / steps;
                int z = from.z + (dz * i) / steps;
                IntVec3 c = new IntVec3(x, 0, z);
                var ed = c.GetEdifice(map);
                if (ed != null && ed.def.passability == Traversability.Impassable)
                    return false;
            }
            return true;
        }

        // Use vanilla Apply method - attendance is handled by RitualOutcomeComp_Attendance
        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            base.Apply(progress, totalPresence, jobRitual);
        }
    }
}
