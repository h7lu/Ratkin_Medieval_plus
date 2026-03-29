using HarmonyLib;
using RimWorld;
using Verse;

namespace RkM.HarmonyPatches
{
    /// <summary>
    /// Harmony prefix for Precept_Ritual.ShouldShowGizmo.
    ///
    /// ShouldShowGizmo is not virtual in RimWorld 1.6, so it cannot be overridden
    /// in Precept_TeachingRitual.  Without this patch, vanilla Thing.GetGizmos calls
    /// ShouldShowGizmo on every ideo ritual every frame while RKM_GrandLectern is
    /// selected.  Our runtime-injected Precept_TeachingRitual has null behavior and
    /// obligationTargetFilter at that point, which causes a NullReferenceException
    /// inside the iterator — aborting it before ThingWithComps can yield the
    /// CompRitualStarter gizmo, so the "Start lecture" button never appears.
    ///
    /// Returning false for Precept_TeachingRitual instances is safe because
    /// CompRitualStarter on RKM_GrandLectern supplies the gizmo directly.
    /// </summary>
    [HarmonyPatch(typeof(Precept_Ritual), nameof(Precept_Ritual.ShouldShowGizmo))]
    public static class Patch_PreceptRitual_ShouldShowGizmo
    {
        [HarmonyPrefix]
        public static bool Prefix(Precept_Ritual __instance, ref bool __result)
        {
            if (__instance is Precept_TeachingRitual)
            {
                __result = false;
                return false; // skip original
            }
            return true; // run original for all other rituals
        }
    }
}
