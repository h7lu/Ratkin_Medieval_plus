using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RkM;

public class JobDriver_PlaySong : JobDriver_CastAbility
    {
        private int ticksPlaying = 0;
        //private CompSongAbility songComp;
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !job.ability.CanCast && !job.ability.Casting);
            AddFinishAction(delegate(JobCondition condition)
            {
                if (job.ability != null && job.def.abilityCasting)
                {
                    job.ability.StartCooldown(job.ability.def.cooldownTicksRange.RandomInRange);
                }
            });
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate()
            {
                pawn.pather.StopDead();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            Stance_Warmup stance = new Stance_Warmup(300, LocalTargetInfo.Invalid, job.verbToUse);
            Toil playToil = new Toil
            {
                initAction = () =>
                {
                    ticksPlaying = 0;
                    pawn.pather.StopDead();
                    
                    // 手动创建预热姿态，开始预热倒计时
                    if (job.verbToUse != null)
                    {
                        //int warmupTicks = (int)(job.verbToUse.verbProps.warmupTime.SecondsToTicks());
                        // 使用自定义演奏时长（例如 5 秒）
                        //int customDuration = 300; // 5 秒 * 60 ticks/秒
//Stance_Warmup stance = new Stance_Warmup(warmupTicks, LocalTargetInfo.Invalid, job.verbToUse);
                        pawn.stances.SetStance(stance);
                    }
                },
                tickAction = () =>
                {
                    ticksPlaying++;
                                
                    // 每 1 秒显示一次演奏效果
                    if (ticksPlaying % 60 == 0)
                    {
                        MoteMaker.ThrowText(pawn.DrawPos, Map, "演奏中🎶...", Color.white);
                    }
                                
                    // 完成演奏（预热完成）
                    if (stance.ticksLeft <= 0f )
                    {
                        ReadyForNextToil();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            // 为 playToil 添加进度条显示
            //playToil.WithProgressBar(TargetIndex.A, () => job.verbToUse?.WarmupProgress ?? 0f, false, -0.5f, false);
            yield return playToil;
            
            Toil toil2 = Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, false);
            if (job.ability != null && job.ability.def.showCastingProgressBar && job.verbToUse != null)
            {
                toil2.WithProgressBar(TargetIndex.A, () => job.verbToUse.WarmupProgress, false, -0.5f, false);
            }
            yield return toil2;
        }
        
    }