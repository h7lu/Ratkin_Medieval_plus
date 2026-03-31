using SYS;
using Verse;

namespace RkM.HarmonyPatches;

public class CompProperties_WeaponExtention : CompProperties
{
    public CompProperties_WeaponExtention() => compClass = typeof(CompWeaponExtention);

    public Offset northOffset;
    public Offset eastOffset;
    public Offset southOffset;
    public Offset westOffset;
    public bool littleDown;
}
public class CompWeaponExtention : ThingComp
{
    public CompProperties_WeaponExtention Props => (CompProperties_WeaponExtention)props;
    public bool LittleDown => Props.littleDown;
}