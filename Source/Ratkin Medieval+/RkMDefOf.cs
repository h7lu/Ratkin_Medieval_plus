// This file is kept for potential future use but currently empty

using RimWorld;
using Verse;

namespace RkM
{
    [DefOf]
    public static class RkMDefOf
    {
        // Empty - teaching ability system removed in favor of lectern gizmo approach
        public static JobDef RkM_PlaySong;
        public static JobDef RkM_DoBillFoodGetter;
		public static JobDef RkM_EatFromBigPot;
        public static JobDef RkM_FillBigPot;

        static RkMDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RkMDefOf));
        }
    }
}