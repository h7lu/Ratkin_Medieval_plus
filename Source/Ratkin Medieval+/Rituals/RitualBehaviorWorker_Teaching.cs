using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RkM
{
    // Custom ritual behavior worker to validate who can be assigned to the lecturer/speaker role.
    // It makes the Begin Ritual UI show a helpful warning and prevent dragging a pawn into the
    // lecturer slot unless they have Social >= 8 and at least one other skill >= 8.
    public class RitualBehaviorWorker_Teaching : RitualBehaviorWorker
    {
        public RitualBehaviorWorker_Teaching()
        {
        }

        public RitualBehaviorWorker_Teaching(RitualBehaviorDef def) : base(def)
        {
            this.def = def;
        }

        // Called by the Begin Ritual dialog when validating assigning a pawn to a role.
        // Return false and an explanatory message to block the drag/assignment.
        public override bool PawnCanFillRole(Pawn pawn, RitualRole role, out string s, TargetInfo ritualTarget)
        {
            // Let base run its checks first (e.g., ideos, precepts, reachability etc.)
            if (!base.PawnCanFillRole(pawn, role, out s, ritualTarget))
            {
                return false;
            }

            s = null;
            if (pawn == null || role == null)
            {
                return true;
            }

            // Apply our special check: require sufficient skills
            if (pawn.skills == null)
            {
                s = "This pawn has no skills data.";
                return false;
            }

            var social = pawn.skills.GetSkill(SkillDefOf.Social);
            if (social == null || social.Level < 8)
            {
                s = "Insufficient skills: Social must be at least 8.";
                return false;
            }

            bool hasOther = false;
            foreach (var sd in DefDatabase<SkillDef>.AllDefs)
            {
                if (sd == SkillDefOf.Social) continue;
                var sk = pawn.skills.GetSkill(sd);
                if (sk != null && sk.Level >= 8)
                {
                    hasOther = true;
                    break;
                }
            }

            if (!hasOther)
            {
                s = "Insufficient skills: must have Social >= 8 and at least one other skill >= 8.";
                return false;
            }

            return true;
        }
    }
}
