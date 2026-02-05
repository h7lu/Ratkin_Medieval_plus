using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace RkM;
public class CompProperties_NutritionStorage : CompProperties
{
    public CompProperties_NutritionStorage() => compClass = typeof(CompNutritionStorge);
    public float maxNutrition = 100f;
    public bool rottable = true;
    //public RottableSlot rottableSlot;
    public bool hideGizmosIfNotPlayerFaction = true;
    public bool targetNutritionLevelConfigurable = true;
    public bool showAllowAutoAddToggle = true;
    public bool initialAllowAutoAdd = true;
    public float initialNutritionPercent = 0f;
    public bool drawNutritionGaugeInMap = true;
    public bool drawOutOfNutritionOverlay = true;
    public readonly string NutritionGizmoLabel = "NutritionGizmoLabel".Translate();
    public bool functionsInVacuum = true;
    public Color nutritionBarColor = Color.white;
    public bool useCustomBarColor = false;
    public bool performMergeCompatibilityChecks = true;
    public FoodKind noIngredientsFoodKind;
    public bool useCookingProgress = false;
    public float ticksPerUnit = 1000f;
}
public class CompNutritionStorge : ThingComp_VacuumAware
{
	public virtual CompNutritionClassify NutritionClassify => (parent as Building_FoodGetter)?.nutritionClassifyComp ;
	public override void Initialize(CompProperties props)
	{
		base.Initialize(props);
		//allowAutoAdd = Props.initialAllowAutoAdd;
		nutrition = Props.maxNutrition * Props.initialNutritionPercent;
		}
    public CompProperties_NutritionStorage Props => (CompProperties_NutritionStorage)props;
    private float nutrition = 0f;
    private List<ThingDef> ingredients = [];
    public virtual float Nutrition => nutrition;
    public virtual IEnumerable<ThingDef> Ingredients => ingredients;
    private float targetNutritionLevel = -1;
    public Color NutritionBarColor => Props.useCustomBarColor?Props.nutritionBarColor:GetFoodKindColor();

    public float TargetNutritionLevel
    {
	    get
	    {
		    if (Mathf.Approximately(targetNutritionLevel, -1)) return Props.maxNutrition - parent.def.building.nutritionCostPerDispense;
		    return targetNutritionLevel;
	    }
	    set => targetNutritionLevel = value;
    }

    // public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    // {
    //     if (parent.def.building is { IsTurret: true })
    //     {
    //         TaggedString taggedString = "RearmCostExplanation".Translate();
    //         if (Props.factorByDifficulty)
    //         {
    //             taggedString += " (" + "RearmCostExplanationDifficulty".Translate() + ")";
    //         }
    //         taggedString += ".";
    //         yield return new StatDrawEntry(StatCategoryDefOf.Building, "RearmCost".Translate(), GenLabel.ThingLabel(Props.fuelFilter.AnyAllowedDef, null, this.GetFuelCountToFullyRefuel()).CapitalizeFirst(), taggedString, 3171);
    //     }
    //     yield break;
    // }
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			if (!Props.hideGizmosIfNotPlayerFaction || parent.Faction == Faction.OfPlayer)
			{
				if (Find.Selector.SelectedObjects.Count == 1) yield return new Gizmo_SetNutritionLevel(this);
				else
				{
					if (Props.targetNutritionLevelConfigurable)
					{
						yield return new Command_SetTargetNutritionLevel
						{
							nutritionStorge = this,
							defaultLabel = "CommandSetTargetFuelLevel".Translate(),
							defaultDesc = "CommandSetTargetFuelLevelDesc".Translate(),
							icon = null,
						};
					}
				}
			}
			if (DebugSettings.ShowDevGizmos)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: ClearNutrition",
					action = delegate
					{
						ClearNutritionAndIngredients();
						parent.BroadcastCompSignal("Cleared");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Set nutrition to 0.1",
					action = delegate
					{
						nutrition = 0.1f;
						parent.BroadcastCompSignal("Filled");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Nutrition -20%",
					action = delegate
					{
						ConsumeNutrition(Props.maxNutrition * 0.2f);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Set nutrition to max",
					action = delegate
					{
						nutrition = Props.maxNutrition;
						parent.BroadcastCompSignal("Filled");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: ClearNutrition",
					action = delegate
					{
						ClearNutritionAndIngredients();
						parent.BroadcastCompSignal("Cleared");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEV: Add ingredient",
					action = delegate
					{
						Find.WindowStack.Add(new Dialog_AddIngredient(this));
					}
				};
			}
		}
	

		public bool HasNutrition => nutrition > 0f;
		
		public float NutritionPercentOfMax => nutrition / Props.maxNutrition;
		private int tick = 0;
		private float cookingProgress = 0f;
		public float CookingProgress => cookingProgress;
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
            if (!HasFuel)
            {
                if (cookingProgress > 0)
                    cookingProgress = Mathf.Max(0, cookingProgress - 1);
                return;
            }
            // Determine what we're cooking
            bool canCook = CanCook;
            // If we started cooking something and ingredients changed, reset
            if (!CanCook)
            {
                cookingProgress = 0;
                return;
            }
            // Progress cooking if we have nutrition OR we are making hot water
            if (canCook)
            {
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
        public virtual bool CanDispenseNow
        {
	        get
	        {
		        if (Props.useCookingProgress)
		        {
			        return  CookingProgressPct >= 0.9f;
		        }
		        return nutrition > 0f;
	        }
        }
        public bool CanCook
        {
	        get
	        {
		        if (NutritionClassify != null)
			        return NutritionClassify.CanCook;
		        return Nutrition > 0f;

	        }
        }
        // public override void CompTick()
  //   {
  //       base.CompTick();
  //       tick++;
  //       if (tick%60 == 0)
  //       {
	 //        if (Props.rottable)
		//         if (rotTick > 0)
		//         {
		// 	        rotTick-=60;
		// 	        if (rotTick <= 0)
		// 	        {
		// 		        ClearNutritionAndIngredients();
		// 		        //警告腐烂
		// 		        Messages.Message("MessageRottedAwayInStorage".Translate(parent.Label, parent).CapitalizeFirst(), new TargetInfo(parent.PositionHeld, parent.MapHeld), MessageTypeDefOf.NegativeEvent);
		// 	        }
		//         }
	 //        if (tick/6000 == 1) tick = 0;
  //       }
  //   }

    // public virtual void AddNutritionFromRaw(List<Thing> raws)
    // {
    //     float num = 0f;
    //     var list = new List<ThingDef>();
    //     int rot = -1;
    //     foreach (var thing in raws)
    //     {
    //         num += thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition);
    //         list.Add(thing.def);
    //         //thing.SplitOff(num2);
    //     }
    //     //if (Props.rottable) rot = (int)(Props.rottableSlot.TicksToRotStart * num);
    //     AddNutrition(num, list);
    // }
    public virtual void AddNutrition(float nutrition, List<Thing> ingredients)
    {
	    if (ingredients == null|| ingredients.Count == 0)
	    {
		    Log.Error($"[RkM] CompNutritionStorge.AddNutrition: ingredients is null for {parent}");
		    return;
	    }
        if (this.nutrition+nutrition > Props.maxNutrition) nutrition = Props.maxNutrition-this.nutrition;
        NutritionClassify?.NotifyNutritionAdded(nutrition, ingredients);
        var ingredientDefs = ingredients.Select(x => x.def).ToList();
        // 添加原材料种类，并集(除去重复值)
        this.ingredients ??= [];
        this.ingredients.AddRange(ingredientDefs);
        this.ingredients = this.ingredients.Distinct().ToList();
        // if (Props.rottable&&rotTick != -1)
        // {
        //     if (this.rotTick == -1) this.rotTick = rotTick;
        //     // 移动加权平均法算剩余时间
        //     this.rotTick = (int)((this.rotTick * Nutrition + rotTick) / (this.nutrition + nutrition));
        // }
        float oldNutrition = this.nutrition;
        this.nutrition += nutrition;
        
        // Reduce cooking progress based on dilution (Thermal Shock / Mixing)
        // Example: 90% progress with 10 nutrition. Add 5. Total 15.
        // New Progress = 90% * (10 + 1/ 15 + 1) = 68.7%.
        if (Props.useCookingProgress)
	        if (CookingProgress > 0 && this.nutrition > 0 && this.nutrition > oldNutrition) cookingProgress *= (oldNutrition + 1)/ (this.nutrition + 1);
    }
    public virtual void ConsumeNutrition(float nutrition)
    {
	    var tempOld = nutrition;
        this.nutrition -= nutrition;
        if (this.nutrition <= 0f)
        {
	        ClearNutritionAndIngredients();
	        nutrition = tempOld;
        }
        NutritionClassify?.NotifyNutritionConsumed(nutrition);
    }
    public virtual void ClearNutritionAndIngredients()
    { 
        nutrition = 0f;
        ingredients.Clear();
        NutritionClassify?.NotifyNutritionCleaned();
        //rotTick = -1;
    }
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref nutrition, "nutrition", 0f);
        Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Def);
        Scribe_Values.Look(ref cookingProgress, "cookingProgress", 0f);
    }
    public override string CompInspectStringExtra()
    {
	    StringBuilder stringBuilder = new StringBuilder();
	    if (ingredients.Count > 0)
	    {
		    stringBuilder.Append("Ingredients".Translate() + ": ");
		    stringBuilder.Append(GetIngredientsString(true, out var flag));
		    if (flag) stringBuilder.Append(" (* " + "OnlyStacksWithCompatibleMeals".Translate().Resolve() + ")");
	    }
	    if (ModsConfig.IdeologyActive) stringBuilder.AppendLineIfNotEmpty().Append(GetFoodKindInspectString());
	    var result = "";
	    if (Props.useCookingProgress)
	    {
		    result += "\n" + "RkM_CookingStew".Translate() + ": " + CookingProgressPct.ToStringPercent();
	    }
	    return base.CompInspectStringExtra() + stringBuilder + result;
    }

    public float CookingProgressPct => cookingProgress / Props.ticksPerUnit;

    public string GetIngredientsString(bool includeMergeCompatibility, out bool hasMergeCompatibilityIngredients)
    {
	    StringBuilder stringBuilder = new StringBuilder();
	    hasMergeCompatibilityIngredients = false;
	    for (int i = 0; i < ingredients.Count; i++)
	    {
		    ThingDef thingDef = ingredients[i];
		    stringBuilder.Append((i == 0) ? thingDef.LabelCap.Resolve() : thingDef.label);
		    if (includeMergeCompatibility && Props.performMergeCompatibilityChecks)
		    {
			    IngredientProperties ingredient = thingDef.ingredient;
			    if (ingredient != null && ingredient.mergeCompatibilityTags.Count > 0)
			    {
				    stringBuilder.Append("*");
				    hasMergeCompatibilityIngredients = true;
			    }
		    }
		    if (i < ingredients.Count - 1) stringBuilder.Append(", ");
	    }
	    return stringBuilder.ToString();
    }
    private string GetFoodKindInspectString()
    {
	    if (GetFoodKind(this) == ReturnFoodType.Vegetarian)
		    return "MealKindVegetarian".Translate().Colorize(Color.green);
	    if (GetFoodKind(this) == ReturnFoodType.Meat)
		    return "MealKindMeat".Translate().Colorize(ColorLibrary.RedReadable);
	    //if (GetFoodKind(this)==ReturnFoodType.MeatAndVegetarian)
		//    return "MealKindMeatAndVegetarian".Translate().Colorize(Color.yellow);
	    return "MealKindOther".Translate().Colorize(Color.white);
    }
    public static readonly Color BarColor_Vegetarian = new(0.4f, 0.8f, 0.4f);
    public static readonly Color BarTipColor_Vegetarian = new(0.6f, 1f, 0.6f);
    public static readonly Color BarColor_Meat = new(0.8f, 0.4f, 0.4f);
	public static readonly Color BarTipColor_Meat = new(1f, 0.6f, 0.6f);
	//public static readonly Color BarColor_MeatAndVegetarian = new(0.8f, 0.8f, 0.4f);
	//public static readonly Color BarTipColor_MeatAndVegetarian = new(1f, 1f, 0.6f);
	public static readonly Color BarColor_Other = new(0.6f, 0.6f, 0.6f);
	public static readonly Color BarTipColor_Other = new(0.8f, 0.8f, 0.8f);

	internal Color GetFoodKindColor(bool tip = false)
    {
	    if (GetFoodKind(this) == ReturnFoodType.Vegetarian)
		    return tip? BarTipColor_Vegetarian : BarColor_Vegetarian;
	    if (GetFoodKind(this) == ReturnFoodType.Meat)
		    return tip? BarTipColor_Meat : BarColor_Meat;
	    //if (GetFoodKind(this)==ReturnFoodType.MeatAndVegetarian)
		//    return tip? BarTipColor_MeatAndVegetarian : BarColor_MeatAndVegetarian;
	    return tip? BarTipColor_Other : BarColor_Other;
    }
    protected override bool FunctionsInVacuum => Props.functionsInVacuum;
    public static ReturnFoodType GetFoodKind(CompNutritionStorge comp)
    {
	    if (comp == null) return ReturnFoodType.Other;
	    if (comp.ingredients.NullOrEmpty()) return ReturnFoodType.Other;
	    bool flag = false;
	    foreach (var t in comp.ingredients)
	    {
		    if (FoodUtility.GetFoodKind(t) == FoodKind.Meat) return ReturnFoodType.Meat;
		    if (t.IsAnimalProduct) flag = true;
	    }
	    return !flag ? ReturnFoodType.Vegetarian : ReturnFoodType.Other;
	    // bool meat = false;
	    // bool vegetarian = false;
	    // for (int index = 0; index < comp.ingredients.Count; ++index)
	    // {
		   //  if (FoodUtility.GetFoodKind(comp.ingredients[index]) == FoodKind.Meat) meat = true;
		   //  else if (comp.ingredients[index].IsAnimalProduct) flag = true;
		   //  else vegetarian = true;
		   //  if (meat&& vegetarian&& flag) break;
	    // }
	    // ReturnFoodType result;
	    // if (meat)
	    // {
		   //  if (vegetarian&&!flag) result = ReturnFoodType.MeatAndVegetarian;
		   //  else result = ReturnFoodType.Meat;
	    // }
	    // else if (vegetarian) result = flag ? ReturnFoodType.Other : ReturnFoodType.Vegetarian;
	    // else result = ReturnFoodType.Other;
	    // return result;
    }
    public enum ReturnFoodType : byte
    {
	    Meat,
	    Vegetarian,
	    //MeatAndVegetarian,
	    Other
    }
}


[StaticConstructorOnStartup]
public class Command_SetTargetNutritionLevel : Command
{
  public CompNutritionStorge nutritionStorge;
  private List<CompNutritionStorge> nutritionStorges;

  public override void ProcessInput(Event ev)
  {
    base.ProcessInput(ev);
    if (nutritionStorges == null)
      nutritionStorges = new List<CompNutritionStorge>();
    if (!nutritionStorges.Contains(nutritionStorge))
      nutritionStorges.Add(nutritionStorge);
    int to = int.MaxValue;
    for (int index = 0; index < nutritionStorges.Count; ++index)
    {
      if ((int) nutritionStorges[index].Props.maxNutrition < to)
        to = (int) nutritionStorges[index].Props.maxNutrition;
    }
    int startingValue = to / 2;
    for (int index = 0; index < nutritionStorges.Count; ++index)
    {
      if ((int) nutritionStorges[index].TargetNutritionLevel <= to)
      {
        startingValue = (int) nutritionStorges[index].TargetNutritionLevel;
        break;
      }
    }
    Dialog_Slider dialogSlider = new Dialog_Slider(!nutritionStorge.parent.def.building.hasFuelingPort ? x => "SetTargetFuelLevel".Translate(x) : x =>
    {
	    CompLaunchable compLaunchable = FuelingPortUtility.LaunchableAt(FuelingPortUtility.GetFuelingPortCell(nutritionStorge.parent.Position, nutritionStorge.parent.Rotation), nutritionStorge.parent.Map);
	    if (compLaunchable == null) return "SetTargetFuelLevel".Translate(x);
	    int num = compLaunchable.MaxLaunchDistanceAtFuelLevel(x);
	    return "SetPodLauncherTargetFuelLevel".Translate(x, num);
    }, 0, to, value =>
    {
	    for (int index = 0; index < nutritionStorges.Count; ++index)
		    nutritionStorges[index].TargetNutritionLevel = value;
    }, startingValue);
    if (nutritionStorge.parent.def.building.hasFuelingPort)
      dialogSlider.extraBottomSpace = Text.LineHeight + 4f;
    Find.WindowStack.Add(dialogSlider);
  }

  public override bool InheritInteractionsFrom(Gizmo other)
  {
    if (nutritionStorges == null) nutritionStorges = new List<CompNutritionStorge>();
    nutritionStorges.Add(((Command_SetTargetNutritionLevel) other).nutritionStorge);
    return false;
  }
}
public class Gizmo_SetNutritionLevel : Gizmo_Slider
{
  private CompNutritionStorge nutritionStorge;
  private static bool draggingBar;
  protected override float Target
  {
    get => nutritionStorge.TargetNutritionLevel / nutritionStorge.Props.maxNutrition;
    set => nutritionStorge.TargetNutritionLevel = value * nutritionStorge.Props.maxNutrition;
  }
  protected override float ValuePercent => nutritionStorge.NutritionPercentOfMax;
  protected override string Title => nutritionStorge.Props.NutritionGizmoLabel;
  protected override bool IsDraggable => nutritionStorge.Props.targetNutritionLevelConfigurable;
  protected override Color BarColor => nutritionStorge.NutritionBarColor;
  protected override Color BarHighlightColor => nutritionStorge.GetFoodKindColor(true);

  protected override string BarLabel => $"{nutritionStorge.Nutrition.ToStringDecimalIfSmall()} / {nutritionStorge.Props.maxNutrition.ToStringDecimalIfSmall()}";

  protected override bool DraggingBar
  {
    get => draggingBar;
    set => draggingBar = value;
  }

  public Gizmo_SetNutritionLevel(CompNutritionStorge nutritionStorge) => this.nutritionStorge = nutritionStorge;

  public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
  {
    if (!nutritionStorge.Props.showAllowAutoAddToggle)
      return base.GizmoOnGUI(topLeft, maxWidth, parms);
    if (SteamDeck.IsSteamDeckInNonKeyboardMode)
      return base.GizmoOnGUI(topLeft, maxWidth, parms);
    KeyCode keyCode = KeyBindingDefOf.Command_ItemForbid == null ? KeyCode.None : KeyBindingDefOf.Command_ItemForbid.MainKey;
    if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode) && KeyBindingDefOf.Command_ItemForbid.KeyDownEvent)
    {
      //ToggleAutoRefuel();
      Event.current.Use();
    }
    return base.GizmoOnGUI(topLeft, maxWidth, parms);
  }

  protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
  {
    if (nutritionStorge.Props.showAllowAutoAddToggle)
    {
      headerRect.xMax -= 24f;
      Rect rect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);
      //GUI.DrawTexture(rect, nutritionStorge.Props.FuelIcon);
      //GUI.DrawTexture(new Rect(rect.center.x, rect.y, rect.width / 2f, rect.height / 2f), nutritionStorge.allowAutoAdd ? Widgets.CheckboxOnTex : (Texture) Widgets.CheckboxOffTex);
      //if (Widgets.ButtonInvisible(rect)) ToggleAutoRefuel();
      if (Mouse.IsOver(rect))
      {
        Widgets.DrawHighlight(rect);
        TooltipHandler.TipRegion(rect, RefuelTip, 712326773);
        mouseOverElement = true;
      }
    }
    base.DrawHeader(headerRect, ref mouseOverElement);
  }
  

  private string RefuelTip()
  {
    string str1 = $"{"CommandToggleAllowAutoRefuel".Translate()}" + "\n\n";
    //string str3 = "CommandToggleAllowAutoRefuelDesc".Translate((NamedArgument) nutritionStorge.TargetNutritionLevel.ToString("F0").Colorize(ColoredText.TipSectionTitleColor), ;
    //string str4 = str1 + str3 + "\n\n";
    string stringReadable = KeyPrefs.KeyPrefsData.GetBoundKeyCode(KeyBindingDefOf.Command_ItemForbid, KeyPrefs.BindingSlot.A).ToStringReadable();
    TaggedString taggedString = "HotKeyTip".Translate() + ": " + stringReadable;
    return taggedString;
  }

  protected override string GetTooltip() => "";
}

public class Dialog_AddIngredient : Window
{
	private readonly CompNutritionStorge nutritionStorage;
	private readonly List<ThingDef> allIngredients;
	private Vector2 scrollPosition = Vector2.zero;

	public override Vector2 InitialSize => new(600f, 600f);

	public Dialog_AddIngredient(CompNutritionStorge nutritionStorage)
	{
		this.nutritionStorage = nutritionStorage;
		allIngredients = DefDatabase<ThingDef>.AllDefs
			.Where(td => td.category == ThingCategory.Item && td.IsNutritionGivingIngestible)
			.ToList();
		forcePause = true;
		doCloseX = true;
		closeOnClickedOutside = true;
		absorbInputAroundWindow = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Medium;
		Rect titleRect = inRect.TopPartPixels(40f);
		inRect = inRect.ContractedBy(10f);
		Widgets.Label(titleRect, "Select Ingredient to Add");
		
		inRect.yMin += 40f;
		Rect scrollViewRect = inRect.AtZero();
		scrollViewRect.height = allIngredients.Count * 30f;
		
		Widgets.BeginScrollView(inRect, ref scrollPosition, scrollViewRect);
		float currentY = 0f;
		foreach (ThingDef thingDef in allIngredients)
		{
			Rect buttonRect = new Rect(0f, currentY, scrollViewRect.width, 30f);
			if (Widgets.ButtonText(buttonRect, thingDef.LabelCap))
			{
				var thing = ThingMaker.MakeThing(thingDef);
				thing.stackCount = 1;
				AddIngredientToStorage(thing);
				Close();
			}
			currentY += 30f;
		}
		Widgets.EndScrollView();
	}

	private void AddIngredientToStorage(Thing thing)
	{
		var list = new List<Thing> { thing };
		float nutritionValue = thing.GetStatValue(StatDefOf.Nutrition);
		nutritionStorage.AddNutrition(nutritionValue, list);
		Messages.Message($"Added {thing.LabelCap} to ingredients", MessageTypeDefOf.TaskCompletion);
	}
}