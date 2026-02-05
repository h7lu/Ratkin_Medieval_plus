using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM
{
    /// <summary>
    /// Enum representing the type of stew to produce based on nutrition availability.
    /// </summary>
    

    /// <summary>
    /// CompProperties for the auto-cooking dispenser with dual nutrition tracking.
    /// Configures nutrition storage for vegetables and protein, and output stew types.
    /// </summary>
    public class CompProperties_DrawGlowMote : CompProperties
    {
        // ========== Glow settings ==========
        /// <summary>Whether to show a glow effect when fueled.</summary>
        public bool showGlowWhenFueled = true;

        /// <summary>Mote definition to use for the glow.</summary>
        public ThingDef glowMoteDef;
        
        /// <summary>Draw offset for the glow effect.</summary>
        public Vector3 glowDrawOffset = new Vector3(0f, 0f, -0.1f);

        public CompProperties_DrawGlowMote()
        {
            compClass = typeof(CompDrawGlowMote);
        }

        // public override void ResolveReferences(ThingDef parentDef)
        // {
        //     base.ResolveReferences(parentDef);
        //     ingredientFilter?.ResolveReferences();
        // }
        //
        // /// <summary>
        // /// Gets the nutrition required per unit of a given stew type.
        // /// For mixed stew, requires half from each type.
        // /// </summary>
        // public float GetNutritionPerUnit(StewType stewType)
        // {
        //     ThingDef stewDef = GetStewDef(stewType);
        //     if (stewDef == null) return 0.5f; // Default fallback
        //     return stewDef.GetStatValueAbstract(StatDefOf.Nutrition);
        // }

        /// <summary>
        /// Gets the appropriate stew def for a given stew type.
        /// </summary>
        
    }

    /// <summary>
    /// Component that handles auto-cooking functionality for the Big Pot.
    /// Stores dual nutrition (vegetable and protein), cooks over time, and dispenses appropriate stews.
    /// </summary>
    public class CompDrawGlowMote : ThingComp
    {

        private float cookingProgress;

        // UI textures for the nutrition bars
        private static readonly Texture2D VegBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.2f)); // Green
        private static readonly Texture2D ProteinBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.3f, 0.2f)); // Red/Brown
        private static readonly Texture2D BarBgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));
        
        private Mote glowMote;

        public CompProperties_DrawGlowMote Props => (CompProperties_DrawGlowMote)props;
        public bool HasFuel
        {
            get
            {
                var refuelable = parent.GetComp<CompRefuelable>();
                return refuelable is { HasFuel: true };
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (Props.showGlowWhenFueled && Props.glowMoteDef != null)
            {
                if (HasFuel)
                {
                    if (glowMote == null || glowMote.Destroyed) glowMote = MoteMaker.MakeAttachedOverlay(parent, Props.glowMoteDef, Props.glowDrawOffset);
                    glowMote.Maintain();
                }
            }
        }
    }
}
