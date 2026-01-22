using RimWorld;
using Verse;

namespace RkM
{
    [DefOf]
    public static class RkM_JobDefOf
    {
        public static JobDef RkM_EatFromBigPot;
        public static JobDef RkM_FillBigPot;

        static RkM_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RkM_JobDefOf));
        }
    }
}