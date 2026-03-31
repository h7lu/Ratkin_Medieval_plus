using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RkM;

public class CompAbilityEffect_OnlyTargetNotHostiles : CompAbilityEffect
{
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        return parent.pawn != null && !parent.pawn.HostileTo(target.Thing);
    }

    public override bool Valid(GlobalTargetInfo target, bool throwMessages = false)
    {
        return parent.pawn != null && !parent.pawn.HostileTo(target.Thing);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return parent.pawn != null && !parent.pawn.HostileTo(target.Thing);
    }
}