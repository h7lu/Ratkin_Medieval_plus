using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RkM
{
    public class CompProperties_RitualStarter : CompProperties
    {
        public string ritualDefName;

        public CompProperties_RitualStarter()
        {
            compClass = typeof(CompRitualStarter);
        }
    }

    public class CompRitualStarter : ThingComp
    {
        public CompProperties_RitualStarter Props => (CompProperties_RitualStarter)props;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            // Always show the normal ritual options - ability check will be in dialog
            foreach (var option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // Always show the gizmo enabled
            var command = new Command_Action();
            command.defaultLabel = "Start lecture";
            command.defaultDesc = "Begin the teaching ritual at this lectern.";
            // Use the lectern's icon as a safe fallback.
            command.icon = parent.def.uiIcon;
            command.action = () => TryOpenBeginRitualDialog();
            
            yield return command;
        }

        private bool CanPawnTeach(Pawn pawn)
        {
            if (pawn.skills == null)
                return false;

            // Check social skill (same requirement as RitualBehaviorWorker)
            var social = pawn.skills.GetSkill(SkillDefOf.Social);
            if (social == null || social.Level < 8)
                return false;

            // Check for at least one other skill >= 8 (same requirement as RitualBehaviorWorker)
            bool hasOtherSkill = false;
            foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
            {
                if (skillDef == SkillDefOf.Social) continue;
                
                var skill = pawn.skills.GetSkill(skillDef);
                if (skill != null && skill.Level >= 8)
                {
                    hasOtherSkill = true;
                    break;
                }
            }

            return hasOtherSkill;
        }

        private void TryOpenBeginRitualDialog()
        {
            if (parent == null || parent.Map == null)
            {
                return;
            }
            var map = parent.Map;

            // Try to find the ritual precept on the player's primary ideo"[RkM] Opening ritual dialog from building for {selectedPawn.LabelShort} at lectern {parent.Position}");
            // Try to find the ritual precept on the player's primary ideo
            Precept_Ritual ritual = null;
            if (!Props.ritualDefName.NullOrEmpty())
            {
                ritual = Faction.OfPlayer.ideos.PrimaryIdeo?.PreceptsListForReading.OfType<Precept_Ritual>().FirstOrDefault(p => p.def.defName == Props.ritualDefName);
            }
            // Fallback: first ritual precept on the primary ideo
            if (ritual == null)
            {
                ritual = Faction.OfPlayer.ideos.PrimaryIdeo?.PreceptsListForReading.OfType<Precept_Ritual>().FirstOrDefault();
            }

            if (ritual == null)
            {
                Messages.Message("No teaching ritual precept found on the player's primary Ideo.", MessageTypeDefOf.RejectInput);
                
                // Try to create a temporary ritual from the def directly
                var ritualDef = DefDatabase<PreceptDef>.GetNamed(Props.ritualDefName, false);
                if (ritualDef != null)
                {
                    Log.Message("[RkM] Cannot create temporary ritual from def - Precept_Ritual constructor not available");
                    Messages.Message("Teaching ritual precept not found. Please add it to your ideoligion.", MessageTypeDefOf.RejectInput);
                    return;
                }
                else
                {
                    Log.Error("[RkM] Could not find ritual def in building either");
                    return;
                }
            }

            TargetInfo target = new TargetInfo(parent.Position, map, false);

            Dialog_BeginRitual.ActionCallback action = (RitualRoleAssignments assignments) =>
            {
                try
                {
                    // Check if assigned teacher meets skill requirements
                    var teacherPawn = assignments.FirstAssignedPawn("teacher");
                    if (teacherPawn != null)
                    {
                        // Apply quality factor based on teacher's skills
                        if (!CanPawnTeach(teacherPawn))
                        {
                            Messages.Message($"{teacherPawn.LabelShort} cannot teach - requires Social 8+ and another skill 8+.", MessageTypeDefOf.RejectInput);
                            return false;
                        }
                    }

                    // Try to execute via the ritual behavior worker. Use playerForced=true to bypass AI checks.
                    ritual.behavior.TryExecuteOn(target, null, ritual, null, assignments, playerForced: true);
                }
                catch (Exception ex)
                {
                    Log.Error("Error while starting ritual from lectern gizmo: " + ex);
                    return false;
                }
                return true;
            };

            
            // Ensure the dialog has an outcomeEffect definition even if the Precept_Ritual instance
            // doesn't have its outcomeEffect instantiated yet. Pass the RitualPatternDef's
            // ritualOutcomeEffect Def as a fallback to the Dialog constructor (it will use
            // ritual.outcomeEffect.def ?? outcome).
            var outcomeDefFallback = ritual?.def?.ritualPatternBase?.ritualOutcomeEffect;
            Find.WindowStack.Add(new Dialog_BeginRitual(ritual.Label, ritual, target, map, action, null, null, null, null, null, null, outcomeDefFallback));
        }
    }
}
