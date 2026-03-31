using RimWorld;
using Verse;

namespace RkM;

public class CompProperties_AbilityGiveJoy : CompProperties_AbilityEffect
{
    public float joyAmount;

    public bool applyToSelf;

    public bool onlyApplyToSelf;

    public bool applyToTarget = true;

    public bool ignoreSelf;
}
    

public class CompAbilityEffect_GiveJoy : CompAbilityEffect
	{
		public new CompProperties_AbilityGiveJoy Props => (CompProperties_AbilityGiveJoy)props;

		public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
		{
			base.Apply(target, dest);
			if (Props.ignoreSelf && target.Pawn == parent.pawn) return;
			if (!Props.onlyApplyToSelf && Props.applyToTarget) ApplyInner(target.Pawn, parent.pawn);
			if (Props.applyToSelf || Props.onlyApplyToSelf) ApplyInner(parent.pawn, target.Pawn);
		}

		protected void ApplyInner(Pawn target, Pawn other)
		{
			if (target != null)
			{
				if (TryResist(target))
				{
					MoteMaker.ThrowText(target.DrawPos, target.Map, "Resisted".Translate());
					return;
				}
				target.needs?.joy?.CurLevel += Props.joyAmount;
				//MoteMaker.ThrowText(pawn.DrawPos, map, "+娱乐", Color.yellow);
			}
		}

		protected virtual bool TryResist(Pawn pawn) => false;
		// public override void ApplyEffect(Pawn caster, IntVec3 center, float radius)
		// {
		// 	Map map = caster.Map;
		// 	var affectedPawns = GetAffectedPawns(center, radius, map);
		// 	foreach (var pawn in affectedPawns)
		// 	{
		// 		if (pawn.needs.joy != null)
		// 		{
		// 			pawn.needs.joy.CurLevel += joyAmount;
		// 			MoteMaker.ThrowText(pawn.DrawPos, map, "+娱乐", Color.yellow);
		// 		}
		// 	}
		// 	Messages.Message($"{caster.LabelShort} 演奏了欢快之歌，为周围的人们带来了欢乐。",
		// 		MessageTypeDefOf.PositiveEvent);
		// }
		public override bool AICanTargetNow(LocalTargetInfo target) => parent.pawn.Faction != Faction.OfPlayer && target.Pawn != null;
	}