using System;
using RimWorld;
using Verse;

namespace RkM;

public class AccidentDef : Def
{
    public Type workClass = typeof(Accident);
    public float radius;
    public DamageDef damage;
    public int amount;
    public float chance = 1f;
    public float armourPenetration = 0;
}
public class Accident
{ 
    public AccidentDef def;
    public virtual void Do(Map map, IntVec3 c) { }
}
public class Accident_Burn : Accident
{
    public override void Do(Map map, IntVec3 c)
    {
        GenSpawn.Spawn(ThingDefOf.Fire, c, map);
    }
}
public class Accident_Explosion : Accident
{
    public override void Do(Map map, IntVec3 c)
    {
        GenExplosion.DoExplosion(c, map, 
            def.radius, def.damage, null, def.amount,
            def.armourPenetration, SoundDefOf.Explosion_FirefoamPopper, 
            null, null, null, 
            null, 0f, 0);
    }
}
//生成毒气的Accident
public class Accident_PoisonGas : Accident
{ 
    public override void Do(Map map, IntVec3 c)
    {
        GenExplosion.DoExplosion(
            c,           // 爆炸中心
            map,                // 地图
            def.radius,                    // 爆炸半径
            DamageDefOf.ToxGas,        // 伤害类型：毒气
            null,          // 发起者
            -1, -1f,                   // 无直接伤害
            null, null, null, null,    // 无音效、武器等
            null, 0f, 1,              
            GasType.ToxGas,  // 毒气类型
            null, 255, false, null, 0f, 1, 0f, false, null, null, null, true, 1f, 0f, true, null, 1f, null, null, null, null             // 单一生成物
        );
    }
}
