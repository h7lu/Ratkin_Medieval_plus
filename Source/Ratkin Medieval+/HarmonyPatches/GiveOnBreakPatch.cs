using HarmonyLib;
using Verse;

namespace RkM.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DestroyEquipment))]
public static class GiveOnBreak_EquipmentDestroyPatch
{
    [HarmonyPrefix]
    public static void DestroyEquipment_Prefix(ThingWithComps eq)
    {
        eq?.TryGetComp<RkM.CompGiveOnBreak>()?.TryGiveOnBreakBeforeDestroy(DestroyMode.Vanish);
    }
}

[HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
public static class GiveOnBreakPatch
{
    [HarmonyPrefix]
    public static void Destroy_Prefix(Thing __instance, DestroyMode mode)
    {
        if (__instance is ThingWithComps thingWithComps)
        {
            thingWithComps.TryGetComp<RkM.CompGiveOnBreak>()?.TryGiveOnBreakBeforeDestroy(mode);
        }
    }
}
