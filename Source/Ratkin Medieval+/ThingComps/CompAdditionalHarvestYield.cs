using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RkM;

public class CompAdditionalHarvestYield : ThingComp
{
    public CompProperties_AdditionalHarvestYield Props => (CompProperties_AdditionalHarvestYield)props;
    public override IEnumerable<ThingDefCountClass> GetAdditionalHarvestYield() => Props.additionalHarvestYield;
}
public class CompProperties_AdditionalHarvestYield : CompProperties
{
    public List<ThingDefCountClass> additionalHarvestYield = [];
    public CompProperties_AdditionalHarvestYield() => compClass = typeof(CompAdditionalHarvestYield);
}