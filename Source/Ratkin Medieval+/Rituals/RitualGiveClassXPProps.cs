using System;
using Verse;
using RimWorld;

namespace RkM
{
    public class RitualGiveClassXPProps : DefModExtension
    {
        public SkillDef skill;

        public float baseXp = 2000f;

        public float teacherMultiplier = 1.2f;

        public float studentMultiplier = 1f;

        public System.Collections.Generic.List<string> blackboardThingDefs;

        public float terribleLossFraction = 0.5f;
        public float boringLossFraction = 0.2f;
        public float funMultiplier = 1f;
        public float unforgettableMultiplier = 2f;
        public float unforgettableInspirationChance = 0.02f;
        public int impressivenessThreshold = 50;
        public float impressivenessPerPoint = 0.01f;
        public float impressivenessMinClamp = -0.2f;
        public float impressivenessMaxClamp = 0.5f;

        public int themeSkillThreshold = 8;
        public float themePerLevel = 0.07f;
        public int socialSkillThreshold = 8;
        public float socialPerLevel = 0.05f;

        public int blackboardMaxCount = 3;
        public float blackboardPerCount = 0.1f;

        public int attendanceRequired = 3;
        
        public SimpleCurve attendanceOffsetCurve = new SimpleCurve
        {
            new CurvePoint(0, 0f),
            new CurvePoint(2, 0f),
            new CurvePoint(3, -0.05f),
            new CurvePoint(5, -0.1f),
            new CurvePoint(10, -0.3f),
            new CurvePoint(15, -0.4f),
            new CurvePoint(20, -0.5f)
        };
    }
}
