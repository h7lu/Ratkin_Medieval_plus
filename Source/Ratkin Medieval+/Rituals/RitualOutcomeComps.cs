using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace RkM
{
    internal static class BlackboardLinkUtility
    {
        internal static int CountLinkedBlackboards(LordJob_Ritual ritualJob, RitualOutcomeEffectDef effectDef, Map map, IntVec3 spot, Thing targetThing)
        {
            try
            {
                if (map == null || !spot.IsValid)
                {
                    return 0;
                }

                var props = effectDef?.GetModExtension<RitualGiveClassXPProps>();
                var allowed = props?.blackboardThingDefs;
                if (allowed == null || allowed.Count == 0)
                {
                    return 0;
                }

                CompAffectedByFacilities compAffected = targetThing?.TryGetComp<CompAffectedByFacilities>();
                if (compAffected == null)
                {
                    var building = map.thingGrid.ThingAt(spot, ThingCategory.Building) as Building;
                    compAffected = building?.TryGetComp<CompAffectedByFacilities>();
                }

                int count = 0;
                if (compAffected != null)
                {
                    foreach (var facility in compAffected.LinkedFacilitiesListForReading)
                    {
                        if (facility != null && allowed.Contains(facility.def.defName))
                        {
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        return count;
                    }
                }

                // fallback radial scan (legacy)
                foreach (var cell in GenRadial.RadialCellsAround(spot, 2f, true))
                {
                    foreach (var thing in cell.GetThingList(map))
                    {
                        if (allowed.Contains(thing.def.defName))
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        internal static bool HasLinkedBlackboard(LordJob_Ritual job, RitualGiveClassXPProps props)
        {
            if (job?.Map == null || props?.blackboardThingDefs == null)
            {
                return false;
            }
            var targetThing = job.selectedTarget.Thing;
            CompAffectedByFacilities comp = targetThing?.TryGetComp<CompAffectedByFacilities>();
            if (comp == null)
            {
                var building = job.Map.thingGrid.ThingAt(job.Spot, ThingCategory.Building) as Building;
                comp = building?.TryGetComp<CompAffectedByFacilities>();
            }
            if (comp != null)
            {
                foreach (var facility in comp.LinkedFacilitiesListForReading)
                {
                    if (facility != null && props.blackboardThingDefs.Contains(facility.def.defName))
                    {
                        return true;
                    }
                }
            }
            foreach (var cell in GenRadial.RadialCellsAround(job.Spot, 1f, true))
            {
                foreach (var thing in cell.GetThingList(job.Map))
                {
                    if (props.blackboardThingDefs.Contains(thing.def.defName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static HashSet<Thing> GetLinkedSeatThings(Map map, IntVec3 spot)
        {
            var result = new HashSet<Thing>();
            if (map == null || !spot.IsValid)
            {
                return result;
            }
            var building = map.thingGrid.ThingAt(spot, ThingCategory.Building) as Building;
            var comp = building?.TryGetComp<CompAffectedByFacilities>();
            if (comp != null)
            {
                foreach (var facility in comp.LinkedFacilitiesListForReading)
                {
                    if (facility != null && facility.def.building?.isSittable == true)
                    {
                        result.Add(facility);
                    }
                }
            }
            return result;
        }
    }

    // Small set of RitualOutcomeComp implementations to show quality factors for
    // podium/blackboard presence, room beauty, attendance and teacher skill.

    // Podium comp removed: rituals start at a lectern/lectern spot by design, no need to recheck for podium presence.

    public class RitualOutcomeComp_Blackboard : RitualOutcomeComp_Quality
    {
        public override bool Applies(LordJob_Ritual ritual) => true;

    public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            try
            {
        var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();    
                if (props == null) return null;
        int foundCount = BlackboardLinkUtility.CountLinkedBlackboards(null, ritual?.outcomeEffect?.def, ritualTarget.Map, ritualTarget.Cell, ritualTarget.Thing);
                // Cap to props.blackboardMaxCount for maximum boost
                int maxBb = props?.blackboardMaxCount ?? 3;
                float perBb = props?.blackboardPerCount ?? 0.05f;
                int cap = Math.Min(foundCount, maxBb);
                // each blackboard: perBb
                float quality = cap * perBb;
                return new QualityFactor
                {
                    label = "Blackboards",
                    count = $"{foundCount}/{maxBb}",
                    present = foundCount >= maxBb,
                    quality = quality,
                    qualityChange = $"+{(quality * 100f).ToString("F0")}%",
                    positive = foundCount > 0,
                    priority = 3f
                };
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_Blackboard error: " + ex, 472388);
                return null;
            }
        }

    public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                return BlackboardLinkUtility.CountLinkedBlackboards(ritual, ritual?.Ritual?.outcomeEffect?.def, ritual.Map, ritual.Spot, ritual.selectedTarget.Thing);
            }
            catch { return 0; }
        }

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
        try
        {
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                if (props == null || ritual?.Spot.IsValid != true || ritual.Map == null) return 0f;
                int foundCount = BlackboardLinkUtility.CountLinkedBlackboards(ritual, ritual?.Ritual?.outcomeEffect?.def, ritual.Map, ritual.Spot, ritual.selectedTarget.Thing);
                int maxBb = props?.blackboardMaxCount ?? 3;
                float perBb = props?.blackboardPerCount ?? 0.05f;
                int cap = Math.Min(foundCount, maxBb);
                return cap * perBb;
            }
            catch { return 0f; }
        }


        public override string GetDesc(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                Log.Message("[RkM] RitualOutcomeComp_Blackboard GetDesc invoked");
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();    
                if (props == null || ritual?.Spot.IsValid != true || ritual.Map == null) return string.Empty;
                int foundCount = BlackboardLinkUtility.CountLinkedBlackboards(ritual, ritual?.Ritual?.outcomeEffect?.def, ritual.Map, ritual.Spot, ritual.selectedTarget.Thing);
                int maxBb = props?.blackboardMaxCount ?? 3;
                float perBb = props?.blackboardPerCount ?? 0.05f;
                int cap = Math.Min(foundCount, maxBb);
                float quality = cap * perBb;
                return $"Blackboards: {(quality * 100f).ToString("F0")}% ({foundCount}/{maxBb})";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class RitualOutcomeComp_RoomBeauty : RitualOutcomeComp_Quality
    {
        public override bool Applies(LordJob_Ritual ritual) => true;

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            try
            {
                // Use room impressiveness (integral score) instead of average beauty.
                if (ritualTarget.Map == null || !ritualTarget.Cell.IsValid) return null;
                var room = ritualTarget.Cell.GetRoom(ritualTarget.Map);
                if (room == null) return null;
                float impressF = room.GetStat(RoomStatDefOf.Impressiveness);
                int impress = Mathf.RoundToInt(impressF);
                var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();    
                int thresh = props?.impressivenessThreshold ?? 120; // requirement threshold
                float perPoint = props?.impressivenessPerPoint ?? 0.01f;
                float minClamp = props?.impressivenessMinClamp ?? -0.2f;
                float maxClamp = props?.impressivenessMaxClamp ?? 0.5f;
                // Quality offset: perPoint per point above threshold, clamped to bounds
                float q = Mathf.Clamp((impress - thresh) * perPoint, minClamp, maxClamp);
                return new QualityFactor
                {
                    label = "Impressiveness",
                    count = $"{impress}/{thresh}",
                    present = impress >= thresh,
                    quality = q,
                    qualityChange = $"{(q * 100f).ToString("F0")}%",
                    positive = q > 0f,
                    priority = 2f
                };
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_RoomBeauty error: " + ex, 472389);
                return null;
            }
        }

    public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                if (ritual?.Spot.IsValid != true || ritual.Map == null) return 0;
                var room = ritual.Spot.GetRoom(ritual.Map);
                if (room == null) return 0;
                float impressF = room.GetStat(RoomStatDefOf.Impressiveness);
                int impress = Mathf.RoundToInt(impressF);
                return impress;
            }
            catch { return 0; }
        }

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                if (ritual?.Spot.IsValid != true || ritual.Map == null) return 0f;
                var room = ritual.Spot.GetRoom(ritual.Map);
                if (room == null) return 0f;
                float impressF = room.GetStat(RoomStatDefOf.Impressiveness);
                int impress = Mathf.RoundToInt(impressF);
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int thresh = props?.impressivenessThreshold ?? 120;
                float perPoint = props?.impressivenessPerPoint ?? 0.01f;
                float minClamp = props?.impressivenessMinClamp ?? -0.2f;
                float maxClamp = props?.impressivenessMaxClamp ?? 0.5f;
                float q = Mathf.Clamp((impress - thresh) * perPoint, minClamp, maxClamp);
                return q;
            }
            catch { return 0f; }
        }


        public override string GetDesc(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                Log.Message("[RkM] RitualOutcomeComp_RoomBeauty GetDesc invoked");
                if (ritual?.Spot.IsValid != true || ritual.Map == null) return string.Empty;
                var room = ritual.Spot.GetRoom(ritual.Map);
                if (room == null) return string.Empty;
                float impressF = room.GetStat(RoomStatDefOf.Impressiveness);
                int impress = Mathf.RoundToInt(impressF);
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int thresh = props?.impressivenessThreshold ?? 120;
                float perPoint = props?.impressivenessPerPoint ?? 0.01f;
                float minClamp = props?.impressivenessMinClamp ?? -0.2f;
                float maxClamp = props?.impressivenessMaxClamp ?? 0.5f;
                float q = Mathf.Clamp((impress - thresh) * perPoint, minClamp, maxClamp);
                return $"Impressiveness: {(q * 100f).ToString("F0")}% ({impress}/{thresh})";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class RitualOutcomeComp_Attendance : RitualOutcomeComp_Quality
    {
        public override bool Applies(LordJob_Ritual ritual) => true;

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            try
            {
                if (assignments == null) return null;
                int participants = assignments.Participants.Count();
                if (participants <= 0) participants = 1;

                // Required participants for full effectiveness. Kept at 3 for now (matches UI label).
                var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int required = props?.attendanceRequired ?? 3;

                // Get attendance offset from curve (0 attendees = 0, 2 attendees = 0, 10 attendees = -0.4, 20 attendees = -0.5)
                var curve = props?.attendanceOffsetCurve;
                float offset = curve != null ? curve.Evaluate(participants) : 0f;

                return new QualityFactor
                {
                    label = "Attendance",
                    count = $"{participants}/{required}",
                    present = participants >= required,
                    // Attendance now contributes as a quality offset (negative when overcrowded)
                    quality = offset,
                    qualityChange = offset.ToString("P0"),
                    positive = offset >= 0f,
                    priority = 1f
                };
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_Attendance error: " + ex, 472390);
                return null;
            }
        }

    public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                int participants = ritual?.assignments?.Participants?.Count() ?? 1;
                return participants;
            }
            catch { return 1; }
        }

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            // Return the same offset value used in GetQualityFactor for consistency
            try
            {
                int participants = ritual?.assignments?.Participants?.Count() ?? 1;
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int required = props?.attendanceRequired ?? 3;
                var curve = props?.attendanceOffsetCurve;
                float offset = curve != null ? curve.Evaluate(participants) : 0f;
                return offset;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_Attendance QualityOffset error: " + ex, 472391);
                return 0f;
            }
        }

        public override string GetDesc(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                int participants = ritual?.assignments?.Participants?.Count() ?? 1;
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int required = props?.attendanceRequired ?? 3;
                var curve = props?.attendanceOffsetCurve;
                float offset = curve != null ? curve.Evaluate(participants) : 0f;
                return $"Attendance: {offset.ToString("P0")} ({participants}/{required})";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class RitualOutcomeComp_TeacherSkill : RitualOutcomeComp_Quality
    {
        public override bool Applies(LordJob_Ritual ritual) => true;

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            try
            {
                if (assignments == null) return null;
                var teacher = assignments.FirstAssignedPawn("teacher") ?? assignments.FirstAssignedPawn("leader") ?? assignments.FirstAssignedPawn("speaker");
                if (teacher == null) return null;
                var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                // highest skill other than social
                var highest = teacher.skills?.skills.Where(s => s.def != SkillDefOf.Social).MaxBy(s => s.Level);
                if (highest == null) return null;
                int level = highest.Level;
                int req = props?.themeSkillThreshold ?? 8;
                int diff = level - req;
                float pct = Mathf.Max(0, diff) * (props?.themePerLevel ?? 0.05f); // per-level pct from props
                string change = pct > 0f ? $"+{(pct * 100f).ToString("F0")} %" : "-";
                return new QualityFactor
                {
                    label = "Course theme",
                    // show the skill name and level in the count column: e.g. "Art 9/8"
                    count = $"{highest.def.LabelCap} {level}/{req}",
                    present = level >= req,
                    quality = pct,
                    qualityChange = change,
                    positive = level >= req,
                    priority = 4f
                };
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_TeacherSkill error: " + ex, 472391);
                return null;
            }
        }

    public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                var teacher = ritual?.assignments?.FirstAssignedPawn("teacher") ?? ritual?.assignments?.FirstAssignedPawn("leader") ?? ritual?.assignments?.FirstAssignedPawn("speaker");
                if (teacher == null) return 0;
                var highest = teacher.skills?.skills.Where(s => s.def != SkillDefOf.Social).MaxBy(s => s.Level);
                if (highest == null) return 0;
                return highest.Level;
            }
            catch { return 0; }
        }

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                var teacher = ritual?.assignments?.FirstAssignedPawn("teacher") ?? ritual?.assignments?.FirstAssignedPawn("leader") ?? ritual?.assignments?.FirstAssignedPawn("speaker");
                if (teacher == null) return 0f;
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                var highest = teacher.skills?.skills.Where(s => s.def != SkillDefOf.Social).MaxBy(s => s.Level);
                if (highest == null) return 0f;
                int level = highest.Level;
                int req = props?.themeSkillThreshold ?? 8;
                int diff = level - req;
                float pct = Mathf.Max(0, diff) * (props?.themePerLevel ?? 0.05f);
                return pct;
            }
            catch { return 0f; }
        }


        public override string GetDesc(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                Log.Message("[RkM] RitualOutcomeComp_TeacherSkill GetDesc invoked");
                var teacher = ritual?.assignments?.FirstAssignedPawn("teacher") ?? ritual?.assignments?.FirstAssignedPawn("leader") ?? ritual?.assignments?.FirstAssignedPawn("speaker");
                if (teacher == null) return string.Empty;
                var highest = teacher.skills?.skills.Where(s => s.def != SkillDefOf.Social).MaxBy(s => s.Level);
                if (highest == null) return string.Empty;
                int level = highest.Level;
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int req = props?.themeSkillThreshold ?? 8;
                float pct = Mathf.Max(0, level - req) * (props?.themePerLevel ?? 0.05f);
                return $"Course theme: {(pct * 100f).ToString("F0")}% ({highest.def.LabelCap} {level}/{req})";
            }
            catch
            {
                return string.Empty;
            }
        }

        public override IEnumerable<string> BlockingIssues(Precept_Ritual ritual, TargetInfo target, RitualRoleAssignments assignments)
        {
            var teacher = assignments?.FirstAssignedPawn("teacher") ?? assignments?.FirstAssignedPawn("leader") ?? assignments?.FirstAssignedPawn("speaker");
            if (teacher == null)
                yield break;
            var highest = teacher.skills?.skills.Where(s => s.def != SkillDefOf.Social).MaxBy(s => s.Level);
            if (highest == null)
                yield break;
            int level = highest.Level;
            var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
            int req = props?.themeSkillThreshold ?? 8;
            if (level < req)
            {
                yield return $"Require {highest.def.LabelCap} level {req} for lecturer";
            }
        }
    }

    // New comp: shows lecturer's Social skill separately (3% per level above 8)
    public class RitualOutcomeComp_SocialSkill : RitualOutcomeComp_Quality
    {
        public override bool Applies(LordJob_Ritual ritual) => true;

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            try
            {
                if (assignments == null) return null;
                var teacher = assignments.FirstAssignedPawn("teacher") ?? assignments.FirstAssignedPawn("leader") ?? assignments.FirstAssignedPawn("speaker");
                if (teacher == null) return null;
                var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int req = props?.socialSkillThreshold ?? 8;
                var social = teacher.skills?.GetSkill(SkillDefOf.Social);
                int level = social?.Level ?? 0;
                int diff = level - req;
                float pct = Mathf.Max(0, diff) * (props?.socialPerLevel ?? 0.03f); // per-level pct from props
                string change = pct > 0f ? $"+{(pct * 100f).ToString("F0")} %" : "-";
                return new QualityFactor
                {
                    label = "Lecturer social",
                    count = $"{level}/{req}",
                    present = level >= req,
                    quality = pct,
                    qualityChange = change,
                    positive = level >= req,
                    priority = 4.5f
                };
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_SocialSkill error: " + ex, 472392);
                return null;
            }
        }

    public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                var teacher = ritual?.assignments?.FirstAssignedPawn("teacher") ?? ritual?.assignments?.FirstAssignedPawn("leader") ?? ritual?.assignments?.FirstAssignedPawn("speaker");
                if (teacher == null) return 0;
                var social = teacher.skills?.GetSkill(SkillDefOf.Social);
                int level = social?.Level ?? 0;
                return level;
            }
            catch { return 0; }
        }

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                var teacher = ritual?.assignments?.FirstAssignedPawn("teacher") ?? ritual?.assignments?.FirstAssignedPawn("leader") ?? ritual?.assignments?.FirstAssignedPawn("speaker");
                if (teacher == null) return 0f;
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int req = props?.socialSkillThreshold ?? 8;
                var social = teacher.skills?.GetSkill(SkillDefOf.Social);
                int level = social?.Level ?? 0;
                int diff = level - req;
                float pct = Mathf.Max(0, diff) * (props?.socialPerLevel ?? 0.03f);
                return pct;
            }
            catch { return 0f; }
        }


        public override IEnumerable<string> BlockingIssues(Precept_Ritual ritual, TargetInfo target, RitualRoleAssignments assignments)
        {
            var teacher = assignments?.FirstAssignedPawn("teacher") ?? assignments?.FirstAssignedPawn("leader") ?? assignments?.FirstAssignedPawn("speaker");
            if (teacher == null)
                yield break;
            var social = teacher.skills?.GetSkill(SkillDefOf.Social);
            int level = social?.Level ?? 0;
            var props = ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
            int req = props?.socialSkillThreshold ?? 8;
            if (level < req)
            {
                yield return $"Require Social level {req} for lecturer";
            }
        }

        public override string GetDesc(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                Log.Message("[RkM] RitualOutcomeComp_SocialSkill GetDesc invoked");
                var teacher = ritual?.assignments?.FirstAssignedPawn("teacher") ?? ritual?.assignments?.FirstAssignedPawn("leader") ?? ritual?.assignments?.FirstAssignedPawn("speaker");
                if (teacher == null) return string.Empty;
                var social = teacher.skills?.GetSkill(SkillDefOf.Social);
                int level = social?.Level ?? 0;
                var props = ritual?.Ritual?.outcomeEffect?.def?.GetModExtension<RitualGiveClassXPProps>();
                int req = props?.socialSkillThreshold ?? 8;
                float pct = Mathf.Max(0, level - req) * (props?.socialPerLevel ?? 0.03f);
                return $"Lecturer social: {(pct * 100f).ToString("F0")}% ({level}/{req})";
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class RitualOutcomeComp_SeatingRate : RitualOutcomeComp_Quality
    {
        public override bool Applies(LordJob_Ritual ritual) => true;

        public override QualityFactor GetQualityFactor(Precept_Ritual ritual, TargetInfo ritualTarget, RitualObligation obligation, RitualRoleAssignments assignments, RitualOutcomeComp_Data data)
        {
            try
            {
                if (assignments == null || ritualTarget.Map == null)
                {
                    return null;
                }
                SeatingStats(assignments, ritualTarget.Map, ritualTarget.Cell, out int seated, out int total, out float qualityOffset);
                return new QualityFactor
                {
                    label = "Seated attendees",
                    count = total > 0 ? $"{seated}/{total}" : "0/0",
                    present = seated >= total / 2,
                    quality = qualityOffset,
                    qualityChange = qualityOffset.ToString("P0"),
                    positive = qualityOffset >= 0f,
                    priority = 1.5f
                };
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("RitualOutcomeComp_SeatingRate error: " + ex, 472393);
                return null;
            }
        }

        public override float QualityOffset(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                SeatingStats(ritual?.assignments, ritual?.Map, ritual?.Spot ?? IntVec3.Invalid, out int _, out int _, out float quality);
                return quality;
            }
            catch
            {
                return 0f;
            }
        }

        public override float Count(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            SeatingStats(ritual?.assignments, ritual?.Map, ritual?.Spot ?? IntVec3.Invalid, out int seated, out int total, out float _);
            return total == 0 ? 0f : (float)seated / total;
        }

        public override string GetDesc(LordJob_Ritual ritual, RitualOutcomeComp_Data data)
        {
            try
            {
                SeatingStats(ritual?.assignments, ritual?.Map, ritual?.Spot ?? IntVec3.Invalid, out int seated, out int total, out float quality);
                return total > 0 ? $"Seated rate: {quality.ToString("P0")} ({seated}/{total})" : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

    private static void SeatingStats(RitualRoleAssignments assignments, Map map, IntVec3 spot, out int seated, out int total, out float quality)
        {
            seated = 0;
            total = 0;
            quality = 0f;
            if (assignments == null || map == null || !spot.IsValid)
            {
                return;
            }

            var participants = assignments.Participants?.ToList();
            if (participants == null)
            {
                return;
            }
            total = participants.Count;
            if (total == 0)
            {
                return;
            }

            // Get all available seats within range of the lectern
            var availableSeats = GetAvailableSeatsInRange(map, spot);
            seated = Math.Min(availableSeats.Count, total); // Count how many participants could be seated

            float seatRatio = (float)seated / total;
            // Quality: -40% when no one seated, +10% when everyone seated
            quality = Mathf.Lerp(-0.4f, 0.1f, Mathf.Clamp01(seatRatio));
        }

        private static List<Thing> GetAvailableSeatsInRange(Map map, IntVec3 spot)
        {
            var seats = new List<Thing>();
            if (map == null || !spot.IsValid)
            {
                return seats;
            }

            // Check for seats within a reasonable range (e.g., 10 cells)
            float searchRadius = 10f;
            foreach (var cell in GenRadial.RadialCellsAround(spot, searchRadius, true))
            {
                foreach (var thing in cell.GetThingList(map))
                {
                    // Check if it's a sittable building
                    if (thing.def.building?.isSittable == true)
                    {
                        seats.Add(thing);
                    }
                }
            }

            return seats;
        }
    }
}
