using RimWorld;
using Verse;

namespace RkM
{
    /// <summary>
    /// Thin subclass of Precept_Ritual that fixes two runtime issues:
    ///
    /// 1. GenerateNameRaw override:
    ///    Vanilla GenerateNameRaw resolves ideo grammar (memes, style.RitualNameMaker)
    ///    which is null in a non-Ideology ideo at runtime, causing a NullReferenceException
    ///    inside Precept.Init → RegenerateId → GenerateNameRaw.
    ///    Returning def.label directly is safe and localisation-friendly.
    ///
    /// 2. ShouldShowGizmo:
    ///    After FindOrCreateRitual injects the precept into the ideo, vanilla
    ///    Thing.GetGizmos iterates ALL ideo rituals and calls ShouldShowGizmo every
    ///    frame the building is selected.  The vanilla implementation accesses
    ///    obligationTargetFilter / behavior which are null for our runtime-injected
    ///    precept, crashing the gizmo iterator BEFORE ThingWithComps ever yields
    ///    comp gizmos — making our CompRitualStarter gizmo invisible.
    ///    ShouldShowGizmo is NOT virtual, so it cannot be overridden here.
    ///    It is suppressed via a Harmony prefix patch in
    ///    HarmonyPatches/TeachingRitualPatches.cs instead.
    /// </summary>
    public class Precept_TeachingRitual : Precept_Ritual
    {
        public override string GenerateNameRaw()
        {
            return def?.label ?? "teaching ritual";
        }
    }
}
