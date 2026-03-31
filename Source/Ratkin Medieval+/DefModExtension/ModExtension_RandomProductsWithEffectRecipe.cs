using System.Collections.Generic;
using Verse;

namespace RkM;

public abstract class ModExtension_RandomProductsWithEffectRecipe : DefModExtension
{
    public int maximumWeight = 100;
    public List<ThingsWithAccidents> weightConfigurations = [];
    public ThingDef defaultProduct;
    public int count = 1;
}

public class ThingsWithAccidents
{ 
    public List<ThingDefCountClass> things = [];
    public List<AccidentDef> accidents = [];
    public int weight;
}
