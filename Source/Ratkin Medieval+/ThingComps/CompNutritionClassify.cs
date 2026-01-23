using System.Collections.Generic;
using System.Xml;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM;

public class CompNutritionClassify : ThingComp
{
    private float vegNutrition;
    private float proteinNutrition;
    public float VegNutrition => vegNutrition;
    public float ProteinNutrition => proteinNutrition;
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref vegNutrition, "vegNutrition", 0f);
        Scribe_Values.Look(ref proteinNutrition, "proteinNutrition", 0f);
    }
    public float VegNutritionPct
    {
        get
        {
            if (storageComp == null) return 0f;
            return vegNutrition / storageComp.Props.maxNutrition;
        }
    }

    public float ProteinNutritionPct
    {
        get
        {
            if (storageComp == null) return 0f;
            return proteinNutrition / storageComp.Props.maxNutrition;
        }
    }

    public CompNutritionStorge storageComp;
    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        storageComp = parent.TryGetComp<CompNutritionStorge>();
    }

    public CompProperties_NutritionClassify Props => (CompProperties_NutritionClassify)props;

    public virtual void NotifyNutritionAdded(float nutrition, List<Thing> foodItems)
    {
        AddNutrition(nutrition, foodItems);
    }
    public virtual void NotifyNutritionCleaned()
    {
        CleanNutrition();
    }
    public virtual void NotifyNutritionConsumed(float nutrition)
    {
        ConsumeNutritionForStew(nutrition, AvailableStewType);
    }
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        // Debug: Add nutrition
        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Add 5 veg nutrition",
                action = () => AddNutritionDirect(5f, false)
            };

            yield return new Command_Action
            {
                defaultLabel = "DEV: Add 5 protein nutrition",
                action = () => AddNutritionDirect(5f, true)
            };
        }
    }
    public virtual void CleanNutrition ()
    {
        vegNutrition = 0f;
        proteinNutrition = 0f;
    }
    public virtual byte AvailableStewType
    {
        get
        {
            bool hasVeg = vegNutrition > 0f;
            bool hasProtein = proteinNutrition > 0f;
            // Priority 1: Mixed (if ANY of both exist)
            if (hasVeg && hasProtein)
                return (byte)StewType.Mixed;
            // Priority 2: Single type
            if (hasVeg) return (byte)StewType.Vegetable;
            if (hasProtein) return (byte)StewType.Meat;
            // Priority 3: Hot Water (when empty)
            return (byte)StewType.HotWater;
        }
    }
    public virtual void ConsumeNutritionForStew(float nutrition,byte stewType)
    {
        switch (stewType)
        {
            case (int)StewType.Vegetable: vegNutrition -= nutrition;
                break;
            case (int)StewType.Meat: proteinNutrition -= nutrition;
                break;
            case (int)StewType.Mixed: float half = nutrition / 2f;
                vegNutrition -= half;
                proteinNutrition -= half;
                break;
            case (int)StewType.HotWater: // Hot water consumes nothing
                break;
        }
        vegNutrition = Mathf.Max(0, vegNutrition);
        proteinNutrition = Mathf.Max(0, proteinNutrition);
    }
    public virtual ThingDef GetStewDef(byte index)
    {
        var stewType = Props.stewDefs.FirstOrFallback(x => x.index == index, null);
        if (stewType!= null) return stewType.def;
        Log.Error($"[RkM] NutritionClassify: No stew def for index {index}!");
        return null;
    }
    public enum StewType : byte
    {
        None,
        Vegetable,
        Meat,
        Mixed,
        HotWater
    }
    /// <summary>
    /// Determine display stew type.
    /// If we have both, Mixed. If only one, that type.
    /// If neither, it's None (which displays water mask).
    /// </summary>
    public virtual byte DisplayStewType
    {
        get
        {
            bool hasVeg = vegNutrition > 0.01f;
            bool hasProtein = proteinNutrition > 0.01f;
            
            if (hasVeg && hasProtein) return (byte)StewType.Mixed;
            if (hasVeg) return (byte)StewType.Vegetable;
            if (hasProtein) return (byte)StewType.Meat;
            return (byte)StewType.None;
        }
    }
    public virtual bool CanCook => true;
    public virtual bool CanDispenseNow => true;
    public override void PostDraw()
    {
        base.PostDraw();
        Vector3 drawPos = parent.DrawPos;
            
        // Draw the appropriate mask based on stew type
        var maskToDraw = Props.stewDefs.FirstOrDefault(x => x.index == DisplayStewType).MaskGraphic(Props.maskDrawSize);
        if (maskToDraw != null&& maskToDraw.MatSingle != BaseContent.BadMat)
        {
            // Rotate offset with the building
            Vector3 rotOffset = Props.maskDrawOffset.RotatedBy(parent.Rotation);
            Vector3 maskPos = drawPos + rotOffset;
            maskPos.y += 0.01f; // Slightly above the base
            maskToDraw.Draw(maskPos, parent.Rotation, parent);
        }
    }
    /// <summary>
    /// Determines if a food item is protein-based (meat, eggs, fish) or vegetable-based.
    /// </summary>
    public static bool IsProteinFood(ThingDef foodDef)
    {
        if (foodDef == null) return false;
        // Check if it's raw meat
        if (foodDef.IsMeat) return true;
        // Check if it's an egg
        if (foodDef.IsEgg) return true;
        // Check for fish category (if Odyssey DLC is present)
        if (ThingCategoryDefOf.Fish != null && foodDef.IsWithinCategory(ThingCategoryDefOf.Fish))
            return true;
        // Default: not protein
        return false;
    }
    public virtual float AddNutrition(float nutrition, List<Thing> foodItems)
    {
        if (foodItems == null || foodItems.Count == 0 || nutrition <= 0)return 0f;
        var vegNut = 0f;
        var proteinNut = 0f;
        foreach (var foodItem in foodItems)
        {
            if (!foodItem.def.IsNutritionGivingIngestible)
                continue;
            bool isProtein = IsProteinFood(foodItem.def);
            float nutritionAvailable = foodItem.GetStatValue(StatDefOf.Nutrition) * foodItem.stackCount;
            if (nutritionAvailable <= 0) continue;
            // Apply nutrition change and dilution
            if (isProtein) proteinNut += nutritionAvailable;
            else vegNut += nutritionAvailable;
            
        }
        if (vegNut > 0 && proteinNut > 0)
        {
            var sum = vegNut + proteinNut;
            var vegNut2 = nutrition * (vegNut / sum);
            AddNutritionDirect(vegNut2, false);
            AddNutritionDirect(sum - vegNut2, true);
        }else if (vegNut > 0) AddNutritionDirect(nutrition, false);
        else if (proteinNut > 0) AddNutritionDirect(nutrition, true);
        else return 0f;
        return nutrition;
    }
    public void AddNutritionDirect(float amount, bool isProtein)
    {
        if (amount <= 0) return;
        float actualAdded = amount;
        if (isProtein) proteinNutrition += actualAdded;
        else vegNutrition += actualAdded;
    }
    public override string CompInspectStringExtra()
    {
        string result = "";

        // Vegetable nutrition
        result += "RkM_VegNutrition".Translate() + ": " + vegNutrition.ToString("F1");
            
        // Protein nutrition
        result += "\n" + "RkM_ProteinNutrition".Translate() + ": " + proteinNutrition.ToString("F1");

        return result;
    }
}
public class CompProperties_NutritionClassify : CompProperties
{
    public CompProperties_NutritionClassify() => compClass = typeof(CompNutritionClassify);
    public List<ThingDefAndPathIndexClass> stewDefs;
    /// <summary>Draw size for the mask overlay.</summary>
    public Vector2 maskDrawSize = new Vector2(1f, 1f);
        
    /// <summary>Draw offset for the mask overlay (x, y, z).</summary>
    public Vector3 maskDrawOffset = Vector3.zero;
}

public class ThingDefAndPathIndexClass
{
    public byte index;
    public ThingDef def;
    public string path;
    [field:Unsaved]private Graphic field;
    public Graphic MaskGraphic(Vector2 size) =>field??=GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Transparent, size, Color.white);
}


