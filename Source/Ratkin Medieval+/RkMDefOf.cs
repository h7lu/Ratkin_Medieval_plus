using RimWorld;
using Verse;

namespace RkM
{
    [DefOf]
    public static class RkMDefOf
    {
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