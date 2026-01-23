using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RkM.HarmonyPatches;

[HarmonyPatch(typeof(Building_NutrientPasteDispenser), "CanDispenseNow", MethodType.Getter)]
public static class Building_NutrientPasteDispenser_CanDispenseNow
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, Building_NutrientPasteDispenser __instance)
    {
        if (__instance is Building_FoodGetter getter)
        {
            Log.Message("CanDispenseNow");
            __result = getter.CanDispenseNow_New;
            return false;
        }
        Log.Message("[Harmony] MO.Getter: Building_NutrientPasteDispenser is not a Building_FoodGetter!");
        return true;
    }
}
