using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM;

public class IngestionOutcomeDoer_TendWithNumLimit : IngestionOutcomeDoer
{
    public TendProperties tendProperties = new TendProperties_Num();
    public abstract class TendProperties
    {
        public virtual bool CanTendBleeding => true;
        public virtual int ShouldTendNum(int potentialCureNum) => 1;
    }

    public class TendProperties_Num : TendProperties
    {
        public int tendNum = 1;
        public bool canTendBleeding = true;
        public override bool CanTendBleeding => canTendBleeding;
        public override int ShouldTendNum(int potentialCureNum)
        {
            return Math.Min(potentialCureNum, tendNum);
        }
    }
    public class TendPropertiesPercent : TendProperties
    {
        public float tendPercent = 1.0f;
        public bool canTendBleeding = true;
        public int atLeastTendNum = 1;

        public override int ShouldTendNum(int potentialCureNum)
        {
            if (potentialCureNum <= atLeastTendNum) return potentialCureNum;
            var commonlyCureNum = Mathf.RoundToInt(potentialCureNum * tendPercent);
            if (commonlyCureNum < atLeastTendNum) return atLeastTendNum;
            return Math.Min(commonlyCureNum, potentialCureNum);
        }
    }

    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
    {
        HealWounds(pawn);
    }

    private void HealWounds(Pawn pawn)
    {
        // 获取所有非出血伤口
        List<Hediff_Injury> wounds = null;

        pawn.health.hediffSet.GetHediffs(ref wounds,
            h => (!h.Bleeding || tendProperties.CanTendBleeding) && h.Visible && h.IsTended() == false);

        if (wounds.Count == 0)
        {
            Messages.Message("NoWoundsToHeal".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent);
            return;
        }

        int woundsHealed = 0;
        int woundsToHeal = tendProperties.ShouldTendNum(wounds.Count);

        // 按严重程度排序，优先治疗严重伤口
        wounds = wounds.OrderByDescending(w => w.Severity).ToList();

        foreach (var wound in wounds)
        {
            if (woundsHealed >= woundsToHeal)
                break;

            // 治疗伤口（设置为已包扎）
            wound.Tended(0.9f, 1f, 0);
            woundsHealed++;

            // 添加治疗效果
            ///if (wound.Severity > 0.5f) wound.Severity = Mathf.Max(wound.Severity * 0.7f, 0.1f);
        }
    }
}
