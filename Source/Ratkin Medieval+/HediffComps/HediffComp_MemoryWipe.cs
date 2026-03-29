using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM
{
    public class HediffCompProperties_MemoryWipe : HediffCompProperties
    {
        public int wipeIntervalTicks = 300;         
        public int totalWipeDurationTicks = 3600;    
        public bool clearSocialMemories = true;      
        public bool clearCulturalMemories = true;    
        public bool clearSkillMemories = false;      
        public bool resetIdeology = true;            
        public bool removeLoyaltyMark = true;        
        public float memoryWipeIntensity = 1.0f;    

        public HediffCompProperties_MemoryWipe()
        {
            this.compClass = typeof(HediffComp_MemoryWipe);
        }
    }

    public class HediffComp_MemoryWipe : HediffComp
    {
        public HediffCompProperties_MemoryWipe Props => (HediffCompProperties_MemoryWipe)props;
        private int ticksSinceLastWipe = 0;
        private int totalWipeTicks = 0;

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticksSinceLastWipe++;
            totalWipeTicks++;
            if (ticksSinceLastWipe >= Props.wipeIntervalTicks)
            {
                PerformMemoryWipe();
                ticksSinceLastWipe = 0;
            }

            if (totalWipeTicks >= Props.totalWipeDurationTicks)
            {
                severityAdjustment = -1f; 
            }
        }

        private void PerformMemoryWipe()
        {
            Pawn pawn = parent.pawn;

            if (Props.clearSocialMemories)
            {
                ClearSocialMemoriesGradually(pawn);
            }

            if (Props.clearCulturalMemories)
            {
                ClearCulturalMemoriesGradually(pawn);
            }

            if (Props.removeLoyaltyMark)
            {
                RemoveLoyaltyMark(pawn);
            }

            // 
            //if (pawn.Spawned)
            //{
            //    MoteMaker.MakeStaticMote(pawn.Position, pawn.Map, ThingDefOf.Mote_PsycastPsychicEffect, 1.0f);
            //}
        }

        private void ClearSocialMemoriesGradually(Pawn pawn)
        {
            if (pawn.needs?.mood?.thoughts?.memories?.Memories != null)
            {
                var memories = pawn.needs.mood.thoughts.memories.Memories.ToList();
                var memoriesToRemove = memories.Where(m => Rand.Chance(0.3f)).Take(2).ToList();

                foreach (var memory in memoriesToRemove)
                {
                    pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
                }
            }

            if (pawn.relations?.DirectRelations != null)
            {
                var relations = pawn.relations.DirectRelations.ToList();
                foreach (var relation in relations.Where(r => Rand.Chance(0.2f)))
                {
                    relation.startTicks = Find.TickManager.TicksGame -
                        Mathf.RoundToInt((Find.TickManager.TicksGame - relation.startTicks) * 0.8f);
                }
            }
        }

        private void ClearCulturalMemoriesGradually(Pawn pawn)
        {
            if (Props.resetIdeology && pawn.ideo != null)
            {
                float reductionAmount = 0.1f;
                pawn.ideo.OffsetCertainty(-reductionAmount);
            }
        }

        private void RemoveLoyaltyMark(Pawn pawn)
        {
            if (pawn.guest != null)
            {
                pawn.guest.resistance = Mathf.Max(0f, pawn.guest.resistance - 10f);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksSinceLastWipe, "ticksSinceLastWipe", 0);
            Scribe_Values.Look(ref totalWipeTicks, "totalWipeTicks", 0);
        }
    }
}
