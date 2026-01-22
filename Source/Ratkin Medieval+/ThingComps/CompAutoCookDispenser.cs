using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM
{
    /// <summary>
    /// Enum representing the type of stew to produce based on nutrition availability.
    /// </summary>
    public enum StewType
    {
        None,
        Vegetable,
        Meat,
        Mixed
    }

    /// <summary>
    /// CompProperties for the auto-cooking dispenser with dual nutrition tracking.
    /// Configures nutrition storage for vegetables and protein, and output stew types.
    /// </summary>
    public class CompProperties_AutoCookDispenser : CompProperties
    {
        /// <summary>Maximum nutrition that can be stored for each type (veg and protein).</summary>
        public float nutritionCapacity = 15f;

        /// <summary>The stew ThingDef produced when only vegetables are available.</summary>
        public ThingDef vegetableStewDef;

        /// <summary>The stew ThingDef produced when only meat/protein is available.</summary>
        public ThingDef meatStewDef;

        /// <summary>The stew ThingDef produced when both veg and protein are available.</summary>
        public ThingDef mixedStewDef;

        /// <summary>Work ticks required to produce one unit of food.</summary>
        public int ticksPerUnit = 5000; // About 2 hours

        /// <summary>Whether colonists can directly eat from this pot.</summary>
        public bool allowDirectConsumption = true;

        /// <summary>Filter for what raw foods can be added to the pot.</summary>
        public ThingFilter ingredientFilter;

        public CompProperties_AutoCookDispenser()
        {
            compClass = typeof(CompAutoCookDispenser);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            ingredientFilter?.ResolveReferences();
        }

        /// <summary>
        /// Gets the nutrition required per unit of a given stew type.
        /// For mixed stew, requires half from each type.
        /// </summary>
        public float GetNutritionPerUnit(StewType stewType)
        {
            ThingDef stewDef = GetStewDef(stewType);
            if (stewDef == null) return 0.5f; // Default fallback
            return stewDef.GetStatValueAbstract(StatDefOf.Nutrition);
        }

        /// <summary>
        /// Gets the appropriate stew def for a given stew type.
        /// </summary>
        public ThingDef GetStewDef(StewType stewType)
        {
            switch (stewType)
            {
                case StewType.Vegetable: return vegetableStewDef;
                case StewType.Meat: return meatStewDef;
                case StewType.Mixed: return mixedStewDef;
                default: return null;
            }
        }
    }

    /// <summary>
    /// Component that handles auto-cooking functionality for the Big Pot.
    /// Stores dual nutrition (vegetable and protein), cooks over time, and dispenses appropriate stews.
    /// </summary>
    public class CompAutoCookDispenser : ThingComp
    {
        private float vegNutrition;
        private float proteinNutrition;
        private float cookingProgress;
        private StewType currentlyCooked = StewType.None;

        // UI textures for the nutrition bars
        private static readonly Texture2D VegBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.2f)); // Green
        private static readonly Texture2D ProteinBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.3f, 0.2f)); // Red/Brown
        private static readonly Texture2D BarBgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));

        public CompProperties_AutoCookDispenser Props => (CompProperties_AutoCookDispenser)props;

        public float VegNutrition => vegNutrition;
        public float ProteinNutrition => proteinNutrition;
        public float VegNutritionPct => vegNutrition / Props.nutritionCapacity;
        public float ProteinNutritionPct => proteinNutrition / Props.nutritionCapacity;
        public float CookingProgressPct => cookingProgress / Props.ticksPerUnit;
        
        /// <summary>
        /// Determines the best stew type that can be made with current nutrition.
        /// Priority: Mixed > Vegetable/Meat (whichever has enough)
        /// </summary>
        public StewType AvailableStewType
        {
            get
            {
                float mixedNutrition = Props.GetNutritionPerUnit(StewType.Mixed);
                float halfNutrition = mixedNutrition / 2f;

                // Check for mixed stew first (requires both)
                if (vegNutrition >= halfNutrition && proteinNutrition >= halfNutrition)
                    return StewType.Mixed;

                // Check for vegetable stew
                float vegStewNutrition = Props.GetNutritionPerUnit(StewType.Vegetable);
                if (vegNutrition >= vegStewNutrition)
                    return StewType.Vegetable;

                // Check for meat stew
                float meatStewNutrition = Props.GetNutritionPerUnit(StewType.Meat);
                if (proteinNutrition >= meatStewNutrition)
                    return StewType.Meat;

                return StewType.None;
            }
        }

        public bool HasEnoughNutrition => AvailableStewType != StewType.None;
        
        /// <summary>
        /// Can dispense if we have enough nutrition, power/fuel, AND cooking is at least 90% complete.
        /// </summary>
        public bool CanDispenseNow => HasEnoughNutrition && IsPoweredAndFueled && CookingProgressPct >= 0.9f;

        private bool IsPoweredAndFueled
        {
            get
            {
                var refuelable = parent.GetComp<CompRefuelable>();
                if (refuelable != null && !refuelable.HasFuel)
                    return false;

                return true;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref vegNutrition, "vegNutrition", 0f);
            Scribe_Values.Look(ref proteinNutrition, "proteinNutrition", 0f);
            Scribe_Values.Look(ref cookingProgress, "cookingProgress", 0f);
            Scribe_Values.Look(ref currentlyCooked, "currentlyCooked", StewType.None);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!IsPoweredAndFueled)
            {
                if (cookingProgress > 0)
                    cookingProgress = Mathf.Max(0, cookingProgress - 1);
                return;
            }

            // Determine what we're cooking
            StewType canCook = AvailableStewType;
            
            // If we started cooking something and ingredients changed, reset
            if (currentlyCooked != StewType.None && currentlyCooked != canCook && canCook == StewType.None)
            {
                currentlyCooked = StewType.None;
                cookingProgress = 0;
                return;
            }

            // Progress cooking if we have nutrition
            if (canCook != StewType.None)
            {
                if (currentlyCooked == StewType.None)
                    currentlyCooked = canCook;

                // Stop incrementing if full
                if (cookingProgress < Props.ticksPerUnit)
                {
                    cookingProgress++;

                    var refuelable = parent.GetComp<CompRefuelable>();
                    refuelable?.Notify_UsedThisTick();
                }

                // If full, do nothing (wait for dispense)
                /*
                if (cookingProgress >= Props.ticksPerUnit)
                {
                    // No longer auto-producing items on ground
                    // ProduceFoodBatch(currentlyCooked);
                    // cookingProgress = 0;
                    // currentlyCooked = StewType.None;
                }
                */
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

        /// <summary>
        /// Helper to add nutrition directly and handle cooking progress dilution.
        /// </summary>
        public void AddNutritionDirect(float amount, bool isProtein)
        {
            if (amount <= 0) return;

            float oldNutrition = vegNutrition + proteinNutrition;
            float currentSpecific = isProtein ? proteinNutrition : vegNutrition;
            float spaceRemaining = Props.nutritionCapacity - currentSpecific;

            if (spaceRemaining <= 0) return;

            float actualAdded = Mathf.Min(amount, spaceRemaining);

            if (isProtein)
                proteinNutrition += actualAdded;
            else
                vegNutrition += actualAdded;

            // Reduce cooking progress based on dilution (Thermal Shock / Mixing)
            // Example: 90% progress with 10 nutrition. Add 5. Total 15.
            // New Progress = 90% * (10 / 15) = 60%.
            float newNutrition = vegNutrition + proteinNutrition;
            if (cookingProgress > 0 && newNutrition > 0 && newNutrition > oldNutrition)
            {
                cookingProgress *= (oldNutrition / newNutrition);
            }
        }

        /// <summary>
        /// Adds nutrition from a food item to the appropriate nutrition pool.
        /// </summary>
        public float AddNutrition(Thing foodItem)
        {
            if (foodItem == null || !foodItem.def.IsNutritionGivingIngestible)
                return 0f;

            bool isProtein = IsProteinFood(foodItem.def);
            float currentSpecific = isProtein ? proteinNutrition : vegNutrition;
            float spaceRemaining = Props.nutritionCapacity - currentSpecific;

            float nutritionAvailable = foodItem.GetStatValue(StatDefOf.Nutrition) * foodItem.stackCount;
            if (nutritionAvailable <= 0 || spaceRemaining <= 0)
                return 0f;

            // Calculate how much we can actually take
            float actualAdded = Mathf.Min(nutritionAvailable, spaceRemaining);

            // Apply nutrition change and dilution
            AddNutritionDirect(actualAdded, isProtein);

            // Consume the item stack
            float nutritionPerItem = foodItem.GetStatValue(StatDefOf.Nutrition);
            int itemsConsumed = Mathf.CeilToInt(actualAdded / nutritionPerItem);
            itemsConsumed = Mathf.Min(itemsConsumed, foodItem.stackCount);

            if (itemsConsumed >= foodItem.stackCount)
            {
                foodItem.Destroy();
            }
            else
            {
                foodItem.stackCount -= itemsConsumed;
            }

            return actualAdded;
        }

        /// <summary>
        /// Produces a batch of food based on the stew type and consumes the appropriate nutrition.
        /// </summary>
        private void ProduceFoodBatch(StewType stewType)
        {
            ThingDef outputDef = Props.GetStewDef(stewType);
            if (outputDef == null)
            {
                Log.Error($"[RkM] AutoCookDispenser: No stew def for type {stewType}!");
                return;
            }

            // Consume the appropriate nutrition
            ConsumeNutritionForStew(stewType);

            Thing food = ThingMaker.MakeThing(outputDef);
            food.stackCount = 1;

            IntVec3 spawnPos = parent.InteractionCell;
            if (!spawnPos.IsValid || !spawnPos.InBounds(parent.Map))
                spawnPos = parent.Position;

            GenPlace.TryPlaceThing(food, spawnPos, parent.Map, ThingPlaceMode.Near);
        }

        /// <summary>
        /// Consumes the appropriate amount of nutrition based on stew type.
        /// </summary>
        private void ConsumeNutritionForStew(StewType stewType)
        {
            float nutrition = Props.GetNutritionPerUnit(stewType);
            
            switch (stewType)
            {
                case StewType.Vegetable:
                    vegNutrition -= nutrition;
                    break;
                case StewType.Meat:
                    proteinNutrition -= nutrition;
                    break;
                case StewType.Mixed:
                    float half = nutrition / 2f;
                    vegNutrition -= half;
                    proteinNutrition -= half;
                    break;
            }

            vegNutrition = Mathf.Max(0, vegNutrition);
            proteinNutrition = Mathf.Max(0, proteinNutrition);
        }

        /// <summary>
        /// Dispenses a single serving directly to a pawn (for eating in place).
        /// </summary>
        public Thing DispenseFood()
        {
            StewType stewType = AvailableStewType;
            if (!CanDispenseNow || stewType == StewType.None)
                return null;

            ThingDef outputDef = Props.GetStewDef(stewType);
            if (outputDef == null)
                return null;

            ConsumeNutritionForStew(stewType);

            Thing food = ThingMaker.MakeThing(outputDef);
            food.stackCount = 1;
            return food;
        }

        public override string CompInspectStringExtra()
        {
            string result = "";

            // Vegetable nutrition
            result += "RkM_VegNutrition".Translate() + ": " + vegNutrition.ToString("F1") + " / " + Props.nutritionCapacity.ToString("F0");
            
            // Protein nutrition
            result += "\n" + "RkM_ProteinNutrition".Translate() + ": " + proteinNutrition.ToString("F1") + " / " + Props.nutritionCapacity.ToString("F0");

            // Current status
            StewType available = AvailableStewType;
            if (available != StewType.None && IsPoweredAndFueled)
            {
                string stewName = ("RkM_StewType_" + available.ToString()).Translate();
                result += "\n" + "RkM_CookingStew".Translate(stewName) + ": " + CookingProgressPct.ToStringPercent();
            }
            else if (!IsPoweredAndFueled)
            {
                result += "\n" + "RkM_NeedsFuel".Translate();
            }
            else
            {
                result += "\n" + "RkM_NeedsIngredients".Translate();
            }

            return result;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

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

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Finish cooking",
                    action = () =>
                    {
                        StewType stewType = AvailableStewType;
                        if (stewType != StewType.None)
                        {
                            ProduceFoodBatch(stewType);
                        }
                    }
                };
            }
        }
    }
}
