using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RkM.HarmonyPatches;
[HarmonyPatch(typeof(FoodUtility), "BestFoodSourceOnMap")]
public static class HarmonyPatch_BestFoodSourceOnMap_Target
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        for (var i = 0; i < codes.Count - 1; i++)
        {
            if (codes[i].opcode == OpCodes.Ldftn && codes[i + 1].opcode == OpCodes.Newobj)
            {
                TargetMethod = (MethodInfo)codes[i].operand;
                break;
            }
        }
        return codes;
    }
    public static MethodInfo TargetMethod;
}
[HarmonyPatch]
public static class HarmonyPatch_BestFoodSourceOnMap_Transpile
{
	private static MethodBase TargetMethod()
		{
			if (HarmonyPatch_BestFoodSourceOnMap_Target.TargetMethod != null) return HarmonyPatch_BestFoodSourceOnMap_Target.TargetMethod;
			throw new Exception("Cannot find target method!");
		}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
			List<CodeInstruction> codes = instructions.ToList();
			bool flag1 = false;
			bool flag2 = false;
			for (int i = 0; i < codes.Count - 1; i++)
			{
				if (codes[i].opcode == OpCodes.Ldsfld)
				{
					FieldInfo field = codes[i].operand as FieldInfo;
					if (field != null && field.Name == "MealNutrientPaste")
					{
						codes[i] = new CodeInstruction(OpCodes.Ldloc_1, null);
						codes.Insert(i + 1, new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Building_NutrientPasteDispenser), "DispensableDef")));
						flag1 = true;
					}
				}
				if (codes[i].opcode == OpCodes.Ldfld)  // 处理实例字段访问
				{
					FieldInfo field = codes[i].operand as FieldInfo;  // 在这里重新获取 field
					if (field != null && field.Name == "powerComp" && 
					    field.DeclaringType == typeof(Building_NutrientPasteDispenser))
					{
						// 检查下一条指令是否是get_PowerOn
						if (i + 1 < codes.Count && codes[i + 1].opcode == OpCodes.Callvirt)
						{
							MethodInfo method = codes[i + 1].operand as MethodInfo;
							if (method != null && method.Name == "get_PowerOn")
							{
								// 替换为加载分发器并调用自定义方法
								codes[i] = new CodeInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Util), "BuildingNutrientPastePowerOn", [typeof(Building_NutrientPasteDispenser)])));
								//codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Util), "BuildingNutrientPastePowerOn", [typeof(Building_NutrientPasteDispenser)])));
								codes[i + 1] = new CodeInstruction(OpCodes.Nop, null); //new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Util), "BuildingNutrientPastePowerOn", [typeof(Building_NutrientPasteDispenser)]));
								flag2 = true;
							}
						}
					}
				}
			}
			if (!flag1 || !flag2) Log.Message("BestFoodSourceOnMap patch failed!");
			return codes;
		}
	}

public static class Util
{
	public static bool BuildingNutrientPastePowerOn(this Building_NutrientPasteDispenser thing)
	{
		if (thing is Building_FoodGetter getter)
			return getter.CanDispenseNow_New;
		return thing.powerComp.PowerOn;
	}
	public static bool TypeCheck(Type thingClass, Type targetType) => thingClass == targetType || thingClass.IsSubclassOf(targetType);
	
}
[HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
public static class HarmonyPatch_ThingListGroupHelper_Includes
{
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> code = new List<CodeInstruction>(instructions);
		MethodInfo equalityMethod = AccessTools.Method(typeof(Type), "op_Equality", [
			typeof(Type),
			typeof(Type)
		], null);
		MethodInfo customCheckMethod = AccessTools.Method(typeof(Util), "TypeCheck", null, null);
		for (int i = 0; i < code.Count; i++)
			if (code[i].Calls(equalityMethod) && i >= 2 && code[i - 1].opcode == OpCodes.Call && code[i - 2].opcode == OpCodes.Ldtoken)
				if (code[i - 2].operand is Type targetType && targetType == typeof(Building_NutrientPasteDispenser))
					code[i] = new CodeInstruction(OpCodes.Call, customCheckMethod);
		return code;
	}
	
}