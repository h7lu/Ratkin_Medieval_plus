using System;
using Verse;
using RimWorld;

namespace RkM
{
    // DefModExtension to hold configurable values for the teaching ritual outcome
    public class RitualGiveClassXPProps : DefModExtension
    {
        // The skill to give XP to (set in XML as a defName of a SkillDef)
        public SkillDef skill;

        // Base XP to grant per participant (can be tuned per outcome)
        public float baseXp = 2000f;

        // Optional multiplier for the 'teacher' role
        public float teacherMultiplier = 1.2f;

        // Optional multiplier for 'student' role
        public float studentMultiplier = 1f;

    // Podium checks are unnecessary because the ritual must start at a lectern/podium; removed.

    // List of allowed blackboard ThingDef defNames. Example: Biotech blackboard def.
    public System.Collections.Generic.List<string> blackboardThingDefs;

        // Per-outcome tuning (multipliers/loss fractions applied to baseXp):
        // Loss fractions are positive numbers representing fraction of baseXp lost for listeners.
        public float terribleLossFraction = 0.5f; // listeners lose 50% of baseXp
        public float boringLossFraction = 0.2f; // listeners lose 20% of baseXp
        public float funMultiplier = 1f; // normal XP multiplier for "fun"
        public float unforgettableMultiplier = 2f; // XP multiplier for "unforgettable"
        public float unforgettableInspirationChance = 0.02f; // chance to start an inspiration on unforgettable
    // Tuning: impressiveness threshold and mapping
    public int impressivenessThreshold = 50;
    // percent per point above threshold (0.01 => 1% per point)
    public float impressivenessPerPoint = 0.01f;
    public float impressivenessMinClamp = -0.2f;
    public float impressivenessMaxClamp = 0.5f;

    // Skill thresholds and per-level bonuses
    public int themeSkillThreshold = 8;
    // fraction per level above threshold (0.05 => 5% per level)
    public float themePerLevel = 0.07f;
    public int socialSkillThreshold = 8;
    public float socialPerLevel = 0.05f;

    // Blackboard tuning
    public int blackboardMaxCount = 3;
    public float blackboardPerCount = 0.1f; // 10% per blackboard up to max

    // Attendance tuning
    public int attendanceRequired = 3;
    
    // Attendance quality offset curve: negative offset for crowding
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
