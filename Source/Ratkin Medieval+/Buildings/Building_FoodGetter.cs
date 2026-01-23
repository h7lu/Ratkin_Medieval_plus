using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RkM;

public class Building_FoodGetter : Building_NutrientPasteDispenser, IBillGiverWithTickAction
{
	public bool CanDispenseNow_New
	{
		get
		{
			if (DispensableDef == null) return false;
			var result = true;
			var flag1 = nutritionComp!=null;
			if (flag1)  result=result && nutritionComp.CanDispenseNow;
			var flag2 = nutritionClassifyComp != null;
			if (flag2) result=result && nutritionClassifyComp.CanDispenseNow;
			return fuelComp.HasFuel && result;
		}
	}

	public override ThingDef DispensableDef
		{
			get
			{
				if (nutritionClassifyComp?.GetStewDef(index: nutritionClassifyComp.AvailableStewType) != null)
					return nutritionClassifyComp.GetStewDef( nutritionClassifyComp.AvailableStewType);
				return thingGetter?.thingDef;
			}
		}
		public override Color DrawColor
		{
			get
			{
				if (!this.IsSociallyProper(null, false, false)) return Building_Bed.SheetColorForPrisoner;
				return base.DrawColor;
			}
		}
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			fuelComp = GetComp<CompRefuelable>();
			nutritionComp = GetComp<CompNutritionStorge>();
			nutritionClassifyComp = GetComp<CompNutritionClassify>();
			thingGetter = def.GetModExtension<ModExtension_ThingGetter>();
			Log.Message("thingGetter: " + thingGetter);
			if (BeingTransportedOnGravship) return;
			foreach (var bill in billStack) bill.ValidateSettings();
		}

		public virtual List<ThingDef> Ingredients => nutritionComp?.Ingredients?.ToList();

		public virtual bool CanConsumeNow
		{
			
			get
			{
				if (nutritionComp == null) return false;
				if (!nutritionComp.CanDispenseNow) return false;
				if (nutritionClassifyComp is null or { CanDispenseNow: true }) return true;
				return false;
			}
		}
		public virtual void ConsumeNutrition(float nutrition) => nutritionComp?.ConsumeNutrition(nutrition);
		public virtual void StorageNutrition(float nutrition, List<Thing> ingredients) => nutritionComp?.AddNutrition(nutrition, ingredients);

		public override Thing TryDispenseFood()
		{
			Log.Message("TryDispenseFood");
			if (!CanDispenseNow) return null;
			List<ThingDef> list = new List<ThingDef>();
			if (nutritionComp != null && nutritionComp.Ingredients.Any()) list = Ingredients;
			def.building.soundDispense.PlayOneShot(new TargetInfo(Position, Map, false));
			Thing thing2 = ThingMaker.MakeThing(DispensableDef, null);
			CompIngredients compIngredients = thing2.TryGetComp<CompIngredients>();
			foreach (var t in list) compIngredients.RegisterIngredient(t);
			ConsumeNutrition(def.building.nutritionCostPerDispense);
			return thing2;
		}
		public virtual float TargetNutrition
		{
			get { 
				if (nutritionComp != null) return nutritionComp.TargetNutritionLevel;
				return 0; 
			}
		}

		public virtual float Nutrition
		{
			get
			{
				if (nutritionComp != null) return nutritionComp.Nutrition;
				return 0;
			}
		}
		public override bool HasEnoughFeedstockInHoppers()
		{
			return nutritionComp.Nutrition >= def.building.nutritionCostPerDispense;
		}
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (var gizmo in base.GetGizmos())
			{
				if (gizmo is Designator_Build designator_Build && designator_Build.PlacingDef == ThingDefOf.Hopper) continue;
				yield return gizmo;
			}
		}
		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.GetInspectString());
			if (!this.IsSociallyProper(null, false, false)) stringBuilder.AppendLine("InPrisonCell".Translate());
			return stringBuilder.ToString().Trim();
		}
		public CompRefuelable fuelComp;
		public CompNutritionStorge nutritionComp;
		public ModExtension_ThingGetter thingGetter;

		public bool CurrentlyUsableForBills()
		{
			return UsableForBillsAfterFueling() && (CanWorkWithoutFuel || fuelComp is { HasFuel: true });
		}

		public bool CanWorkWithoutFuel => false;

		public bool UsableForBillsAfterFueling() => true;

		public void Notify_BillDeleted(Bill bill)
		{
		}
		public BillStack billStack;
		public CompNutritionClassify nutritionClassifyComp;

		public Building_FoodGetter()
		{
			billStack = new BillStack(this);
		}

		public BillStack BillStack => billStack;
		public IEnumerable<IntVec3> IngredientStackCells =>GenAdj.CellsOccupiedBy(this);
		public void UsedThisTick()
		{
			fuelComp?.Notify_UsedThisTick();
			// if (this.moteEmitterComp != null)
			// {
			// 	if (!this.moteEmitterComp.MoteLive)
			// 	{
			// 		this.moteEmitterComp.Emit();
			// 	}
			// 	this.moteEmitterComp.Maintain();
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref billStack, "billStack", this);
		}
}