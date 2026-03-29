using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RkM
{
    /// <summary>
    /// Job driver for adding ingredients to the Big Pot (Building_FoodGetter).
    /// </summary>
    public class JobDriver_FillBigPot : JobDriver
    {
        private const int FillDuration = 120; // Ticks to add ingredients

        private Building_FoodGetter BigPot => (Building_FoodGetter)TargetThingA;
        private Thing Ingredient => TargetThingB;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (BigPot?.nutritionComp == null)
                return false;

            return pawn.Reserve(BigPot, job, 1, -1, null, errorOnFailed) 
                && pawn.Reserve(Ingredient, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail conditions
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);

            // Go to the ingredient
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);

            // Pick up the ingredient
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true);

            // Go to the pot
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            // Add ingredient to pot
            Toil addToil = ToilMaker.MakeToil("AddIngredientToPot");
            addToil.initAction = () =>
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried != null && BigPot?.nutritionComp != null)
                {
                    float nutritionValue = carried.GetStatValue(StatDefOf.Nutrition) * carried.stackCount;
                    BigPot.nutritionComp.AddNutrition(nutritionValue, new List<Thing> { carried });
                    carried.Destroy();
                }
            };
            addToil.defaultCompleteMode = ToilCompleteMode.Delay;
            addToil.defaultDuration = FillDuration;
            addToil.WithProgressBarToilDelay(TargetIndex.A);
            yield return addToil;
        }
    }
}
