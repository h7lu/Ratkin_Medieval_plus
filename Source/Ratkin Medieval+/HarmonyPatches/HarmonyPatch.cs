using System.Reflection;
using HarmonyLib;
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