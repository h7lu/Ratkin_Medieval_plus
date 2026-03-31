using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RkM;

public class CompEquippableAbilities : CompEquippable
{
	private CompProperties_EquippableAbilities Props => props as CompProperties_EquippableAbilities;

	public virtual List<Ability> AbilitiesForReading
	{
		get
		{
			Log.Message($"[RkM] CompEquippableAbilities.AbilitiesForReading 开始执行");
			if (abilities == null && !Props.abilityDefs.NullOrEmpty())
			{
				abilities = [];
				Log.Message($"[RkM] CompEquippableAbilities.AbilitiesForReading abilities 为空，开始初始化");
				foreach (var propsAbilityDef in Props.abilityDefs) abilities.Add(AbilityUtility.MakeAbility(propsAbilityDef, Holder));
			} 
			return abilities;
		}
	}

	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		if (Holder != null && !AbilitiesForReading.EnumerableNullOrEmpty())
		{
			foreach (var ability in AbilitiesForReading)
			{
				ability.pawn = Holder;
				ability.verb?.caster = Holder;
			}
		}
	}

	public override void Notify_Equipped(Pawn pawn)
	{
		Log.Message($"[RkM] CompEquippableAbilities.Notify_Equipped 启动");
		Log.Message($"[RkM] Pawn: {pawn.Name}, equipment: {pawn.equipment}, Primary: {pawn.equipment?.Primary}");
		Log.Message($"[RkM]AbilitiesForReading : {AbilitiesForReading.Count()}");
		if (AbilitiesForReading.EnumerableNullOrEmpty()) return;
		foreach (var ability in AbilitiesForReading)
		{
			ability.pawn = pawn;
			ability.verb?.caster = pawn;
			Log.Message($"[RkM] 初始化能力：{ability.def.label}, verb: {ability.verb}");
		}
		
		pawn.abilities.Notify_TemporaryAbilitiesChanged();
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		if (AbilitiesForReading.EnumerableNullOrEmpty()) return;
		pawn.abilities.Notify_TemporaryAbilitiesChanged();
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Collections.Look(ref abilities, "abilities",LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && Holder != null && !AbilitiesForReading.EnumerableNullOrEmpty())
		{
			InitializeAbilities();
		}
	}

	private void InitializeAbilities()
	{
		if (AbilitiesForReading.EnumerableNullOrEmpty()) return;
		
		foreach (var ability in AbilitiesForReading)
		{
			if (ability != null)
			{
				ability.pawn = Holder;
				ability.verb?.caster = Holder;
			}
		}
	}

	private List<Ability> abilities;
}
public class CompProperties_EquippableAbilities : CompProperties
{
	public List<AbilityDef> abilityDefs = [];
	public CompProperties_EquippableAbilities() => compClass = typeof (CompEquippableAbilities);
}
