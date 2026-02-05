using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RkM.HarmonyPatches;

    [HarmonyPatch("DrawEquipmentAndApparelExtras")]
	[HarmonyPatch(typeof(PawnRenderUtility))]
	public static class DrawEquipment_WeaponBackPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(30000)]
		public static bool DrawEquipmentAndApparelExtrasPrefix(Pawn pawn, Vector3 drawPos)
		{
			if (pawn.Dead || !pawn.Spawned || pawn.equipment?.Primary == null ||
			    (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon) ||
			    pawn.equipment.Primary.TryGetComp<CompWeaponExtention>() is null)
				return true;
			var comp = pawn.equipment.Primary.GetComp<CompWeaponExtention>();
			Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
			bool flag = stance_Busy is { neverAimWeapon: false, focusTarg.IsValid: true };
			if (flag)
			{
				Vector3 vector = drawPos;
				Vector3 a;
				if (stance_Busy.focusTarg.HasThing) a = stance_Busy.focusTarg.Thing.DrawPos;
				else a = stance_Busy.focusTarg.Cell.ToVector3Shifted();

				float num = 0f;
				if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f) num = (a - pawn.DrawPos).AngleFlat();
				if (comp != null && comp.littleDown) vector.z += -0.2f;
				vector += new Vector3(0f, 0f, 0.4f).RotatedBy(num);
				vector.y += 0.0390625f;
				PawnRenderUtility.DrawEquipmentAiming(pawn.equipment.Primary, vector, num);
				DrawWornExtras(pawn.apparel);
			}
			else if ((pawn.carryTracker == null || pawn.carryTracker.CarriedThing == null) && (pawn.Drafted || (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) || (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon)))
			{
				Vector3 vector = drawPos;
				if (true)
				{
					switch (pawn.Rotation.AsInt)
					{
						case 0:
							vector += comp.Props.northOffset.position;
							DrawEquipmentAiming(pawn.equipment.Primary, vector, comp.Props.northOffset.angle);
							break;
						case 1:
							vector += comp.Props.eastOffset.position;
							vector.y += 0.0390625f;
							DrawEquipmentAiming(pawn.equipment.Primary, vector, comp.Props.eastOffset.angle);
							break;
						case 2:
							vector += comp.Props.southOffset.position;
							vector.y += 0.0390625f;
							DrawEquipmentAiming(pawn.equipment.Primary, vector, comp.Props.southOffset.angle, true);
							break;
						case 3:
							vector += comp.Props.westOffset.position;
							vector.y += 0.0390625f;
							DrawEquipmentAiming(pawn.equipment.Primary, vector, comp.Props.westOffset.angle, true);
							break;
					}
				}
			}
			else
			{
				DrawWornExtras(pawn.apparel);
			}
			return false;
		}

		public static void DrawWornExtras(Pawn_ApparelTracker tracker)
		{
			if (tracker != null) foreach (Apparel apparel in tracker.WornApparel) apparel.DrawWornExtras();
		}

		public static void DrawEquipmentAiming(Thing eq, Vector3 drawLoc, float aimAngle, bool isFlip = false)
		{
			Material material = eq.Graphic.MatSingleFor(eq);
			Vector3 s = new Vector3(eq.Graphic.drawSize.x, 0f, eq.Graphic.drawSize.y);
			Matrix4x4 matrix = Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(aimAngle, Vector3.up), s);
			if (isFlip) Graphics.DrawMesh(MeshPool.plane10Flip, matrix, material, 0);
			else Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
		}

		public const float drawYPosition = 0.0390625f;

		public const float drawSYSYPosition = 0.03904f;

		public const float littleDown = -0.2f;
	}