using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RkM;

public class JobDriver_PlaySong : JobDriver
    {
        private int ticksPlaying = 0;
        private CompSongAbility songComp;
        
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 获取技能组件
            Log.Message($"{pawn.Name} 尝试播放技能：{job.count}");
            if (TargetThingB is ThingWithComps lute)
                if (lute.GetComps<CompSongAbility>().ToList()[job.count] is { } comp) songComp = comp;
            // 走到合适位置（可选）
            //yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            
            // 开始演奏
            Toil playToil = new Toil();
            playToil.initAction = () =>
            {
                ticksPlaying = 0;
                pawn.pather.StopDead();
            };
            
            playToil.tickAction = () =>
            {
                ticksPlaying++;
                
                // 每1秒显示一次演奏效果
                if (ticksPlaying % 60 == 0)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, Map, "演奏中...", Color.white);
                    //MoteMaker.(pawn.Position, Map, ThingDefOf.);
                }
                
                // 完成演奏
                if (ticksPlaying >= songComp?.Props.castingTimeTicks) ReadyForNextToil();
            };
            
            playToil.defaultCompleteMode = ToilCompleteMode.Never;
            playToil.WithProgressBar(TargetIndex.A, 
                () => (float)ticksPlaying / songComp?.Props.castingTimeTicks ?? 1f,
                false, -0.5f);
            yield return playToil;
            
            // 应用效果
            yield return new Toil
            {
                
                initAction = () => songComp?.ApplyEffect(pawn),
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }