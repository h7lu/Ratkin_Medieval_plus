using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM
{
//     public enum StewType
//     {
//         None,
//         Vegetable,
//         Meat,
//         Mixed,
//         HotWater
//     }

//     public class CompProperties_AutoCookDispenser : CompProperties
//     {
//         public float nutritionCapacity = 15f;

//         public ThingDef vegetableStewDef;

//         public ThingDef meatStewDef;

//         public ThingDef mixedStewDef;

//         public ThingDef hotWaterDef;

//         public int ticksPerUnit = 5000;

//         public bool allowDirectConsumption = true;

//         public ThingFilter ingredientFilter;

//         public string waterMaskPath = "Things/Building/potMasks/RkM_Water_potMask";
        
//         public string vegStewMaskPath = "Things/Building/potMasks/RkM_VegetableStew_potMask";
        
//         public string meatStewMaskPath = "Things/Building/potMasks/RkM_MeatStew_potMask";
        
//         public string mixedStewMaskPath = "Things/Building/potMasks/RkM_MixedStew_potMask";
        
//         public Vector2 maskDrawSize = new Vector2(1f, 1f);
        
//         public Vector3 maskDrawOffset = Vector3.zero;

//         public bool showGlowWhenFueled = true;

//         public ThingDef glowMoteDef;
        
//         public Vector3 glowDrawOffset = new Vector3(0f, 0f, -0.1f);

//         public CompProperties_AutoCookDispenser()
//         {
//             compClass = typeof(CompAutoCookDispenser);
//         }

//         public override void ResolveReferences(ThingDef parentDef)
//         {
//             base.ResolveReferences(parentDef);
//             ingredientFilter?.ResolveReferences();
//         }

//         public float GetNutritionPerUnit(StewType stewType)
//         {
//             ThingDef stewDef = GetStewDef(stewType);
//             if (stewDef == null) return 0.5f;
//             return stewDef.GetStatValueAbstract(StatDefOf.Nutrition);
//         }

//         public ThingDef GetStewDef(StewType stewType)
//         {
//             switch (stewType)
//             {
//                 case StewType.Vegetable: return vegetableStewDef;
//                 case StewType.Meat: return meatStewDef;
//                 case StewType.Mixed: return mixedStewDef;
//                 case StewType.HotWater: return hotWaterDef;
//                 default: return null;
//             }
//         }
//     }

//     public class CompAutoCookDispenser : ThingComp
//     {
//         private float vegNutrition;
//         private float proteinNutrition;
//         private float cookingProgress;
//         private StewType currentlyCooked = StewType.None;

//         private static readonly Texture2D VegBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.2f));
//         private static readonly Texture2D ProteinBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.3f, 0.2f));
//         private static readonly Texture2D BarBgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));

//         private Graphic waterMaskGraphic;
//         private Graphic vegStewMaskGraphic;
//         private Graphic meatStewMaskGraphic;
//         private Graphic mixedStewMaskGraphic;
//         private Mote glowMote;
//         private bool graphicsInitialized;

//         public CompProperties_AutoCookDispenser Props => (CompProperties_AutoCookDispenser)props;

//         public float VegNutrition => vegNutrition;
//         public float ProteinNutrition => proteinNutrition;
//         public float VegNutritionPct => vegNutrition / Props.nutritionCapacity;
//         public float ProteinNutritionPct => proteinNutrition / Props.nutritionCapacity;
//         public float CookingProgressPct => cookingProgress / Props.ticksPerUnit;

//         public StewType DisplayStewType
//         {
//             get
//             {
//                 bool hasVeg = vegNutrition > 0.01f;
//                 bool hasProtein = proteinNutrition > 0.01f;

//                 if (hasVeg && hasProtein)
//                     return StewType.Mixed;
//                 if (hasVeg)
//                     return StewType.Vegetable;
//                 if (hasProtein)
//                     return StewType.Meat;
//                 return StewType.None;
//             }
//         }
        
//         public StewType AvailableStewType
//         {
//             get
//             {
//                 bool hasVeg = vegNutrition > 0f;
//                 bool hasProtein = proteinNutrition > 0f;

//                 if (hasVeg && hasProtein)
//                     return StewType.Mixed;

//                 if (hasVeg)
//                     return StewType.Vegetable;
//                 if (hasProtein)
//                     return StewType.Meat;

//                 return StewType.HotWater;
//             }
//         }

//         public bool HasEnoughNutrition => true;
        
//         public bool CanDispenseNow => IsPoweredAndFueled && CookingProgressPct >= 0.9f;

//         private bool IsPoweredAndFueled
//         {
//             get
//             {
//                 var refuelable = parent.GetComp<CompRefuelable>();
//                 if (refuelable != null && !refuelable.HasFuel)
//                     return false;

//                 return true;
//             }
//         }

//         private bool HasFuel
//         {
//             get
//             {
//                 var refuelable = parent.GetComp<CompRefuelable>();
//                 return refuelable != null && refuelable.HasFuel;
//             }
//         }

//         private void InitializeGraphics()
//         {
//             if (graphicsInitialized) return;
//             graphicsInitialized = true;

//             Vector2 size = Props.maskDrawSize;

//             if (!Props.waterMaskPath.NullOrEmpty())
//             {
//                 waterMaskGraphic = GraphicDatabase.Get<Graphic_Single>(
//                     Props.waterMaskPath, ShaderDatabase.Transparent, size, Color.white);
//             }
//             if (!Props.vegStewMaskPath.NullOrEmpty())
//             {
//                 vegStewMaskGraphic = GraphicDatabase.Get<Graphic_Single>(
//                     Props.vegStewMaskPath, ShaderDatabase.Transparent, size, Color.white);
//             }
//             if (!Props.meatStewMaskPath.NullOrEmpty())
//             {
//                 meatStewMaskGraphic = GraphicDatabase.Get<Graphic_Single>(
//                     Props.meatStewMaskPath, ShaderDatabase.Transparent, size, Color.white);
//             }
//             if (!Props.mixedStewMaskPath.NullOrEmpty())
//             {
//                 mixedStewMaskGraphic = GraphicDatabase.Get<Graphic_Single>(
//                     Props.mixedStewMaskPath, ShaderDatabase.Transparent, size, Color.white);
//             }
//         }

//         public override void PostDraw()
//         {
//             base.PostDraw();
//             InitializeGraphics();

//             Vector3 drawPos = parent.DrawPos;
            
//             Graphic maskToDraw = null;
//             StewType displayType = DisplayStewType;

//             switch (displayType)
//             {
//                 case StewType.None:
//                     maskToDraw = waterMaskGraphic;
//                     break;
//                 case StewType.Vegetable:
//                     maskToDraw = vegStewMaskGraphic;
//                     break;
//                 case StewType.Meat:
//                     maskToDraw = meatStewMaskGraphic;
//                     break;
//                 case StewType.Mixed:
//                     maskToDraw = mixedStewMaskGraphic;
//                     break;
//             }

//             if (maskToDraw != null)
//             {
//                 Vector3 rotOffset = Props.maskDrawOffset.RotatedBy(parent.Rotation);
//                 Vector3 maskPos = drawPos + rotOffset;
//                 maskPos.y += 0.01f;
//                 maskToDraw.Draw(maskPos, parent.Rotation, parent);
//             }
//         }

//         public override void PostExposeData()
//         {
//             base.PostExposeData();
//             Scribe_Values.Look(ref vegNutrition, "vegNutrition", 0f);
//             Scribe_Values.Look(ref proteinNutrition, "proteinNutrition", 0f);
//             Scribe_Values.Look(ref cookingProgress, "cookingProgress", 0f);
//             Scribe_Values.Look(ref currentlyCooked, "currentlyCooked", StewType.None);
//         }

//         public override void CompTick()
//         {
//             base.CompTick();

//             if (Props.showGlowWhenFueled && Props.glowMoteDef != null)
//             {
//                 if (IsPoweredAndFueled)
//                 {
//                     if (glowMote == null || glowMote.Destroyed)
//                     {
//                         glowMote = MoteMaker.MakeAttachedOverlay(parent, Props.glowMoteDef, Props.glowDrawOffset);
//                     }
//                     glowMote.Maintain();
//                 }
//             }

//             if (!IsPoweredAndFueled)
//             {
//                 if (cookingProgress > 0)
//                     cookingProgress = Mathf.Max(0, cookingProgress - 1);
//                 return;
//             }

//             StewType canCook = AvailableStewType;
            
//             if (currentlyCooked != StewType.None && currentlyCooked != canCook && canCook == StewType.None)
//             {
//                 currentlyCooked = StewType.None;
//                 cookingProgress = 0;
//                 return;
//             }

//             if (canCook != StewType.None)
//             {
//                 if (currentlyCooked == StewType.None)
//                     currentlyCooked = canCook;

//                 if (cookingProgress < Props.ticksPerUnit)
//                 {
//                     cookingProgress++;

//                     var refuelable = parent.GetComp<CompRefuelable>();
//                     refuelable?.Notify_UsedThisTick();
//                 }
//             }
//         }

//         public static bool IsProteinFood(ThingDef foodDef)
//         {
//             if (foodDef == null) return false;

//             if (foodDef.IsMeat) return true;

//             if (foodDef.IsEgg) return true;

//             if (ThingCategoryDefOf.Fish != null && foodDef.IsWithinCategory(ThingCategoryDefOf.Fish))
//                 return true;

//             return false;
//         }

//         public void AddNutritionDirect(float amount, bool isProtein)
//         {
//             if (amount <= 0) return;

//             float oldNutrition = vegNutrition + proteinNutrition;
//             float currentSpecific = isProtein ? proteinNutrition : vegNutrition;
//             float spaceRemaining = Props.nutritionCapacity - currentSpecific;

//             if (spaceRemaining <= 0) return;

//             float actualAdded = Mathf.Min(amount, spaceRemaining);

//             if (isProtein)
//                 proteinNutrition += actualAdded;
//             else
//                 vegNutrition += actualAdded;

//             float newNutrition = vegNutrition + proteinNutrition;
//             if (cookingProgress > 0 && newNutrition > 0 && newNutrition > oldNutrition)
//             {
//                 cookingProgress *= ((oldNutrition + 1)/ (newNutrition + 1));
//             }
//         }

//         public float AddNutrition(Thing foodItem)
//         {
//             if (foodItem == null || !foodItem.def.IsNutritionGivingIngestible)
//                 return 0f;

//             bool isProtein = IsProteinFood(foodItem.def);
//             float currentSpecific = isProtein ? proteinNutrition : vegNutrition;
//             float spaceRemaining = Props.nutritionCapacity - currentSpecific;

//             float nutritionAvailable = foodItem.GetStatValue(StatDefOf.Nutrition) * foodItem.stackCount;
//             if (nutritionAvailable <= 0 || spaceRemaining <= 0)
//                 return 0f;

//             float actualAdded = Mathf.Min(nutritionAvailable, spaceRemaining);

//             AddNutritionDirect(actualAdded, isProtein);

//             float nutritionPerItem = foodItem.GetStatValue(StatDefOf.Nutrition);
//             int itemsConsumed = Mathf.CeilToInt(actualAdded / nutritionPerItem);
//             itemsConsumed = Mathf.Min(itemsConsumed, foodItem.stackCount);

//             if (itemsConsumed >= foodItem.stackCount)
//             {
//                 foodItem.Destroy();
//             }
//             else
//             {
//                 foodItem.stackCount -= itemsConsumed;
//             }

//             return actualAdded;
//         }

//         private void ProduceFoodBatch(StewType stewType)
//         {
//             ThingDef outputDef = Props.GetStewDef(stewType);
//             if (outputDef == null)
//             {
//                 Log.Error($"[RkM] AutoCookDispenser: No stew def for type {stewType}!");
//                 return;
//             }

//             ConsumeNutritionForStew(stewType);

//             Thing food = ThingMaker.MakeThing(outputDef);
//             food.stackCount = 1;

//             IntVec3 spawnPos = parent.InteractionCell;
//             if (!spawnPos.IsValid || !spawnPos.InBounds(parent.Map))
//                 spawnPos = parent.Position;

//             GenPlace.TryPlaceThing(food, spawnPos, parent.Map, ThingPlaceMode.Near);
//         }

//         private void ConsumeNutritionForStew(StewType stewType)
//         {
//             float nutrition = Props.GetNutritionPerUnit(stewType);
            
//             switch (stewType)
//             {
//                 case StewType.Vegetable:
//                     vegNutrition -= nutrition;
//                     break;
//                 case StewType.Meat:
//                     proteinNutrition -= nutrition;
//                     break;
//                 case StewType.Mixed:
//                     float half = nutrition / 2f;
//                     vegNutrition -= half;
//                     proteinNutrition -= half;
//                     break;
//                 case StewType.HotWater:
//                     break;
//             }

//             vegNutrition = Mathf.Max(0, vegNutrition);
//             proteinNutrition = Mathf.Max(0, proteinNutrition);
//         }

//         public Thing DispenseFood()
//         {
//             StewType stewType = AvailableStewType;
//             if (!CanDispenseNow || stewType == StewType.None)
//                 return null;

//             ThingDef outputDef = Props.GetStewDef(stewType);
//             if (outputDef == null)
//                 return null;

//             ConsumeNutritionForStew(stewType);

//             Thing food = ThingMaker.MakeThing(outputDef);
//             food.stackCount = 1;
//             return food;
//         }

//         public override string CompInspectStringExtra()
//         {
//             string result = "";

//             result += "RkM_VegNutrition".Translate() + ": " + vegNutrition.ToString("F1") + " / " + Props.nutritionCapacity.ToString("F0");
            
//             result += "\n" + "RkM_ProteinNutrition".Translate() + ": " + proteinNutrition.ToString("F1") + " / " + Props.nutritionCapacity.ToString("F0");

//             StewType available = AvailableStewType;
//             if (available != StewType.None && IsPoweredAndFueled)
//             {
//                 string stewName = ("RkM_StewType_" + available.ToString()).Translate();
//                 result += "\n" + "RkM_CookingStew".Translate(stewName) + ": " + CookingProgressPct.ToStringPercent();
//             }
//             else if (!IsPoweredAndFueled)
//             {
//                 result += "\n" + "RkM_NeedsFuel".Translate();
//             }
//             else
//             {
//                 result += "\n" + "RkM_NeedsIngredients".Translate();
//             }

//             return result;
//         }

//         public override IEnumerable<Gizmo> CompGetGizmosExtra()
//         {
//             foreach (var gizmo in base.CompGetGizmosExtra())
//                 yield return gizmo;

//             if (DebugSettings.godMode)
//             {
//                 yield return new Command_Action
//                 {
//                     defaultLabel = "DEV: Add 5 veg nutrition",
//                     action = () => AddNutritionDirect(5f, false)
//                 };

//                 yield return new Command_Action
//                 {
//                     defaultLabel = "DEV: Add 5 protein nutrition",
//                     action = () => AddNutritionDirect(5f, true)
//                 };

//                 yield return new Command_Action
//                 {
//                     defaultLabel = "DEV: Finish cooking",
//                     action = () =>
//                     {
//                         StewType stewType = AvailableStewType;
//                         if (stewType != StewType.None)
//                         {
//                             ProduceFoodBatch(stewType);
//                         }
//                     }
//                 };
//             }
//         }
//     }
}
