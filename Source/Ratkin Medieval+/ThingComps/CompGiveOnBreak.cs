using RimWorld;
using Verse;

namespace RkM;

public enum GiveOnBreakQualityMode
{
    random,
    inherit,
    lower,
    custom
}

public class CompProperties_GiveOnBreak : CompProperties
{
    public ThingDef item;
    public bool alwaysDrop = false;
    public bool alwaysForbidden = false;
    public string warnMessage;
    public GiveOnBreakQualityMode qualitymode = GiveOnBreakQualityMode.random;
    public QualityCategory custom = QualityCategory.Normal;

    public CompProperties_GiveOnBreak()
    {
        compClass = typeof(CompGiveOnBreak);
    }
}

public class CompGiveOnBreak : ThingComp
{
    public CompProperties_GiveOnBreak Props => (CompProperties_GiveOnBreak)props;

    private bool replaced;

    public void TryGiveOnBreakBeforeDestroy(DestroyMode mode)
    {
        if (replaced || Props.item == null)
        {
            return;
        }

        if (!parent.def.useHitPoints || parent.HitPoints > 0)
        {
            return;
        }

        replaced = true;

        bool wasSpawned = parent.Spawned;
        ThingOwner owner = parent.holdingOwner;
        Map map = parent.MapHeld;
        IntVec3 pos = parent.PositionHeld;
        bool originalForbidden = parent.IsForbidden(Faction.OfPlayer);

        ThingDef stuffDef = parent.Stuff;
        Thing newThing = Props.item.MadeFromStuff && stuffDef != null
            ? ThingMaker.MakeThing(Props.item, stuffDef)
            : ThingMaker.MakeThing(Props.item);

        ApplyQuality(newThing);

        bool shouldForceDrop = Props.alwaysDrop;
        bool shouldForceForbidden = Props.alwaysDrop && Props.alwaysForbidden;

        if (!shouldForceDrop && !wasSpawned)
        {
            if (owner != null && owner.TryAdd(newThing, canMergeWithExistingStacks: true))
            {
                if (newThing.Spawned)
                {
                    newThing.SetForbidden(originalForbidden, warnOnFail: false);
                }
                TryWarn();
                return;
            }
        }

        if (map != null)
        {
            IntVec3 dropPos = pos.IsValid ? pos : parent.PositionHeld;
            GenPlace.TryPlaceThing(newThing, dropPos, map, ThingPlaceMode.Near, out Thing placedThing);
            Thing thingToForbid = placedThing ?? newThing;
            if (shouldForceForbidden)
            {
                thingToForbid.SetForbidden(value: true, warnOnFail: false);
            }
            else
            {
                thingToForbid.SetForbidden(value: originalForbidden, warnOnFail: false);
            }
        }
        else
        {
            newThing.Destroy(DestroyMode.Vanish);
        }

        TryWarn();
    }

    private void ApplyQuality(Thing newThing)
    {
        CompQuality newQualityComp = newThing.TryGetComp<CompQuality>();
        if (newQualityComp == null)
        {
            return;
        }

        CompQuality oldQualityComp = parent.TryGetComp<CompQuality>();
        QualityCategory finalQuality;

        switch (Props.qualitymode)
        {
            case GiveOnBreakQualityMode.inherit:
                if (oldQualityComp != null)
                {
                    finalQuality = oldQualityComp.Quality;
                }
                else
                {
                    finalQuality = RandomQuality();
                }
                break;
            case GiveOnBreakQualityMode.lower:
                if (oldQualityComp != null)
                {
                    finalQuality = oldQualityComp.Quality > QualityCategory.Awful
                        ? oldQualityComp.Quality - 1
                        : QualityCategory.Awful;
                }
                else
                {
                    finalQuality = RandomQuality();
                }
                break;
            case GiveOnBreakQualityMode.custom:
                finalQuality = Props.custom;
                break;
            default:
                finalQuality = RandomQuality();
                break;
        }

        newQualityComp.SetQuality(finalQuality, null);
    }

    private static QualityCategory RandomQuality()
    {
        return (QualityCategory)Rand.RangeInclusive((int)QualityCategory.Awful, (int)QualityCategory.Legendary);
    }

    private void TryWarn()
    {
        if (Props.warnMessage.NullOrEmpty())
        {
            return;
        }

        Messages.Message(Props.warnMessage, MessageTypeDefOf.NeutralEvent);
    }
}
