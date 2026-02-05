using RimWorld;
using Verse;

namespace RkM;

public class ColdPlant : Plant 
{
    protected override float LeaflessTemperatureThresh => def.plant.minGrowthTemperature + Rand.RangeSeeded(-40f, -36f, thingIDNumber ^ 838051265);
}