using RimWorld;
using Verse;
using Verse.AI;

namespace RkM
{
    /// <summary>
    /// Makes the teacher stand at their duty.focus cell and deliver a lecture.
    ///
    /// Problem being solved:
    ///   Vanilla JobGiver_GiveSpeech requires duty.focusSecond to be a
    ///   Building_Throne assigned to the pawn — it always returns null otherwise,
    ///   leaving the teacher with no job and causing them to wander.
    ///   JobGiver_GiveSpeechFacingTarget is not needed for this flow.
    ///
    /// How it works:
    ///   The RKM_TeachAtLectern DutyDef first sends the pawn to their exact
    ///   duty.focus cell (the standing spot in front of the Grand Lectern) via
    ///   JobGiver_GotoTravelDestination with exactCell=true.  Once there,
    ///   ThinkNode_ConditionalAtDutyLocation becomes satisfied and this giver
    ///   fires, creating a JobDefOf.GiveSpeech job at the pawn's current
    ///   position facing toward the spectators.
    ///
    ///   JobDriver_GiveSpeech supports a plain cell as TargetA (not just a
    ///   throne) and automatically faces RitualUtility.RitualCrowdCenterFor,
    ///   shows speech bubbles, and plays lecturer sounds.
    /// </summary>
    public class JobGiver_TeachAtLectern : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            PawnDuty duty = pawn.mindState.duty;
            if (duty == null)
                return null;

            // Stand at current position (GotoTravelDestination with exactCell=true
            // has already moved the pawn to duty.focus before this giver fires).
            IntVec3 standCell = pawn.Position;

            // Determine the facing target: honour overrideFacing from the ritual
            // position, otherwise face toward the spectator/student crowd area.
            Rot4? overrideFacing = duty.overrideFacing.IsValid ? duty.overrideFacing : (Rot4?)null;
            IntVec3 facingTarget = overrideFacing.HasValue
                ? standCell + overrideFacing.Value.FacingCell
                : duty.spectateRect.CenterCell;

            Job job = JobMaker.MakeJob(JobDefOf.GiveSpeech, standCell, facingTarget);
            job.showSpeechBubbles = true;
            job.speechFaceSpectatorsIfPossible = true;
            job.speechSoundMale   = SoundDefOf.Speech_Leader_Male;
            job.speechSoundFemale = SoundDefOf.Speech_Leader_Female;
            return job;
        }
    }
}
