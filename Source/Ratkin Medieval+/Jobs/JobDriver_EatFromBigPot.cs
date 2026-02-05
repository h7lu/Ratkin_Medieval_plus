using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RkM
{
    ///  
    /// Job driver for eating from the Big Pot.
    /// Dispenses food into hand, then finds a place to sit and eat.
    ///  
    // public class JobDriver_EatFromBigPot : JobDriver
    // {
    //     private Building_BigPot BigPot => job.GetTarget(TargetIndex.A).Thing as Building_BigPot;
    //
    //     public override bool TryMakePreToilReservations(bool errorOnFailed)
    //     {
    //         // Reserve the pot to use it
    //         return pawn.Reserve(BigPot, job, 1, -1, null, errorOnFailed);
    //     }
    //
    //     protected override IEnumerable<Toil> MakeNewToils()
    //     {
    //         // 1. Go to Pot
    //         Toil gotoPot = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
    //         gotoPot.FailOnDespawnedNullOrForbidden(TargetIndex.A);
    //         gotoPot.FailOn(() => BigPot != null && !BigPot.CanDispenseNow);
    //         yield return gotoPot;
    //
    //         // 2. Dispense food into hands
    //         Toil dispense = ToilMaker.MakeToil("Dispense");
    //         dispense.initAction = () =>
    //         {
    //             Building_BigPot pot = BigPot;
    //             if (pot == null || !pot.CanDispenseNow)
    //             {
    //                 EndJobWith(JobCondition.Incompletable);
    //                 return;
    //             }
    //
    //             Thing food = pot.TryDispenseFood();
    //             if (food == null)
    //             {
    //                 EndJobWith(JobCondition.Incompletable);
    //                 return;
    //             }
    //
    //             // Pickup the food
    //             pawn.carryTracker.TryStartCarry(food);
    //             
    //             // Update job target to the food we are now holding
    //             job.SetTarget(TargetIndex.A, food);
    //             job.count = 1;
    //         };
    //         dispense.defaultCompleteMode = ToilCompleteMode.Instant;
    //         dispense.FailOnDespawnedNullOrForbidden(TargetIndex.A);
    //         yield return dispense;
    //
    //         // 3. Find a place to eat and carry food there
    //         // Note: TargetIndex.A is now the food
    //         yield return Toils_Ingest.CarryIngestibleToChewSpot(pawn, TargetIndex.A);
    //         
    //         // 4. Find table surface if applicable
    //         yield return Toils_Ingest.FindAdjacentEatSurface(TargetIndex.B, TargetIndex.A);
    //         
    //         // 5. Chew the food
    //         yield return Toils_Ingest.ChewIngestible(pawn, 1f, TargetIndex.A, TargetIndex.B);
    //         
    //         // 6. Finish executing (apply nutrition, thoughts, etc)
    //         yield return Toils_Ingest.FinalizeIngest(pawn, TargetIndex.A);
    //     }
    // }
}
