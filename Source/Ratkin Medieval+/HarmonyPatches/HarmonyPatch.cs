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
[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
    Harmony harmony = new Harmony("com.SYSFix.rimworld.mod");
    harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}

[HarmonyPatch(typeof(Pawn_AbilityTracker))]
    [HarmonyPatch(nameof(Pawn_AbilityTracker.AllAbilitiesForReading), MethodType.Getter)]
    public static class Patch_AllAbilitiesForReading
    {
        // 辅助方法：添加 CompEquippableAbilities 提供的能力
        private static void AddEquippableAbilities(Pawn pawn, List<Ability> targetList)
        {
            Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities 开始执行");
            Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities Pawn: {pawn.Name}");
            Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities equipment: {pawn?.equipment}");
            Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities Primary: {pawn?.equipment?.Primary}");
            var comp = pawn?.equipment?.Primary?.TryGetComp<CompEquippableAbilities>();
            Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities comp: {comp}");
            if (comp == null) return;
            Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities comp.AbilitiesForReading: {comp.AbilitiesForReading.Count()}");
            foreach (var ability in comp.AbilitiesForReading)
            {
                Log.Message($"[RkM] Patch_AllAbilitiesForReading.AddEquippableAbilities ability: {ability}");
                if (ability != null)
                    targetList.Add(ability);
            }
        }

        // Transpiler 在原有 equipment 处理代码后插入对 AddEquippableAbilities 的调用
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            // 找到原始 equipment 处理块中，调用 get_AbilityForReading 之前的代码
            // 目标位置：在 comp 变量赋值之后，if (comp != null) 之前
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Stloc_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Brfalse_S)
            );

            if (matcher.IsValid)
            {
                // 在 stloc.0 之后插入我们的调用
                matcher.Advance(1); // 移到 brfalse 之前

                var helper = AccessTools.Method(typeof(Patch_AllAbilitiesForReading), nameof(AddEquippableAbilities));
                var pawnField = AccessTools.Field(typeof(Pawn_AbilityTracker), "pawn");
                var cacheField = AccessTools.Field(typeof(Pawn_AbilityTracker), "allAbilitiesCached");

                matcher.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),               // this
                    new CodeInstruction(OpCodes.Ldfld, pawnField),      // pawn
                    new CodeInstruction(OpCodes.Ldarg_0),               // this
                    new CodeInstruction(OpCodes.Ldfld, cacheField),     // allAbilitiesCached
                    new CodeInstruction(OpCodes.Call, helper)
                );
            }
            //输出修改后的指令序列（可选，用于详细调试）
                int index = 0;
                foreach (var instruction in matcher.InstructionEnumeration())
                {
                    Log.Message($"[{index++}]: {instruction}");
                }
            return matcher.InstructionEnumeration();
        }
        // static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        // {
        //     Log.Message("[RkM] Patch_AllAbilitiesForReading.Transpiler 开始执行");
        //
        //     var codeInstructions = instructions.ToList();
        //     var matcher = new CodeMatcher(codeInstructions);
        //     Log.Message($"[RkM] Transpiler 初始指令数：{matcher.Length}");
        //     
        //     // 匹配 callvirt List<Ability>.Add 之后紧接着 apparel 处理开始的位置
        //     Log.Message("[RkM] 开始匹配目标指令序列...");
        //     
        //     var addMethod = AccessTools.Method(typeof(List<Ability>), "Add");
        //     var pawnField = AccessTools.Field(typeof(Pawn_AbilityTracker), "pawn");
        //     var apparelField = AccessTools.Field(typeof(Pawn), "apparel");
        //     var cacheField = AccessTools.Field(typeof(Pawn_AbilityTracker), "allAbilitiesCached");
        //     
        //     Log.Message($"[RkM] 目标方法 - List.Add: {addMethod?.Name ?? "null"}");
        //     Log.Message($"[RkM] 目标字段 - pawn: {pawnField?.Name ?? "null"}, apparel: {apparelField?.Name ?? "null"}, allAbilitiesCached: {cacheField?.Name ?? "null"}");
        //     
        //     matcher.MatchStartForward(
        //         new CodeMatch(OpCodes.Callvirt, addMethod),
        //         new CodeMatch(OpCodes.Ldarg_0),
        //         new CodeMatch(OpCodes.Ldfld, pawnField),
        //         new CodeMatch(OpCodes.Ldfld, apparelField)
        //     );
        //     
        //     if (matcher.IsInvalid)
        //     {
        //         Log.Error("[RkM] Patch_AllAbilitiesForReading.Transpiler 匹配失败！未找到目标指令序列");
        //         return codeInstructions;
        //     }
        //     
        //     Log.Message($"[RkM] 匹配成功！当前位置：{matcher.Pos} / {matcher.Length}");
        //     Log.Message($"[RkM] 当前指令：{matcher.Instruction}");
        //     
        //     matcher.Advance(-3); // 回退到 callvirt 之后
        //     Log.Message($"[RkM] Advance(-3) 后位置：{matcher.Pos}");
        //     
        //     var helper = AccessTools.Method(typeof(Patch_AllAbilitiesForReading), nameof(AddEquippableAbilities));
        //     Log.Message($"[RkM] 辅助方法：{helper?.Name ?? "null"}");
        //     
        //     if (helper == null)
        //     {
        //         Log.Error("[RkM] 无法找到辅助方法 AddEquippableAbilities");
        //         return codeInstructions;
        //     }
        //     
        //     Log.Message($"[RkM] 准备插入 {5} 条新指令...");
        //     Log.Message($"[RkM] 插入前指令总数：{matcher.Length}");
        //     
        //     matcher.Insert(
        //         new CodeInstruction(OpCodes.Ldarg_0),
        //         new CodeInstruction(OpCodes.Ldfld, pawnField),
        //         new CodeInstruction(OpCodes.Ldarg_0),
        //         new CodeInstruction(OpCodes.Ldfld, cacheField),
        //         new CodeInstruction(OpCodes.Call, helper)
        //     );
        //     
        //     Log.Message($"[RkM] 插入完成！新指令总数：{matcher.Length}");
        //     Log.Message($"[RkM] 补丁应用成功！");
        //     
        //     // 输出修改后的指令序列（可选，用于详细调试）
        //     int index = 0;
        //     foreach (var instruction in matcher.InstructionEnumeration())
        //     {
        //         Log.Message($"[RkM] 指令 [{index++}]: {instruction}");
        //     }
        //     
        //     return matcher.InstructionEnumeration();
        // }
    }