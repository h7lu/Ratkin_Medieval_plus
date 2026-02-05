using System.Collections.Generic;
using Verse;

namespace RkM;

public class CompProperties_GetEquipmentGizmo : CompProperties
{
    public CompProperties_GetEquipmentGizmo() => compClass = typeof(CompGetEquipmentGizmo);
}
public class CompGetEquipmentGizmo : ThingComp
{
    public CompProperties_GetEquipmentGizmo Props => props as CompProperties_GetEquipmentGizmo;
    private int ticks;
    public override void CompTick()
    {
        base.CompTick();
        ticks++;
        if (ticks < 60) return;
        ticks = 0;
        if (parent is not Pawn pawn || pawn.Dead || !pawn.Spawned || pawn.equipment == null) return;
        var primary = pawn.equipment.Primary;
        if (primary == null) return;
        foreach (var comp in primary.GetComps<CompEquipmentWithGizmos>())
            if (comp is { ticks: >= 0 }) comp.ticks -= 60;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent is not Pawn p || p.Dead || !p.Spawned || p.equipment == null || !p.Drafted) yield break;
        var thing = p.equipment.Primary;
        if (thing == null) yield break;
        foreach (var comp in thing.GetComps<CompEquipmentWithGizmos>())
            if (comp != null) foreach (var g in comp.CompGetGizmosExtra()) yield return g;
    }
}
