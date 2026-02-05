using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RkM
{
    /// <summary>
    /// Custom building class for the Big Pot that allows pawns to eat directly from it.
    /// Extends Building_WorkTable to maintain bill functionality while adding dispenser behavior.
    /// </summary>
    // public class Building_BigPot : Building_WorkTable
    // {
    //     private CompAutoCookDispenser dispenserComp;
    //
    //     public CompAutoCookDispenser DispenserComp
    //     {
    //         get
    //         {
    //             if (dispenserComp == null)
    //                 dispenserComp = GetComp<CompAutoCookDispenser>();
    //             return dispenserComp;
    //         }
    //     }
    //
    //     public bool CanDispenseNow => DispenserComp != null && DispenserComp.CanDispenseNow;
    //
    //     public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    //     {
    //         foreach (var option in base.GetFloatMenuOptions(selPawn))
    //             yield return option;
    //
    //         // Add option to eat from pot
    //         if (DispenserComp != null && DispenserComp.Props.allowDirectConsumption)
    //         {
    //             if (!CanDispenseNow)
    //             {
    //                 yield return new FloatMenuOption("RkM_CannotEatFromPot".Translate() + ": " + "RkM_NotReady".Translate(), null);
    //             }
    //             else if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
    //             {
    //                 yield return new FloatMenuOption("RkM_CannotEatFromPot".Translate() + ": " + "NoPath".Translate(), null);
    //             }
    //             else
    //             {
    //                 yield return new FloatMenuOption(
    //                     "RkM_EatFromPot".Translate(),
    //                     () =>
    //                     {
    //                         Job job = JobMaker.MakeJob(RkMDefOf.RkM_EatFromBigPot, this);
    //                         selPawn.jobs.TryTakeOrderedJob(job);
    //                     }
    //                 );
    //             }
    //         }
    //     }
    //
    //     /// <summary>
    //     /// Dispenses food for a pawn to consume.
    //     /// </summary>
    //     public Thing TryDispenseFood()
    //     {
    //         return DispenserComp?.DispenseFood();
    //     }
    // }
}
