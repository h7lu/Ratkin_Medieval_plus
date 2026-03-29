using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RkM;

public class CompProperties_SongAbility : CompProperties
{
    public string label = "技能";
    public string description = "技能描述";
    public string iconPath;
    public float radius = 10f;
    public int castingTimeTicks = 120;
    public int cooldownTicks = 30000; // 12小时
    public SongEffect songEffect;
        
    public CompProperties_SongAbility() => compClass = typeof(CompSongAbility);
}


public class CompSongAbility : CompEquipmentWithGizmos
    {
        public CompProperties_SongAbility Props => (CompProperties_SongAbility)props;
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (ParentHolder.ParentHolder is not Pawn wearer || wearer.Faction != Faction.OfPlayer)
                yield break;
                
            yield return new Command_Action
            {
                defaultLabel = Props.label,
                defaultDesc = Props.description + $"\n范围: {Props.radius}格\n冷却: {Props.cooldownTicks / 2500}小时\n施法时间: {Props.castingTimeTicks / 60}秒",
                icon = ContentFinder<Texture2D>.Get(Props.iconPath, false) ?? BaseContent.BadTex,
                Disabled = ticks > 0,
                disabledReason = ticks > 0 ? $"冷却中 (剩余: {ticks / 2500}小时)" : null,
                action = () => StartPlaying(wearer)
            };
        }
        
        private void StartPlaying(Pawn pawn)
        {
            int index = parent.GetComps<CompSongAbility>().FirstIndexOf(c => c.Props.label == Props.label);
            Job job = JobMaker.MakeJob(RkMDefOf.RkM_PlaySong, pawn, parent);
            job.verbToUse = null;
            job.count = index;
            job.expiryInterval = Props.castingTimeTicks;
            job.playerForced = true;
            
            job.placedThings ??= new List<ThingCountClass>();
            job.placedThings.Add(new ThingCountClass(parent, 1));
            
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
        
        public void ApplyEffect(Pawn caster)
        {
            Props.songEffect.ApplyEffect(caster, caster.Position, Props.radius);
            ticks = Props.cooldownTicks;
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(caster.Map);
        }
        
        public override string CompInspectStringExtra()
        {
            if (ticks > 0) return $"{Props.label}: 冷却中 (剩余: {ticks / 2500}小时)";
            return base.CompInspectStringExtra();
        }
    }
    public abstract class SongEffect
    {
        public int durationTicks = 360000; // 1小时
        public abstract void ApplyEffect(Pawn caster, IntVec3 center, float radius);
        protected List<Pawn> GetAffectedPawns(IntVec3 center, float radius, Map map)
        {
            List<Pawn> affectedPawns = [];
            
            var pawnsInRange = GenRadial.RadialDistinctThingsAround(center, map, radius, true).OfType<Pawn>().ToList();
            Log.Message ($"bf:{pawnsInRange}");
            foreach (Pawn pawn in pawnsInRange)
                if (!pawn.Dead && pawn.needs != null) affectedPawns.Add(pawn);
            Log.Message ($"af:{affectedPawns}");
            return affectedPawns;
        }
    }
    
    public class SongEffect_AddJoy : SongEffect
    {
        public float joyAmount = 0.5f;
        public override void ApplyEffect(Pawn caster, IntVec3 center, float radius)
        {
            Map map = caster.Map;
            var affectedPawns = GetAffectedPawns(center, radius, map);
            foreach (var pawn in affectedPawns)
            {
                if (pawn.needs.joy != null)
                {
                    pawn.needs.joy.CurLevel += joyAmount;
                    MoteMaker.ThrowText(pawn.DrawPos, map, "+娱乐", Color.yellow);
                }
            }
            Messages.Message($"{caster.LabelShort} 演奏了欢快之歌，为周围的人们带来了欢乐。",
                MessageTypeDefOf.PositiveEvent);
        }
    }
    
    public class SongEffect_AddThought : SongEffect
    {
        public ThoughtDef thoughtDef;
        public override void ApplyEffect(Pawn caster, IntVec3 center, float radius)
        {
            Map map = caster.Map;
            if (thoughtDef == null)
                return;
            List<Pawn> affectedPawns = GetAffectedPawns(center, radius, map);
            foreach (Pawn pawn in affectedPawns)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef, null);
                
                MoteMaker.ThrowText(pawn.DrawPos, map, "+心情", Color.green);
            }
            
            Messages.Message($"{caster.LabelShort} 演奏了振奋之歌，激励了周围的人们。",
                MessageTypeDefOf.PositiveEvent);
        }
    }
    
    public class SongEffect_AddHediff : SongEffect
    {
        public HediffDef hediffDef;
        public override void ApplyEffect(Pawn caster, IntVec3 center, float radius)
        {
            Map map = caster.Map;
            if (hediffDef == null)
                return;
            List<Pawn> affectedPawns = GetAffectedPawns(center, radius, map);
            foreach (Pawn pawn in affectedPawns)
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1f;
                
                HediffComp_Disappears disappearsComp = hediff.TryGetComp<HediffComp_Disappears>();
                if (disappearsComp != null)
                {
                    disappearsComp.ticksToDisappear = durationTicks;
                }
                
                pawn.health.AddHediff(hediff);
                
                MoteMaker.ThrowText(pawn.DrawPos, map, "+速度", Color.red);
            }
            
            Messages.Message($"{caster.LabelShort} 演奏了激昂之歌，鼓舞了周围的人们。",
                MessageTypeDefOf.PositiveEvent);
        }
    }