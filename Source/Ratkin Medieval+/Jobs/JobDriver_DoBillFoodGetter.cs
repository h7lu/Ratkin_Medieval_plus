using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RkM;

public class JobDriver_DoBillFoodGetter : JobDriver_DoBill
{
    protected override IEnumerable<Toil> MakeNewToils()
    {
        ModExtension_NutritionRecipe extension = job.bill.recipe.GetModExtension<ModExtension_NutritionRecipe>();
        if (extension is ModExtension_NutritionStorageRecipe extensionS)
            this.FailOn(() =>
            {
                if (job.GetTarget(TargetIndex.A).Thing is Building_FoodGetter foodGetter)
                    if (foodGetter.TargetNutrition <= foodGetter.Nutrition) return true;
                return false;
            });
        if (extension is ModExtension_NutritionConsumeRecipe extensionC)
            this.FailOn(() =>
            {
                if (job.GetTarget(TargetIndex.A).Thing is Building_FoodGetter foodGetter)
                {
                    if (!foodGetter.CanConsumeNow) return true;
                    if (extensionC.Nutrition == -1 && !foodGetter.HasEnoughFeedstockInHoppers()) return true;
                    else if (extensionC.Nutrition != -1 && foodGetter.Nutrition < extensionC.Nutrition) return true;
                }

                return false;
            });
        //ModExtension_WithoutNutritionGetRecipe
        if (extension is ModExtension_WithoutNutritionGetRecipe extensionW)
        { 
            this.FailOn(() =>
            { 
                if (job.GetTarget(TargetIndex.A).Thing is Building_FoodGetter foodGetter)
                {
                    if (!foodGetter.CanConsumeNow) return true;
                    if (job.bill.recipe.products.NullOrEmpty()) return true;
                    if (extensionW.matchDispensable&&(job.bill.recipe.products.First()?.thingDef!=foodGetter.DispensableDef||foodGetter.DispensableDef==null)) return true;
                }
                return false;
            });
        }
        
            this.AddEndCondition(() =>
            {
                Thing thing = this.GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (thing is Building && !thing.Spawned)
                {
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            });
            
            this.FailOnBurningImmobile(TargetIndex.A);
            
            this.FailOn(() =>
            {
                IBillGiver billGiver = this.job.GetTarget(TargetIndex.A).Thing as IBillGiver;
                if (billGiver != null)
                {
                    if (this.job.bill.DeletedOrDereferenced)
                    {
                        return true;
                    }
                    if (!billGiver.CurrentlyUsableForBills())
                    {
                        return true;
                    }
                }
                return false;
            });
            
            Toil gotoBillGiver = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell, false);
            
            Toil initToil = ToilMaker.MakeToil("MakeNewToils");
            initToil.initAction = () =>
            {
                if (this.job.targetQueueB is { Count: 1 })
                {
                    if (this.job.targetQueueB[0].Thing is UnfinishedThing { Destroyed: false } unfinishedThing)
                    {
                        unfinishedThing.BoundBill = (Bill_ProductionWithUft)this.job.bill;
                    }
                }
                
                this.job.bill.Notify_DoBillStarted(this.pawn);
            };
            yield return initToil;
            
            yield return Toils_Jump.JumpIf(gotoBillGiver, () => this.job.GetTargetQueue(TargetIndex.B).NullOrEmpty());
            
            foreach (Toil toil in CollectIngredientsToils(
                TargetIndex.B, 
                TargetIndex.A, 
                TargetIndex.C, 
                false, 
                true, 
                this.BillGiver is Building_WorkTableAutonomous))
            {
                yield return toil;
            }
            
            yield return gotoBillGiver;
            
            yield return Toils_Recipe.MakeUnfinishedThingIfNeeded();
            
            yield return Toils_Recipe.DoRecipeWork()
                .FailOnDespawnedNullOrForbiddenPlacedThings(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            
            yield return Toils_Recipe.CheckIfRecipeCanFinishNow();
            
            yield return Toils_Recipe.FinishRecipeAndStartStoringProduct(TargetIndex.None);
    }
    public new static IEnumerable<Toil> CollectIngredientsToils(TargetIndex ingredientInd, TargetIndex billGiverInd, TargetIndex ingredientPlaceCellInd, bool subtractNumTakenFromJobCount = false, bool failIfStackCountLessThanJobCount = true, bool placeInBillGiver = false)
		{
			Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(ingredientInd, true);
			yield return extract;
			Toil jumpIfHaveTargetInQueue = Toils_Jump.JumpIfHaveTargetInQueue(ingredientInd, extract);
			yield return JumpIfTargetInsideBillGiver(jumpIfHaveTargetInQueue, ingredientInd, billGiverInd);
			Toil getToHaulTarget = Toils_Goto.GotoThing(ingredientInd, PathEndMode.ClosestTouch, true).FailOnForbidden(ingredientInd).FailOnSomeonePhysicallyInteracting(ingredientInd);
			yield return getToHaulTarget;
			yield return Toils_Haul.StartCarryThing(ingredientInd, true, subtractNumTakenFromJobCount, failIfStackCountLessThanJobCount, false, true);
			yield return JumpToCollectNextIntoHandsForBill(getToHaulTarget, TargetIndex.B);
			yield return Toils_Goto.GotoThing(billGiverInd, PathEndMode.InteractionCell, false).FailOnDestroyedOrNull(ingredientInd);
			if (!placeInBillGiver)
			{
				Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(billGiverInd, ingredientInd, ingredientPlaceCellInd);
				yield return findPlaceTarget;
				yield return PlaceHauledThingInCell(ingredientPlaceCellInd, findPlaceTarget, false, false);
				Toil physReserveToil = ToilMaker.MakeToil("CollectIngredientsToils");
				physReserveToil.initAction = delegate()
				{
					physReserveToil.actor.Map.physicalInteractionReservationManager.Reserve(physReserveToil.actor, physReserveToil.actor.CurJob, physReserveToil.actor.CurJob.GetTarget(ingredientInd));
				};
				yield return physReserveToil;
				findPlaceTarget = null;
			}
			else
			{
				yield return Toils_Haul.DepositHauledThingInContainer(billGiverInd, ingredientInd, null);
			}
			yield return jumpIfHaveTargetInQueue;
		}private static Toil JumpIfTargetInsideBillGiver(Toil jumpToil, TargetIndex ingredient, TargetIndex billGiver)
    {
        Toil toil = ToilMaker.MakeToil("JumpIfTargetInsideBillGiver");
        toil.initAction = delegate()
        {
            Thing thing = toil.actor.CurJob.GetTarget(billGiver).Thing;
            if (thing == null || !thing.Spawned)
            {
                return;
            }
            Thing thing2 = toil.actor.jobs.curJob.GetTarget(ingredient).Thing;
            if (thing2 == null)
            {
                return;
            }
            ThingOwner thingOwner = thing.TryGetInnerInteractableThingOwner();
            if (thingOwner != null && thingOwner.Contains(thing2))
            {
                HaulAIUtility.UpdateJobWithPlacedThings(toil.actor.jobs.curJob, thing2, thing2.stackCount);
                toil.actor.jobs.curDriver.JumpToToil(jumpToil);
            }
        };
        return toil;
    }
        public static Toil PlaceHauledThingInCell(
            TargetIndex cellInd, 
            Toil nextToilOnPlaceFailOrIncomplete, 
            bool storageMode, 
            bool tryStoreInSameStorageIfSpotCantHoldWholeStack = false)
        {
            Toil toil = ToilMaker.MakeToil("PlaceHauledThingInCell");
            
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                IntVec3 cell = curJob.GetTarget(cellInd).Cell;
                
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error(actor + " tried to place hauled thing in cell but is not hauling anything.");
                    return;
                }
                
                SlotGroup slotGroup = actor.Map.haulDestinationManager.SlotGroupAt(cell);
                if (slotGroup != null && slotGroup.Settings.AllowedToAccept(actor.carryTracker.CarriedThing))
                {
                    actor.Map.designationManager.TryRemoveDesignationOn(actor.carryTracker.CarriedThing, DesignationDefOf.Haul);
                }
                
                Action<Thing, int> placedAction = null;
                if (curJob.def == JobDefOf.DoBill ||  curJob.def == RkMDefOf.RkM_DoBillFoodGetter ||
                    curJob.def == JobDefOf.RecolorApparel || 
                    curJob.def == JobDefOf.RefuelAtomic || 
                    curJob.def == JobDefOf.RearmTurretAtomic)
                {
                    placedAction = (th, added) => HaulAIUtility.UpdateJobWithPlacedThings(curJob, th, added);
                }
                
                Thing placedThing;
                if (!actor.carryTracker.TryDropCarriedThing(cell, ThingPlaceMode.Direct, out placedThing, placedAction))
                {
                    if (storageMode)
                    {
                        HandleStoragePlacementFailure(actor, curJob, cellInd, nextToilOnPlaceFailOrIncomplete, tryStoreInSameStorageIfSpotCantHoldWholeStack);
                    }
                    else if (nextToilOnPlaceFailOrIncomplete != null)
                    {
                        actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
                    }
                }
            };
            
            return toil;
        }
         private static void HandleStoragePlacementFailure(
            Pawn actor, 
            Job curJob, 
            TargetIndex cellInd, 
            Toil nextToilOnPlaceFailOrIncomplete, 
            bool tryStoreInSameStorageIfSpotCantHoldWholeStack)
        {
            if (nextToilOnPlaceFailOrIncomplete != null && 
                ((tryStoreInSameStorageIfSpotCantHoldWholeStack && 
                  StoreUtility.TryFindBestBetterStoreCellForIn(
                      actor.carryTracker.CarriedThing, 
                      actor, 
                      actor.Map, 
                      StoragePriority.Unstored, 
                      actor.Faction, 
                      curJob.bill?.GetSlotGroup(), 
                      out var newStorageCell, true)) || 
                 StoreUtility.TryFindBestBetterStoreCellFor(
                     actor.carryTracker.CarriedThing, 
                     actor, 
                     actor.Map, 
                     StoragePriority.Unstored, 
                     actor.Faction, 
                     out newStorageCell, true)))
            {
                if (actor.CanReserve(newStorageCell, 1, -1, null, false))
                {
                    actor.Reserve(newStorageCell, actor.CurJob, 1, -1, null, true);
                }
                
                actor.CurJob.SetTarget(cellInd, newStorageCell);
                actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
                return;
            }
            
            if (!HaulAIUtility.CanHaulAside(actor, actor.carryTracker.CarriedThing, out var asideCell))
            {
                Log.Warning($"Incomplete haul for {actor}: Could not find anywhere to put {actor.carryTracker.CarriedThing} near {actor.Position}. Destroying.");
                actor.carryTracker.CarriedThing.Destroy(DestroyMode.Vanish);
                return;
            }
            
            curJob.SetTarget(cellInd, asideCell);
            curJob.count = int.MaxValue;
            curJob.haulOpportunisticDuplicates = false;
            curJob.haulMode = HaulMode.ToCellNonStorage;
            
            if (nextToilOnPlaceFailOrIncomplete != null)
            {
                actor.jobs.curDriver.JumpToToil(nextToilOnPlaceFailOrIncomplete);
            }
        }
}