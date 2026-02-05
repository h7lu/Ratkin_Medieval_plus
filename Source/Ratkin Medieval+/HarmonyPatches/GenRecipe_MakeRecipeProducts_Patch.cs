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
        var tempExtension = recipeDef.GetModExtension<ModExtension_NutritionRecipe>();
        if (tempExtension == null || !(billGiver is Building_FoodGetter foodGetter) ) return;
        IEnumerable<Thing> productsToReplace = [];

        productsToReplace = tempExtension switch
        {
            ModExtension_NutritionConsumeRecipe extension => NutritionConsumeRecipe(recipeDef, foodGetter, extension),
            ModExtension_NutritionStorageRecipe extension => NutritionStorageRecipe(recipeDef, foodGetter, extension, ingredients),
            ModExtension_WithoutNutritionGetRecipe extension => WithoutNutritionGetRecipe(recipeDef, foodGetter, extension),
            _ => __result,
        };
        __result = productsToReplace;
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