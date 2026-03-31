using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM.HarmonyPatches;


[HarmonyPatch(typeof(Verse.GenRecipe))]
[HarmonyPatch("MakeRecipeProducts")]
public static class GenRecipe_MakeRecipeProducts_Patch
{
    [HarmonyPostfix]
    public static void Postfix(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients,
        ref IEnumerable<Thing> __result, Thing dominantIngredient, IBillGiver billGiver,
        Precept_ThingStyle precept = null, ThingStyleDef style = null, int? overrideGraphicIndex = null)
    {
        DefModExtension tempExtension = recipeDef.GetModExtension<ModExtension_NutritionRecipe>();
        if (tempExtension != null && billGiver is Building_FoodGetter foodGetter){
            IEnumerable<Thing> productsToReplace = [];

            productsToReplace = tempExtension switch
            {
                ModExtension_NutritionConsumeRecipe extension => NutritionConsumeRecipe(recipeDef, foodGetter,
                    extension),
                ModExtension_NutritionStorageRecipe extension => NutritionStorageRecipe(recipeDef, foodGetter,
                    extension, ingredients),
                ModExtension_WithoutNutritionGetRecipe extension => WithoutNutritionGetRecipe(recipeDef, foodGetter,
                    extension),
                _ => __result,
            };
            __result = productsToReplace;
        }
        tempExtension = recipeDef.GetModExtension<ModExtension_RandomProductsWithEffectRecipe>();
        if (tempExtension != null)
        {
            __result = RandomProductsRecipe((ModExtension_RandomProductsWithEffectRecipe)tempExtension,billGiver,worker);
        }
    }
    
    public static IEnumerable<Thing> RandomProductsRecipe(ModExtension_RandomProductsWithEffectRecipe tempExtension,
        IBillGiver billGiver,
        Pawn pawn)
    {
        if (!tempExtension.weightConfigurations.Any())
        {
            // 如果没有配置项但有默认产品，则返回默认产品
            if (tempExtension.defaultProduct != null)
            {
                var thing = ThingMaker.MakeThing(tempExtension.defaultProduct);
                thing.stackCount = tempExtension.count;
                yield return thing;
            }
            yield break;
        }
        //(轮盘)按照权重随机选择.每项x.count份，共maximumWeight份,份数未满则填补默认项
        // 计算所有奖项的总频数
        var totalWeight = tempExtension.weightConfigurations.Sum(x => x.weight);
        var totalDraws = Math.Max(totalWeight, tempExtension.maximumWeight);
    
        // 确保总抽取数至少为1，避免除零错误
        if (totalDraws <= 0) yield break;

        // 计算需要填充的默认项数量
        var defaultCount = totalDraws - totalWeight;
    
        //随机数（0至totalDraws-1）
        var random = Rand.Range(0, totalDraws);
        ThingsWithAccidents result = null;
    
        foreach (var config in tempExtension.weightConfigurations)
        {
            if (random < config.weight)
            {
                result = config;
                break;
            }
            random -= config.weight;
        }
    
        // 如果没有找到匹配项且有默认项，则使用默认项
        if (result == null && defaultCount > 0 && tempExtension.defaultProduct != null)
        {
            var thing = ThingMaker.MakeThing(tempExtension.defaultProduct);
            thing.stackCount = tempExtension.count;
            yield return thing;
        }
    
        // 如果最终结果仍为null，不返回任何东西
        if (result == null) yield break;
        foreach (var accidentDef in result.accidents)
        {
            // 根据 accidentDef的workClass 创建一个 accident
            var accident = (Accident)Activator.CreateInstance(accidentDef.workClass);
            accident.def = accidentDef;
            accident.Do( billGiver.Map, pawn.Position);
        }
        foreach (var thingItem in result.things)
        {
            var thing =ThingMaker.MakeThing(thingItem.thingDef);
            thing.stackCount = thingItem.count;
            yield return thing;
        }
    }

    private static IEnumerable<Thing> WithoutNutritionGetRecipe(RecipeDef recipeDef, Building_FoodGetter foodGetter, ModExtension_WithoutNutritionGetRecipe extensionWithout)
    {
        if (!recipeDef.products.NullOrEmpty())
        {
            List<ThingDef> ingredients = [];
            if (extensionWithout.useIngredients)
            {
                ingredients = foodGetter.Ingredients;
            }
            if (extensionWithout.matchDispensable && foodGetter.DispensableDef == recipeDef.products.First().thingDef)
            {
                Thing thing = ThingMaker.MakeThing(foodGetter.DispensableDef);
                CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                foreach (var t in ingredients) compIngredients?.RegisterIngredient(t);
                thing.stackCount = Mathf.CeilToInt(1);
                yield return thing;
            }else
            {
                foreach (var product in recipeDef.products)
                {
                    Thing thing = ThingMaker.MakeThing( product.thingDef, null);
                    CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                    foreach (var t in ingredients) compIngredients?.RegisterIngredient(t);
                    thing.stackCount = Mathf.CeilToInt(product.count);
                    yield return thing;
                }
            }
        }
        
    }

    public static IEnumerable<Thing> NutritionConsumeRecipe(RecipeDef recipeDef, Building_FoodGetter foodGetter,
        ModExtension_NutritionConsumeRecipe extensionC)
    {

        List<ThingDef> ingredients = [];
        var nutrition = 0f;
        if (extensionC.Nutrition == -1 && foodGetter.HasEnoughFeedstockInHoppers() && foodGetter.CanConsumeNow)
        {
            ingredients = foodGetter.Ingredients;
            nutrition = foodGetter.def.building.nutritionCostPerDispense;
        }
        else if (extensionC.Nutrition != -1 && foodGetter.Nutrition >= extensionC.Nutrition && foodGetter.CanConsumeNow)
        {
            ingredients = foodGetter.Ingredients;
            nutrition = extensionC.Nutrition;
        }
        else Log.Message("[RkM] Can not to make this recipe");

        if (!recipeDef.products.NullOrEmpty())
        {
            Log.Message($"[RkM] Consume nutrition: {recipeDef.products}");
            foreach (var product in recipeDef.products)
            {
                Thing thing = ThingMaker.MakeThing(product.thingDef, null);
                CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                foreach (var t in ingredients) compIngredients.RegisterIngredient(t);
                thing.stackCount = Mathf.CeilToInt(product.count);
                yield return thing;
            }
        }
        else if (foodGetter.DispensableDef != null)
        {
            Thing thing = ThingMaker.MakeThing(foodGetter.DispensableDef, null);
            CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
            foreach (var t in ingredients) compIngredients.RegisterIngredient(t);
            thing.stackCount = Mathf.CeilToInt(1);
            yield return thing;
        }
        foodGetter.ConsumeNutrition(nutrition);
    }
    public static IEnumerable<Thing> NutritionStorageRecipe(RecipeDef recipeDef, Building_FoodGetter foodGetter,ModExtension_NutritionStorageRecipe extensionS,List<Thing> ingredients)
    {

        if (extensionS.Nutrition == -1 && foodGetter.TargetNutrition > foodGetter.Nutrition)
            foodGetter.StorageNutrition(recipeDef.ingredients.Sum(x => x.GetBaseCount()), ingredients);
        else if (extensionS.Nutrition != -1 && foodGetter.TargetNutrition > foodGetter.Nutrition)
            foodGetter.StorageNutrition(extensionS.Nutrition, ingredients);
        else Log.Message("[RkM] Not enough nutrition to make this recipe");
        return [];
    }
}