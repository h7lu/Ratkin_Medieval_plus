using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RkM;

public class WorkGiver_DoBillFoodGetter : WorkGiver_DoBill
{
    private static JobDef Job => RkMDefOf.RkM_DoBillFoodGetter;
        private List<ThingCount> chosenIngThings = [];
        
        private static List<IngredientCount> missingIngredients = [];
        private static List<Thing> tmpMissingUniqueIngredients = [];
        private static readonly IntRange ReCheckFailedBillTicksRange = new(500, 600);
        private static List<Thing> relevantThings = [];
        private static HashSet<Thing> processedThings = [];
        private static List<Thing> newRelevantThings = [];
        private static List<Thing> tmpMedicine = [];
        private static DefCountList availableCounts = new();

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Some;

        public override ThingRequest PotentialWorkThingRequest => 
            def.fixedBillGiverDefs is { Count: 1 } ? ThingRequest.ForDef(def.fixedBillGiverDefs[0]) : ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.PotentialBillGiver);
            
            foreach (var t in list)
                if (t is IBillGiver billGiver && billGiver != pawn && 
                    ThingIsUsableBillGiver(t) && billGiver.BillStack.AnyShouldDoNow)
                    return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            // 检查是否是可用的工作台
            if (thing is not IBillGiver billGiver || 
                !ThingIsUsableBillGiver(thing) || 
                !billGiver.BillStack.AnyShouldDoNow || 
                !billGiver.UsableForBillsAfterFueling() || 
                !pawn.CanReserve(thing, 1, -1, null, forced) || 
                thing.IsBurning())
                return null;
            
            // 检查交互单元格是否可用
            if (thing.def.hasInteractionCell && 
                !pawn.CanReserveSittableOrSpot(thing.InteractionCell, thing, forced))
                return null;
            
            // 检查是否需要加油
            CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
            if (compRefuelable == null || compRefuelable.HasFuel)
            {
                // 移除无法完成的配方
                billGiver.BillStack.RemoveIncompletableBills();
                return StartOrResumeBillJob(pawn, billGiver, forced);
            }
            
            // 如果可以加油，先分配加油工作
            if (!RefuelWorkGiverUtility.CanRefuel(pawn, thing, forced)) return null;
            return RefuelWorkGiverUtility.RefuelJob(pawn, thing, forced);
        }

        private static UnfinishedThing ClosestUnfinishedThingForBill(Pawn pawn, Bill_ProductionWithUft bill)
        {
            Predicate<Thing> validator = t =>
            {
                if (!t.IsForbidden(pawn) && 
                    ((UnfinishedThing)t).Recipe == bill.recipe && 
                    ((UnfinishedThing)t).Creator == pawn)
                {
                    List<Thing> ingredients = ((UnfinishedThing)t).ingredients;
                    // 检查所有材料是否符合配方要求
                    if (ingredients.TrueForAll(x => bill.IsFixedOrAllowedIngredient(x.def))) return pawn.CanReserve(t);
                }
                return false;
            };
            
            return (UnfinishedThing)GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(bill.recipe.unfinishedThingDef),
                PathEndMode.InteractionCell,
                TraverseParms.For(pawn, pawn.NormalMaxDanger()),
                9999f,
                validator);
        }

        private static Job FinishUftJob(Pawn pawn, UnfinishedThing uft, Bill_ProductionWithUft bill)
        {
            // 安全检查：确保殖民者是未完成物品的创建者
            if (uft.Creator != pawn)
            {
                Log.Error($"Tried to get FinishUftJob for {pawn} finishing {uft} but its creator is {uft.Creator}");
                return null;
            }
            
            // 尝试清理工作台上的物品
            Job job = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, bill.billStack.billGiver, uft);
            if (job != null && job.targetA.Thing != uft) return job;
            
            // 创建完成未完成物品的工作
            Job finishJob = JobMaker.MakeJob(Job, (Thing)bill.billStack.billGiver);
            finishJob.bill = bill;
            finishJob.targetQueueB = [uft];
            finishJob.countQueue = [1];
            finishJob.haulMode = HaulMode.ToCellNonStorage;
            
            return finishJob;
        }

        private Job StartOrResumeBillJob(Pawn pawn, IBillGiver giver, bool forced = false)
        {
            bool isFloatMenu = FloatMenuMakerMap.makingFor == pawn;
            
            // 遍历所有配方
            foreach (var bill in giver.BillStack)
            {
                // 检查配方是否可用
                if ((bill.recipe.requiredGiverWorkType == null || bill.recipe.requiredGiverWorkType == def.workType) &&
                    (Find.TickManager.TicksGame > bill.nextTickToSearchForIngredients || 
                     FloatMenuMakerMap.makingFor == pawn) && bill.ShouldDoNow() && 
                    bill.PawnAllowedToStartAnew(pawn))
                {
                    // 检查专项要求
                    ModExtension_NutritionRecipe extension = bill.recipe.GetModExtension<ModExtension_NutritionRecipe>();
                    switch (extension)
                    {
                        case ModExtension_NutritionStorageRecipe extensionS:
                        {
                            if (giver is Building_FoodGetter foodGetter) if (foodGetter.TargetNutrition <= foodGetter.Nutrition) continue;
                            break;
                        }
                        case ModExtension_NutritionConsumeRecipe extensionC:
                        {
                            if (giver is Building_FoodGetter foodGetter)
                            {
                                if (!foodGetter.CanConsumeNow) continue;
                                if (extensionC.Nutrition == -1 && !foodGetter.HasEnoughFeedstockInHoppers()) continue;
                                else if (extensionC.Nutrition != -1 && foodGetter.Nutrition < extensionC.Nutrition) continue;}
                            break;
                        }
                        case ModExtension_WithoutNutritionGetRecipe extensionW:
                        {
                            if (giver is Building_FoodGetter foodGetter)
                            {
                                if (!foodGetter.CanConsumeNow) continue;
                                if (bill.recipe.products.NullOrEmpty()) continue;
                                if (extensionW.matchDispensable&&(bill.recipe.products.First()?.thingDef!=foodGetter.DispensableDef||foodGetter.DispensableDef==null)) continue;
                            }
                            break;
                        }
                    }

                    // 检查技能要求
                    SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
                    if (skillRequirement != null)
                    {
                        JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
                        continue;
                    }
                    // 检查医疗配方
                    if (bill is Bill_Medical billMedical)
                    {
                        if (billMedical.IsSurgeryViolationOnExtraFactionMember(pawn))
                        {
                            JobFailReason.Is("SurgeryViolationFellowFactionMember".Translate());
                            continue;
                        }
                        
                        if (!pawn.CanReserve(billMedical.GiverPawn, 1, -1, null, forced))
                        {
                            Pawn reserver = pawn.MapHeld.reservationManager.FirstRespectedReserver(billMedical.GiverPawn, pawn);
                            JobFailReason.Is("IsReservedBy".Translate(billMedical.GiverPawn.LabelShort, reserver.LabelShort));
                            continue;
                        }
                    }
                    // 检查机械师配方
                    if (bill is Bill_Mech billMech && billMech.Gestator.WasteProducer.Waste != null && billMech.Gestator.GestatingMech == null)
                    {
                        JobFailReason.Is("WasteContainerFull".Translate());
                        continue;
                    }
                    
                    // 处理未完成物品
                    if (bill is Bill_ProductionWithUft billProductionWithUft)
                    {
                        if (billProductionWithUft.BoundUft != null)
                        {
                            // 继续完成已绑定的未完成物品
                            if (billProductionWithUft.BoundWorker == pawn && 
                                pawn.CanReserveAndReach(billProductionWithUft.BoundUft, PathEndMode.Touch, Danger.Deadly) && 
                                !billProductionWithUft.BoundUft.IsForbidden(pawn))
                                return FinishUftJob(pawn, billProductionWithUft.BoundUft, billProductionWithUft);
                            continue;
                        }

                        // 寻找未完成的物品
                        UnfinishedThing unfinishedThing = ClosestUnfinishedThingForBill(pawn, billProductionWithUft);
                        if (unfinishedThing != null)
                            return FinishUftJob(pawn, unfinishedThing, billProductionWithUft);
                    }
                    
                    // 处理自主工作台配方
                    if (bill is Bill_Autonomous billAutonomous && billAutonomous.State != FormingState.Gathering)
                        return WorkOnFormedBill((Thing)giver, billAutonomous);
                    
                    // 清空材料列表
                    List<IngredientCount> localMissingIngredients = null;
                    if (isFloatMenu)
                    {
                        localMissingIngredients = missingIngredients;
                        localMissingIngredients.Clear();
                        tmpMissingUniqueIngredients.Clear();
                    }
                    
                    // 检查医疗配方的特殊材料
                    Bill_Medical billMedical2 = bill as Bill_Medical;
                    List<Thing> uniqueRequiredIngredients = billMedical2?.uniqueRequiredIngredients;
                    if (uniqueRequiredIngredients != null && !uniqueRequiredIngredients.NullOrEmpty())
                    {
                        foreach (Thing thing in billMedical2.uniqueRequiredIngredients)
                        {
                            if (thing.IsForbidden(pawn) || 
                                !pawn.CanReserveAndReach(thing, PathEndMode.OnCell, Danger.Deadly))
                                tmpMissingUniqueIngredients.Add(thing);
                        }
                    }

                    // 尝试寻找最佳材料
                    if (TryFindBestBillIngredients(bill, pawn, (Thing)giver, chosenIngThings, localMissingIngredients) && 
                        tmpMissingUniqueIngredients.NullOrEmpty())
                    {
                        isFloatMenu = false;
                        
                        // 添加医疗配方的特殊材料
                        List<Thing> uniqueRequiredIngredients2 = billMedical2?.uniqueRequiredIngredients;
                        if (uniqueRequiredIngredients2 != null && !uniqueRequiredIngredients2.NullOrEmpty())
                            foreach (Thing thing in billMedical2.uniqueRequiredIngredients) chosenIngThings.Add(new ThingCount(thing, 1));

                        // 创建工作
                        Job haulOffJob;
                        Job result = TryStartNewDoBillJob(pawn, bill, giver, chosenIngThings, out haulOffJob);
                        chosenIngThings.Clear();
                        return result;
                    }
                    
                    // 如果没有找到材料，设置重新检查时间
                    if (FloatMenuMakerMap.makingFor != pawn)
                        bill.nextTickToSearchForIngredients =
                            Find.TickManager.TicksGame + ReCheckFailedBillTicksRange.RandomInRange;
                    else if (isFloatMenu)
                    {
                        // 显示失败原因
                        if (CannotDoBillDueToMedicineRestriction(giver, bill, localMissingIngredients))
                            JobFailReason.Is(
                                "NoMedicineMatchingCategory".Translate(GetMedicalCareCategory((Thing)giver).GetLabel()
                                    .Named("CATEGORY")), bill.Label);
                        else
                        {
                            string missingList = localMissingIngredients
                                .Select(missing => missing.Summary)
                                .Concat(tmpMissingUniqueIngredients.Select(t => t.Label))
                                .ToCommaList();
                            
                            JobFailReason.Is("MissingMaterials".Translate(missingList), bill.Label);
                        }
                        isFloatMenu = false;
                    }
                    
                    chosenIngThings.Clear();
                }
            }
            
            chosenIngThings.Clear();
            return null;
        }

        private static bool CannotDoBillDueToMedicineRestriction(IBillGiver giver, Bill bill, List<IngredientCount> missingIngredients)
        {
            if (giver is not Pawn pawn)
                return false;
            
            // 检查是否有需要药品的配方
            bool needsMedicine = false;
            foreach (IngredientCount ingredient in missingIngredients)
            {
                if (ingredient.filter.Allows(ThingDefOf.MedicineIndustrial))
                {
                    needsMedicine = true;
                    break;
                }
            }
            
            if (!needsMedicine) return false;
            
            // 检查是否有符合医疗等级的药品可用
            MedicalCareCategory medicalCareCategory = GetMedicalCareCategory(pawn);
            return pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine).All(medicine => !IsUsableIngredient(medicine, bill) || !medicalCareCategory.AllowsMedicine(medicine.def));
        }

        public static Job TryStartNewDoBillJob(Pawn pawn, Bill bill, IBillGiver giver, List<ThingCount> chosenIngThings, out Job haulOffJob, bool dontCreateJobIfHaulOffRequired = true)
        {
            haulOffJob = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, giver, null);
            if (haulOffJob != null && dontCreateJobIfHaulOffRequired)
                return haulOffJob;
            
            // 创建执行配方的工作
            Job job = JobMaker.MakeJob(Job, (Thing)giver);
            job.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
            job.countQueue = new List<int>(chosenIngThings.Count);
            
            foreach (var t in chosenIngThings)
            {
                job.targetQueueB.Add(t.Thing);
                job.countQueue.Add(t.Count);
            }
            
            // 添加异种基因（如果存在）
            if (bill.xenogerm != null)
            {
                job.targetQueueB.Add(bill.xenogerm);
                job.countQueue.Add(1);
            }
            
            job.haulMode = HaulMode.ToCellNonStorage;
            job.bill = bill;
            
            return job;
        }

        private static Job WorkOnFormedBill(Thing giver, Bill_Autonomous bill)
        {
            Job job = JobMaker.MakeJob(Job, giver);
            job.bill = bill;
            return job;
        }

        public bool ThingIsUsableBillGiver(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            Corpse corpse = thing as Corpse;
            Pawn innerPawn = corpse?.InnerPawn;
            
            // 检查固定定义
            if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Contains(thing.def)) return true;
            // 检查活体
            if (pawn != null)
            {
                if (def.billGiversAllHumanlikes && pawn.RaceProps.Humanlike)
                    return true;
                if (def.billGiversAllMechanoids && pawn.RaceProps.IsMechanoid)
                    return true;
                if (def.billGiversAllAnimals && pawn.IsAnimal)
                    return true;
            }
            
            // 检查尸体
            if (corpse != null && innerPawn != null)
            {
                if (def.billGiversAllHumanlikesCorpses && innerPawn.RaceProps.Humanlike)
                    return true;
                if (def.billGiversAllMechanoidsCorpses && innerPawn.RaceProps.IsMechanoid)
                    return true;
                if (def.billGiversAllAnimalsCorpses && innerPawn.IsAnimal)
                    return true;
            }
            
            return false;
        }

        private static bool IsUsableIngredient(Thing t, Bill bill)
        {
            if (!bill.IsFixedOrAllowedIngredient(t))
                return false;
            
            foreach (IngredientCount ingredient in bill.recipe.ingredients)
                if (ingredient.filter.Allows(t))
                    return true;
            
            return false;
        }

        public static bool TryFindBestFixedIngredients(List<IngredientCount> ingredients, Pawn pawn, Thing ingredientDestination, List<ThingCount> chosen, float searchRadius = 999f)
        {
            return TryFindBestIngredientsHelper(
                t =>
                {
                    foreach (IngredientCount ingredient in ingredients)
                        if (ingredient.filter.Allows(t))
                            return true;
                    return false;
                },
                foundThings => TryFindBestIngredientsInSet_NoMixHelper(foundThings, ingredients, chosen, GetBillGiverRootCell(ingredientDestination, pawn), false, null),
                ingredients,
                pawn,
                ingredientDestination,
                chosen,
                searchRadius);
        }

        private static bool TryFindBestBillIngredients(Bill bill, Pawn pawn, Thing billGiver, List<ThingCount> chosen, List<IngredientCount> missingIngredients)
        {
            return TryFindBestIngredientsHelper(
                t => IsUsableIngredient(t, bill),
                foundThings => TryFindBestBillIngredientsInSet(foundThings, bill, chosen, GetBillGiverRootCell(billGiver, pawn), billGiver is Pawn, missingIngredients),
                bill.recipe.ingredients,
                pawn,
                billGiver,
                chosen,
                bill.ingredientSearchRadius);
        }

        private static bool TryFindBestIngredientsHelper(
            Predicate<Thing> thingValidator,
            Predicate<List<Thing>> foundAllIngredientsAndChoose,
            List<IngredientCount> ingredients,
            Pawn pawn,
            Thing billGiver,
            List<ThingCount> chosen,
            float searchRadius)
        {
            chosen.Clear();
            newRelevantThings.Clear();
            
            if (ingredients.Count == 0)
                return true;
            
            IntVec3 billGiverRootCell = GetBillGiverRootCell(billGiver, pawn);
            Region rootReg = billGiverRootCell.GetRegion(pawn.Map);
            if (rootReg == null)
                return false;
            
            relevantThings.Clear();
            processedThings.Clear();
            bool foundAll = false;
            float radiusSq = searchRadius * searchRadius;
            
            Predicate<Thing> baseValidator = t => 
                t.Spawned && 
                thingValidator(t) && 
                (t.Position - billGiver.Position).LengthHorizontalSquared < radiusSq && 
                !t.IsForbidden(pawn) && 
                pawn.CanReserve(t);
            
            bool billGiverIsPawn = billGiver is Pawn;
            
            // 如果是医疗配方，添加所有药品
            if (billGiverIsPawn)
            {
                AddEveryMedicineToRelevantThings(pawn, billGiver, relevantThings, baseValidator, pawn.Map);
                if (foundAllIngredientsAndChoose(relevantThings))
                {
                    relevantThings.Clear();
                    return true;
                }
            }
            
            // 检查自主工作台内部容器
            if (billGiver is Building_WorkTableAutonomous autonomousTable)
            {
                relevantThings.AddRange(autonomousTable.innerContainer);
                if (foundAllIngredientsAndChoose(relevantThings))
                {
                    relevantThings.Clear();
                    return true;
                }
            }
            
            // 遍历所有搬运来源
            foreach (IHaulSource haulSource in pawn.Map.haulDestinationManager.AllHaulSourcesListForReading)
            {
                if (haulSource.HaulSourceEnabled)
                {
                    if (haulSource is Thing { Spawned: true } thing && 
                        thing.Position.InHorDistOf(billGiver.Position, searchRadius) && 
                        !thing.IsForbidden(pawn))
                    {
                        ThingOwnerUtility.GetAllThingsRecursively(haulSource, newRelevantThings);
                        foreach (Thing t in newRelevantThings)
                        {
                            if (!processedThings.Contains(t) && 
                                !t.IsForbidden(pawn) && 
                                pawn.CanReserve(t) && 
                                thingValidator(t))
                            {
                                relevantThings.Add(t);
                                processedThings.Add(t);
                            }
                        }
                    }
                }
            }
            
            newRelevantThings.Clear();
            
            // 区域遍历寻找材料
            TraverseParms traverseParams = TraverseParms.For(pawn);
            RegionEntryPredicate entryCondition;
            
            if (Math.Abs(999f - searchRadius) >= 1f)
            {
                entryCondition = (from, r) =>
                {
                    if (!r.Allows(traverseParams, false)) return false;
                    
                    CellRect extentsClose = r.extentsClose;
                    int distX = Math.Abs(billGiver.Position.x - Math.Max(extentsClose.minX, Math.Min(billGiver.Position.x, extentsClose.maxX)));
                    if (distX > searchRadius) return false;
                    
                    int distZ = Math.Abs(billGiver.Position.z - Math.Max(extentsClose.minZ, Math.Min(billGiver.Position.z, extentsClose.maxZ)));
                    return distZ <= searchRadius && (distX * distX + distZ * distZ) <= radiusSq;
                };
            }
            else entryCondition = (from, r) => r.Allows(traverseParams, false);

            int adjacentRegionsAvailable = rootReg.Neighbors.Count(region => entryCondition(rootReg, region));
            int regionsProcessed = 0;
            processedThings.AddRange(relevantThings);
            
            if (foundAllIngredientsAndChoose(relevantThings)) return true;
            
            RegionProcessor regionProcessor = r =>
            {
                // 收集区域内的可搬运物品
                foreach (Thing thing in r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver)))
                {
                    if (!processedThings.Contains(thing) && 
                        ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch, pawn) && 
                        baseValidator(thing) && 
                        !(thing.def.IsMedicine && billGiverIsPawn))
                    {
                        newRelevantThings.Add(thing);
                        processedThings.Add(thing);
                    }
                }
                
                regionsProcessed++;
                
                // 处理收集到的材料
                if (newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                {
                    relevantThings.AddRange(newRelevantThings);
                    newRelevantThings.Clear();
                    
                    if (foundAllIngredientsAndChoose(relevantThings))
                    {
                        foundAll = true;
                        return true;
                    }
                }
                
                return false;
            };
            
            RegionTraverser.BreadthFirstTraverse(rootReg, entryCondition, regionProcessor, 99999);
            
            relevantThings.Clear();
            newRelevantThings.Clear();
            processedThings.Clear();
            
            return foundAll;
        }

        private static IntVec3 GetBillGiverRootCell(Thing billGiver, Pawn forPawn)
        {
            if (billGiver is not Building building) return billGiver.Position;
            
            if (building.def.hasInteractionCell) return building.InteractionCell;
            
            Log.Error($"Tried to find bill ingredients for {billGiver} which has no interaction cell.");
            return forPawn.Position;
        }

        private static void AddEveryMedicineToRelevantThings(Pawn pawn, Thing billGiver, List<Thing> relevantThings, Predicate<Thing> baseValidator, Map map)
        {
            MedicalCareCategory medicalCareCategory = GetMedicalCareCategory(billGiver);
            List<Thing> allMedicine = map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine);
            tmpMedicine.Clear();
            
            // 收集符合医疗等级的药品
            for (int i = 0; i < allMedicine.Count; i++)
            {
                Thing medicine = allMedicine[i];
                if (medicalCareCategory.AllowsMedicine(medicine.def) && 
                    baseValidator(medicine) && 
                    pawn.CanReach(medicine, PathEndMode.OnCell, Danger.Deadly)) tmpMedicine.Add(medicine);
            }
            
            // 按医疗效力和距离排序
            tmpMedicine.SortBy(x => -x.GetStatValue(StatDefOf.MedicalPotency), 
                               x => x.Position.DistanceToSquared(billGiver.Position));
            
            relevantThings.AddRange(tmpMedicine);
            tmpMedicine.Clear();
        }

        public static MedicalCareCategory GetMedicalCareCategory(Thing billGiver)
        {
            if (billGiver is Pawn { playerSettings: not null } pawn)
                return pawn.playerSettings.medCare;
            
            return MedicalCareCategory.Best;
        }

        private static bool TryFindBestBillIngredientsInSet(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, IntVec3 rootCell, bool alreadySorted, List<IngredientCount> missingIngredients) => bill.recipe.allowMixingIngredients ? TryFindBestBillIngredientsInSet_AllowMix(availableThings, bill, chosen, rootCell, missingIngredients) : TryFindBestBillIngredientsInSet_NoMix(availableThings, bill, chosen, rootCell, alreadySorted, missingIngredients);

        private static bool TryFindBestBillIngredientsInSet_NoMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, IntVec3 rootCell, bool alreadySorted, List<IngredientCount> missingIngredients) => TryFindBestIngredientsInSet_NoMixHelper(availableThings, bill.recipe.ingredients, chosen, rootCell, alreadySorted, missingIngredients, bill);

        private static bool TryFindBestIngredientsInSet_NoMixHelper(List<Thing> availableThings, List<IngredientCount> ingredients, List<ThingCount> chosen, IntVec3 rootCell, bool alreadySorted, List<IngredientCount> missingIngredients, Bill bill = null)
        {
            // 按距离排序
            if (!alreadySorted)
                availableThings.Sort((t1, t2) => 
                    (t1.PositionHeld - rootCell).LengthHorizontalSquared.CompareTo(
                        (t2.PositionHeld - rootCell).LengthHorizontalSquared));
            
            chosen.Clear();
            availableCounts.Clear();
            missingIngredients?.Clear();

            availableCounts.GenerateFrom(availableThings);
            
            // 为每个配方成分寻找材料
            foreach (var ingredient in ingredients)
            {
                bool found = false;
                for (int j = 0; j < availableCounts.Count; j++)
                {
                    float requiredCount = (bill != null) ? 
                        ingredient.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe, bill) : 
                        ingredient.GetBaseCount();
                    
                    if ((bill == null || bill.recipe.ignoreIngredientCountTakeEntireStacks || requiredCount <= availableCounts.GetCount(j)) && 
                        ingredient.filter.Allows(availableCounts.GetDef(j)) && 
                        (bill == null || ingredient.IsFixedIngredient || bill.ingredientFilter.Allows(availableCounts.GetDef(j))))
                    {
                        foreach (var t in availableThings)
                        {
                            if (t.def == availableCounts.GetDef(j))
                            {
                                int availableCount = t.stackCount - ThingCountUtility.CountOf(chosen, t);
                                if (availableCount > 0)
                                {
                                    if (bill != null && bill.recipe.ignoreIngredientCountTakeEntireStacks)
                                    {
                                        ThingCountUtility.AddToList(chosen, t, availableCount);
                                        return true;
                                    }
                                    
                                    int takeCount = Mathf.Min(Mathf.FloorToInt(requiredCount), availableCount);
                                    ThingCountUtility.AddToList(chosen, t, takeCount);
                                    requiredCount -= takeCount;
                                    
                                    if (requiredCount < 0.001f)
                                    {
                                        found = true;
                                        float remainingCount = availableCounts.GetCount(j) - requiredCount;
                                        availableCounts.SetCount(j, remainingCount);
                                        break;
                                    }
                                }
                            }
                        }
                        if (found) break;
                    }
                }
                if (!found)
                {
                    if (missingIngredients == null) return false;
                    missingIngredients.Add(ingredient);
                }
            }
            return missingIngredients == null || missingIngredients.Count == 0;
        }

        private static bool TryFindBestBillIngredientsInSet_AllowMix(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, IntVec3 rootCell, List<IngredientCount> missingIngredients)
        {
            chosen.Clear();
            missingIngredients?.Clear();

            // 按材料价值和距离排序
            availableThings.SortBy(
                t => bill.recipe.IngredientValueGetter.ValuePerUnitOf(t.def),
                t => (t.Position - rootCell).LengthHorizontalSquared);
            
            // 允许混合材料
            foreach (var ingredient in bill.recipe.ingredients)
            {
                float requiredValue = ingredient.GetBaseCount();
                
                foreach (var thing in availableThings)
                {
                    if (ingredient.filter.Allows(thing) && (ingredient.IsFixedIngredient || bill.ingredientFilter.Allows(thing)))
                    {
                        float valuePerUnit = bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
                        int takeCount = Mathf.Min(Mathf.CeilToInt(requiredValue / valuePerUnit), thing.stackCount);
                        
                        ThingCountUtility.AddToList(chosen, thing, takeCount);
                        requiredValue -= takeCount * valuePerUnit;
                        
                        if (requiredValue <= 0.0001f) break;
                    }
                }
                if (requiredValue > 0.0001f)
                {
                    if (missingIngredients == null) return false;
                    missingIngredients.Add(ingredient);
                }
            }
            return missingIngredients == null || missingIngredients.Count == 0;
        }

        // 内部辅助类：用于跟踪材料数量和类型
        private class DefCountList
        {
            private List<ThingDef> defs = [];
            private List<float> counts = [];

            public int Count => defs.Count;

            public float this[ThingDef def]
            {
                get
                {
                    int index = defs.IndexOf(def);
                    return index < 0 ? 0f : counts[index];
                }
                set
                {
                    int index = defs.IndexOf(def);
                    if (index < 0)
                    {
                        defs.Add(def);
                        counts.Add(value);
                        index = defs.Count - 1;
                    }
                    else
                    {
                        counts[index] = value;
                    }
                    
                    CheckRemove(index);
                }
            }

            public float GetCount(int index) => counts[index];

            public void SetCount(int index, float val)
            {
                counts[index] = val;
                CheckRemove(index);
            }

            public ThingDef GetDef(int index) => defs[index];

            private void CheckRemove(int index)
            {
                if (counts[index] == 0f)
                {
                    counts.RemoveAt(index);
                    defs.RemoveAt(index);
                }
            }

            public void Clear()
            {
                defs.Clear();
                counts.Clear();
            }

            public void GenerateFrom(List<Thing> things)
            {
                Clear();
                for (int i = 0; i < things.Count; i++)
                {
                    ThingDef def = things[i].def;
                    this[def] += things[i].stackCount;
                }
            }
        }
    }