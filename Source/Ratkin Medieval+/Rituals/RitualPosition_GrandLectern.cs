using RimWorld;
using Verse;

namespace RkM
{
    /// <summary>
    /// Positions the teacher-role pawn at the cell in front of a nearby
    /// RKM_GrandLectern, mirroring what the vanilla RitualPosition_Lectern
    /// does for the Ideology Lectern.
    /// </summary>
    public class RitualPosition_GrandLectern : RitualPosition_ThingDef
    {
        protected override ThingDef ThingDef => RkMDefOf.RKM_GrandLectern;
    }
}
