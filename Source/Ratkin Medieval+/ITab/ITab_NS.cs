using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RkM;

public class ITab_NS : ITab
	{
		private float viewHeight = 1000f;
  private Vector2 scrollPosition;
  private Bill mouseoverBill;
  private static readonly Vector2 WinSize = new Vector2(420f, 480f);
  [TweakValue("Interface", 0.0f, 128f)]
  private static float PasteX = 48f;
  [TweakValue("Interface", 0.0f, 128f)]
  private static float PasteY = 3f;
  [TweakValue("Interface", 0.0f, 32f)]
  private static float PasteSize = 24f;

  protected Building_FoodGetter SelTable => (Building_FoodGetter) SelThing;

  public ITab_NS()
  {
    size = WinSize;
    labelKey = "TabBills";
    tutorTag = "Bills";
  }

  protected override void FillTab()
  {
    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
    // 粘贴按钮区域
    Rect pasteRect = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
    // 处理粘贴功能
    HandlePasteButton(pasteRect);
    // 显示配方列表
    Rect listRect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            mouseoverBill = SelTable.billStack.DoListing(listRect, MakeOptions, ref scrollPosition, ref viewHeight);
  }
  private void HandlePasteButton(Rect rect)
  { 
    if (BillUtility.Clipboard != null)
    {
      bool recipeAvailable = SelTable.def.AllRecipes.Contains(BillUtility.Clipboard.recipe) && BillUtility.Clipboard.recipe.AvailableNow && BillUtility.Clipboard.recipe.AvailableOnNow(SelTable);
      bool billLimitReached = SelTable.billStack.Count >= 15;
      if (!recipeAvailable)
      {
        GUI.color = Color.gray;
        Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
        GUI.color = Color.white;
        if (Mouse.IsOver(rect)) TooltipHandler.TipRegion(rect, "ClipboardBillNotAvailableHere".Translate() + ": " + BillUtility.Clipboard.LabelCap);
      }
      else if (billLimitReached)
      {
        GUI.color = Color.gray;
        Widgets.DrawTextureFitted(rect, TexButton.Paste, 1f);
        GUI.color = Color.white;
        if (Mouse.IsOver(rect))
          TooltipHandler.TipRegion(rect, "PasteBillTip".Translate() + " (" + "PasteBillTip_LimitReached".Translate() + "): " + BillUtility.Clipboard.LabelCap);
      }
      else
      {
        if (Widgets.ButtonImageFitted(rect, TexButton.Paste, Color.white))
        {
          Bill bill = BillUtility.Clipboard.Clone();
          bill.InitializeAfterClone();
          SelTable.billStack.AddBill(bill);
          SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        if (Mouse.IsOver(rect)) TooltipHandler.TipRegion(rect, "PasteBillTip".Translate() + ": " + BillUtility.Clipboard.LabelCap);
      }
    }
  }
  public override void TabUpdate()
  {
    if (mouseoverBill == null) return;
    mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelTable.Position);
    mouseoverBill = null;
  }
  private List<FloatMenuOption> MakeOptions()
  {
    List<FloatMenuOption> options = [];
    // 添加所有可用的配方
    for (int i = 0; i < SelTable.def.AllRecipes.Count; i++)
    {
      RecipeDef recipe = SelTable.def.AllRecipes[i];
      if (recipe.AvailableNow && recipe.AvailableOnNow(SelTable))
      {
        // 添加基础配方选项
        AddRecipeOption(options, recipe, i, null);
        // 添加来自意识形态的建筑变体配方
        foreach (var ideo in Faction.OfPlayer.ideos.AllIdeos)
        foreach (var buildingPrecept in ideo.cachedPossibleBuildings.Where(buildingPrecept => buildingPrecept.ThingDef == recipe.ProducedThingDef)) AddRecipeOption(options, recipe, i, buildingPrecept);
      }
    }if (!options.Any()) options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
    return options;
  }
  private void AddRecipeOption(List<FloatMenuOption> options, RecipeDef recipe, int index, Precept_ThingStyle precept)
  {
    string label = precept != null ? "RecipeMake".Translate(precept.LabelCap).CapitalizeFirst() : recipe.LabelCap;
    Action action = () => OnRecipeSelected(recipe, precept, index);
    Action<Rect> mouseoverAction = (rect) => ShowRecipeInfo(recipe, precept, label, index, rect);
    Func<Rect, bool> infoButtonAction = (rect) => ShowInfoCard(recipe, precept, rect);
    options.Add(new FloatMenuOption(
      label, action, recipe.UIIconThing,
      recipe.UIIcon, null, false,
      MenuOptionPriority.Default, mouseoverAction,
      null, 29f,
      infoButtonAction, null,
      true, -recipe.displayPriority));
  }
  private void OnRecipeSelected(RecipeDef recipe, Precept_ThingStyle precept, int index)
  {
    // 检查机械师专属配方
    if (ModsConfig.BiotechActive && recipe.mechanitorOnlyRecipe && !SelTable.Map.mapPawns.FreeColonists.Any(MechanitorUtility.IsMechanitor))
    {
      Find.WindowStack.Add(new Dialog_MessageBox("RecipeRequiresMechanitor".Translate(recipe.LabelCap)));
      return;
    }
    // 检查是否有满足技能要求的殖民者
    if (!SelTable.Map.mapPawns.FreeColonists.Any(recipe.PawnSatisfiesSkillRequirements))
    {
      Bill.CreateNoPawnsWithSkillDialog(recipe);
      return;
    }
    // 创建并添加新配方
    Bill bill = recipe.MakeNewBill(precept);
    SelTable.billStack.AddBill(bill);
    // 记录知识
    if (recipe.conceptLearned != null) PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
  }
  private void ShowRecipeInfo(RecipeDef recipe, Precept_ThingStyle precept, string label, int index, Rect rect) => BillUtility.DoBillInfoWindow(index, label, rect, recipe);
  private bool ShowInfoCard(RecipeDef recipe, Precept_ThingStyle precept, Rect rect)
  {
    return Widgets.InfoCardButton(
      rect.x + 5f,
      rect.y + (rect.height - 24f) / 2f,
      recipe,
      precept
    );
  }
  
}