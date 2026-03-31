using RimWorld;
using Verse;

namespace RkM;

public class CompProperties_AbilityGiveThought : CompProperties_AbilityEffect
{
    public ThoughtDef thoughtDef;

    public bool applyToSelf;

    public bool onlyApplyToSelf;

    public bool applyToTarget = true;

    public bool replaceExisting;

    public bool ignoreSelf;
}
    

public class CompAbilityEffect_GiveThought : CompAbilityEffect_WithDuration
	{
		public new CompProperties_AbilityGiveThought Props => (CompProperties_AbilityGiveThought)props;

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
				if (Props.replaceExisting)
				{
					if (target.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(Props.thoughtDef) is not null)
					{
						target.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(Props.thoughtDef);
					}
				}
				target.needs?.mood?.thoughts?.memories?.TryGainMemory(Props.thoughtDef);
			}
		}

		protected virtual bool TryResist(Pawn pawn) => false;
		// public override void ResolveReferences()
		// {
		// 	base.ResolveReferences();
		// 	if (thoughtDef == null) Log.Error($"Ability {label} has no thoughtDef");
		// }
		// public override void ApplyEffect(Pawn caster, IntVec3 center, float radius)
		// {
		// 	Map map = caster.Map;
		// 	if (thoughtDef == null)
		// 		return;
		// 	List<Pawn> affectedPawns = GetAffectedPawns(center, radius, map);
		// 	foreach (Pawn pawn in affectedPawns)
		// 	{
  //           
  //               
		// 		MoteMaker.ThrowText(pawn.DrawPos, map, "+心情", Color.green);
		// 	}
  //           
		// 	Messages.Message($"{caster.LabelShort} 演奏了振奋之歌，激励了周围的人们。",
		// 		MessageTypeDefOf.PositiveEvent);
		// }
		public override bool AICanTargetNow(LocalTargetInfo target) => parent.pawn.Faction != Faction.OfPlayer && target.Pawn != null;
	}