using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RkM
{
    public class CompProperties_RitualStarter : CompProperties
    {
        public string ritualDefName;

        public CompProperties_RitualStarter()
        {
            compClass = typeof(CompRitualStarter);
        }
    }

    public class CompRitualStarter : ThingComp
    {
        public CompProperties_RitualStarter Props => (CompProperties_RitualStarter)props;

        /// <summary>Cached ritual precept so we don't search every frame.</summary>
        private Precept_Ritual cachedRitual;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            var command = new Command_Action();
            command.defaultLabel = "Start lecture";
            command.defaultDesc = "Begin the teaching ritual at this lectern.";
            command.icon = parent.def.uiIcon;
            command.action = () => TryOpenBeginRitualDialog();

            yield return command;
        }

        /// <summary>
        /// Find the teaching ritual on any player ideo.
        /// If it doesn't exist yet, create it from the PreceptDef and add it to the
        /// primary ideo so the vanilla ritual system can manage it normally.
        ///
        /// Why init:false + manual Fill?
        ///   We keep ritual initialization explicit and stable by skipping Init and
        ///   letting RitualPatternDef.Fill() populate ritual fields.
        /// </summary>
        private Precept_Ritual FindOrCreateRitual()
        {
            // Return cached reference if still valid.
            if (cachedRitual != null)
                return cachedRitual;

            string targetDefName = Props.ritualDefName;
            if (targetDefName.NullOrEmpty())
            {
                Log.Error("[RkM] CompRitualStarter has no ritualDefName set.");
                return null;
            }

            // --- 1. Search every player ideo (primary + minor) ---
            var ideoTracker = Faction.OfPlayer?.ideos;
            if (ideoTracker == null)
            {
                Log.Error("[RkM] Player faction has no ideo tracker.");
                return null;
            }

            foreach (var ideo in ideoTracker.AllIdeos)
            {
                var found = ideo.PreceptsListForReading
                    .OfType<Precept_Ritual>()
                    .FirstOrDefault(p => p.def.defName == targetDefName);
                if (found != null)
                {
                    cachedRitual = found;
                    return cachedRitual;
                }
            }

            // --- 2. Not on any ideo — create from def and inject ---
            var preceptDef = DefDatabase<PreceptDef>.GetNamed(targetDefName, errorOnFail: false);
            if (preceptDef == null)
            {
                Log.Error($"[RkM] PreceptDef '{targetDefName}' not found in DefDatabase.");
                return null;
            }

            var primaryIdeo = ideoTracker.PrimaryIdeo;
            if (primaryIdeo == null)
            {
                Log.Error("[RkM] Player faction has no primary ideo — cannot add ritual.");
                return null;
            }

            try
            {
                var newRitual = (Precept_Ritual)PreceptMaker.MakePrecept(preceptDef);

                // Use init:false and pass fillWith so AddPrecept applies
                // RitualPatternDef.Fill internally; this sets behavior, triggers,
                // isAnytime, outcome, etc. Fall back to direct Fill if needed.
                primaryIdeo.AddPrecept(newRitual, init: false,
                    generatingFor: null,
                    fillWith: preceptDef.ritualPatternBase);

                // Fill sets behavior and flags; if it didn't set the name, do it now.
                // Precept_TeachingRitual.GenerateNameRaw() returns def.label safely.
                if (newRitual.Label.NullOrEmpty())
                    newRitual.GenerateNewName();

                // If Fill was not applied by AddPrecept (engine version difference),
                // call it directly as a safety net.
                if (newRitual.behavior == null && preceptDef.ritualPatternBase != null)
                {
                    preceptDef.ritualPatternBase.Fill(newRitual);
                    Log.Message("[RkM] Called Fill() directly (AddPrecept did not apply fillWith).");
                }

                cachedRitual = newRitual;
                Log.Message($"[RkM] Auto-added teaching ritual '{targetDefName}' to ideo '{primaryIdeo.name}'.");
            }
            catch (Exception ex)
            {
                Log.Error($"[RkM] Failed to create ritual precept '{targetDefName}': {ex}");
                return null;
            }

            return cachedRitual;
        }

        private void TryOpenBeginRitualDialog()
        {
            if (parent?.Map == null)
                return;

            var map = parent.Map;
            var ritual = FindOrCreateRitual();

            if (ritual == null)
            {
                Messages.Message("Cannot start lecture — teaching ritual could not be initialised.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            TargetInfo target = new TargetInfo(parent.Position, map, false);

            Dialog_BeginRitual.ActionCallback action = (RitualRoleAssignments assignments) =>
            {
                try
                {
                    ritual.behavior.TryExecuteOn(target, null, ritual, null, assignments, playerForced: true);
                }
                catch (Exception ex)
                {
                    Log.Error("[RkM] Error starting teaching ritual: " + ex);
                    return false;
                }
                return true;
            };

            var outcomeDefFallback = ritual.def?.ritualPatternBase?.ritualOutcomeEffect;
            Find.WindowStack.Add(new Dialog_BeginRitual(
                ritual.Label, ritual, target, map, action,
                null, null, null, null, null, null, outcomeDefFallback));
        }
    }
}
