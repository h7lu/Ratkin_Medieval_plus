using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RkM
{
    public class IngestionOutcomeDoer_GiveOrRemoveHediffWithConditions : IngestionOutcomeDoer
    {
        public enum Type : byte
        {
            Give, Remove
        }
        public class HediffDefCondition 
        {
            public HediffDef hediffDef;
            public Type type = Type.Have;
            public enum Type : byte
            {
                Have, NotHave
            }
        }
        
        public HediffDef hediffDef;
        public float severity = 1.0f;
        public Type type = Type.Give;
        public List<HediffDefCondition> HediffDefConditions; 
         protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            // 检查所有条件是否满足
            if (AreAllConditionsMet(pawn))
            {
                switch (type)
                {
                    case Type.Give:
                        GiveHediff(pawn);
                        break;
                    case Type.Remove:
                        RemoveHediff(pawn);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private bool AreAllConditionsMet(Pawn pawn)
        {
            // 如果没有条件，则默认满足
            if (HediffDefConditions == null || HediffDefConditions.Count == 0)
                return true;
            // 检查每个条件
            foreach (var condition in HediffDefConditions)
            {
                bool hasHediff = pawn.health.hediffSet.HasHediff(condition.hediffDef);
                // 检查条件是否满足
                if (condition.type == HediffDefCondition.Type.Have && !hasHediff)
                    return false;
                if (condition.type == HediffDefCondition.Type.NotHave && hasHediff)
                    return false;
            }
            
            return true;
        }
        
        private void GiveHediff(Pawn pawn)
        {
            if (hediffDef != null)
            {
                var hediff = pawn.health.AddHediff(hediffDef);
                hediff.Severity = severity;
            }
        }
        
        private void RemoveHediff(Pawn pawn)
        {
            if (hediffDef != null)
            {
                var hediffs = pawn.health.hediffSet.hediffs;
                var hediffsToRemove = hediffs.Where(hediff => hediff.def == hediffDef).ToList();
                foreach (var hediff in hediffsToRemove) pawn.health.RemoveHediff(hediff);
            }
        }
    }
}