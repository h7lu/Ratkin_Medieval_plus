using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM
{
    public class CompProperties_DrawGlowMote : CompProperties
    {
        public bool showGlowWhenFueled = true;

        public ThingDef glowMoteDef;
        
        public Vector3 glowDrawOffset = new Vector3(0f, 0f, -0.1f);

        public CompProperties_DrawGlowMote()
        {
            compClass = typeof(CompDrawGlowMote);
        }
    }

    public class CompDrawGlowMote : ThingComp
    {
        private float cookingProgress;

        private static readonly Texture2D VegBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.8f, 0.2f));
        private static readonly Texture2D ProteinBarFilledTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.8f, 0.3f, 0.2f));
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
