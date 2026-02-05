using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RkM
{
    /// <summary>
    /// Job driver for adding ingredients to the Big Pot.
    /// </summary>
    // public class JobDriver_FillBigPot : JobDriver
    // {
    //     private const int FillDuration = 120; // Ticks to add ingredients
    //
    //     private Building_BigPot BigPot => (Building_BigPot)TargetThingA;
    //     private Thing Ingredient => TargetThingB;
    //
    //     public override bool TryMakePreToilReservations(bool errorOnFailed)
    //     {
    //         if (BigPot?.DispenserComp == null)
    //             return false;
    //
    //         return pawn.Reserve(BigPot, job, 1, -1, null, errorOnFailed) 
    //             && pawn.Reserve(Ingredient, job, 1, -1, null, errorOnFailed);
    //     }
    //
    //     protected override IEnumerable<Toil> MakeNewToils()
    //     {
    //         // Fail conditions
    //         this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
    //         this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
    //
    //         // Go to the ingredient
    //         yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
    //
    //         // Pick up the ingredient
    //         yield return Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true);
    //
    //         // Go to the pot
    //         yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
    //
    //         // Add ingredient to pot
    //         Toil addToil = ToilMaker.MakeToil("AddIngredientToPot");
    //         addToil.initAction = () =>
    //         {
    //             Thing carried = pawn.carryTracker.CarriedThing;
    //             if (carried != null && BigPot?.DispenserComp != null)
    //             {
    //                 float added = BigPot.DispenserComp.AddNutrition(carried);
    //                 if (added > 0)
    //                 {
    //                     // Item was consumed (partially or fully) by AddNutrition
    //                     pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
    //                 }
    //             }
    //         };
    //         addToil.defaultCompleteMode = ToilCompleteMode.Delay;
    //         addToil.defaultDuration = FillDuration;
    //         addToil.WithProgressBarToilDelay(TargetIndex.A);
    //         yield return addToil;
    //     }
    // }
}
