using SYS;
using Verse;

namespace RkM.HarmonyPatches;

public class CompProperties_WeaponExtention : CompProperties
{
    public CompProperties_WeaponExtention()
    {
        compClass = typeof(CompWeaponExtention);
    }

    public Offset northOffset;
    public Offset eastOffset;
    public Offset southOffset;
    public Offset westOffset;
    public bool littleDown = false;
}
public class CompWeaponExtention : ThingComp
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Props = (CompProperties_WeaponExtention)props;
        if (Props.littleDown) littleDown = true;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Props = (CompProperties_WeaponExtention)props;
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        Props = (CompProperties_WeaponExtention)props;
        if (Props.littleDown) littleDown = true;
    }

    public bool littleDown;

    public CompProperties_WeaponExtention Props;
}