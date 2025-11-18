using UnityEngine;
using Verse;

namespace RkM
{
    public class CompProperties_ColoredFacilityLink : CompProperties_Facility
    {
        public Color linkColor = Color.yellow;

        public CompProperties_ColoredFacilityLink()
        {
            this.compClass = typeof(CompColoredFacilityLink);
        }
    }
}