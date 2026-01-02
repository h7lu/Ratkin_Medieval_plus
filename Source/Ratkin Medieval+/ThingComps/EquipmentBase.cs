using Verse;

namespace RkM;

public abstract class CompEquipmentWithGizmos : ThingComp
{
    public int ticks = 0;
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref ticks, "ticks", 0);
    }
}