﻿using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
	[HarmonyPatch(typeof(Projectile), "Launch", new Type[]
	{
		typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(Thing), typeof(ThingDef)
	})]
	public static class Patch_Projectile_Launch
	{
		public static void Postfix(Projectile __instance, Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Thing equipment = null, ThingDef targetCoverDef = null)
		{
			if (launcher is Pawn pawn && equipment is ThingWithComps eq && __instance.EquipmentDef == equipment.def)
			{
				var compCharge = eq.GetComp<CompChargeResource>();
				if (compCharge != null)
				{
					var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(compCharge.Props.hediffResource) as HediffResource;
					if (hediffResource != null && compCharge.Props.damageScaling.HasValue)
                    {
						Log.Message("Should do charging damage: " + __instance);
						compCharge.projectilesWithChargedResource[__instance] = hediffResource.ResourceAmount;
						hediffResource.ResourceAmount = 0f;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), "DamageAmount", MethodType.Getter)]
	internal static class DamageAmount_Patch
	{
		private static void Postfix(Projectile __instance, ref int __result)
		{
			if (__instance.Launcher is Pawn launcher)
			{
				var equipment = launcher.equipment?.Primary;
				if (equipment != null && __instance.EquipmentDef == equipment.def)
				{
					var compCharge = equipment.GetComp<CompChargeResource>();
					if (compCharge != null && compCharge.projectilesWithChargedResource.TryGetValue(__instance, out float resourceAmount))
					{
						Log.Message("1 instance - " + __instance + " - __result: " + __result + " - hediffResource: " + resourceAmount + " - compCharge.Props.damageScaling.HasValue: " + compCharge.Props.damageScaling.HasValue);
						switch (compCharge.Props.damageScaling.Value)
						{
							case DamageScalingMode.Flat: DoFlatDamage(ref __result, resourceAmount, compCharge); break;
							case DamageScalingMode.Scalar: DoScalarDamage(ref __result, resourceAmount, compCharge); break;
							case DamageScalingMode.Linear: DoLinearDamage(ref __result, resourceAmount, compCharge); break;
							default: break;
						}
						Log.Message("2 instance - " + __instance + " - result: " + __result + " - hediffResource: " + resourceAmount + " - compCharge.Props.damageScaling.HasValue: " + compCharge.Props.damageScaling.HasValue);
					}
				}
			}
		}

		private static void DoFlatDamage(ref int __result, float resourceAmount, CompChargeResource compCharge)
		{
			var oldDamage = __result;
			__result = (int)(__result + (compCharge.Props.damagePerCharge * (resourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge));
			Log.Message("Flat: old damage: " + oldDamage + " - new damage: " + __result);
		}
		private static void DoScalarDamage(ref int __result, float resourceAmount, CompChargeResource compCharge)
		{
			var oldDamage = __result;
			__result = (int)(__result * Mathf.Pow((1 + compCharge.Props.damagePerCharge), (resourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge));
			Log.Message("Scalar: old damage: " + oldDamage + " - new damage: " + __result);
		}

		private static void DoLinearDamage(ref int __result, float resourceAmount, CompChargeResource compCharge)
		{
			var oldDamage = __result;
			__result = (int)(__result * (1 + (compCharge.Props.damagePerCharge * (resourceAmount - compCharge.Props.minimumResourcePerUse) / compCharge.Props.resourcePerCharge)));
			Log.Message("Linear: old damage: " + oldDamage + " - new damage: " + __result);
		}
	}

	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal", new Type[] 
	{
		typeof(Vector3), typeof(float), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode), typeof(bool), typeof(bool), typeof(bool)
	})]
	public static class Patch_RenderPawnInternal
    {
		public static void Postfix(bool portrait, Pawn ___pawn)
		{
			if (portrait) return;
			foreach (var hediff in ___pawn.health.hediffSet.hediffs.OfType<HediffResource>())
			{
				hediff.Draw();
			}
		}
	}



	[HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
	public static class Patch_PreApplyDamage
	{
		private static void Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;

			var effectOnImpactOptions = dinfo.Def.GetModExtension<EffectOnImpact>();
			if (effectOnImpactOptions != null && __instance.health?.hediffSet != null)
			{
				foreach (var resourceEffect in effectOnImpactOptions.resourceEffects)
				{
					var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance, resourceEffect.hediffDef, resourceEffect.adjustTargetResource, resourceEffect.addHediffIfMissing);
					if (resourceEffect.delayTargetOnDamage != IntRange.zero)
					{
						hediffResource.AddDelay(resourceEffect.delayTargetOnDamage.RandomInRange);
					}
				}
			}

			var hediffResources = __instance?.health?.hediffSet?.hediffs?.OfType<HediffResource>().ToList();
			if (hediffResources != null)
            {
				for (int num = hediffResources.Count - 1; num >= 0; num--)
                {
					var hediff = hediffResources[num];
					if (dinfo.Amount > 0 && hediff.def.shieldProperties != null)
					{
						var shieldProps = hediff.def.shieldProperties;
						if (shieldProps.absorbRangeDamage && (dinfo.Weapon?.IsRangedWeapon ?? false))
						{
							ProcessDamage(__instance, ref dinfo, hediff, shieldProps);
						}
						else if (shieldProps.absorbMeleeDamage && (dinfo.Weapon is null || dinfo.Weapon.IsMeleeWeapon))
						{
							ProcessDamage(__instance, ref dinfo, hediff, shieldProps);
						}
						if (dinfo.Amount <= 0)
						{
							absorbed = true;
						}
						hediff.AbsorbedDamage(dinfo);
					}
				}
			}

		}

		private static void ProcessDamage(Pawn pawn, ref DamageInfo dinfo, HediffResource hediff, ShieldProperties shieldProps)
		{
			bool damageIsProcessed = false;
			if (shieldProps.resourceConsumptionPerDamage.HasValue && hediff.ResourceAmount >= shieldProps.resourceConsumptionPerDamage.Value)
			{
				if (shieldProps.maxAbsorb.HasValue)
				{
					dinfo.SetAmount(dinfo.Amount - shieldProps.maxAbsorb.Value);
				}
				else
				{
					dinfo.SetAmount(0);
				}
				hediff.ResourceAmount -= shieldProps.resourceConsumptionPerDamage.Value;
				damageIsProcessed = true;
			}
			else if (shieldProps.damageAbsorbedPerResource.HasValue)
			{
				var damageAmount = dinfo.Amount;
				if (shieldProps.maxAbsorb.HasValue && damageAmount > shieldProps.maxAbsorb.Value)
				{
					damageAmount = shieldProps.maxAbsorb.Value;
				}
				var resourceAmount = hediff.ResourceAmount;
				var ratioPerAbsorb = shieldProps.damageAbsorbedPerResource.Value;
				var resourceCost = damageAmount / ratioPerAbsorb;
				if (resourceAmount >= resourceCost)
				{
					dinfo.SetAmount(0f);
					hediff.ResourceAmount -= resourceCost;
				}
				else
				{
					damageAmount -= resourceAmount * ratioPerAbsorb;
					dinfo.SetAmount(damageAmount);
					hediff.ResourceAmount = 0;
				}
				damageIsProcessed = true;
			}

			if (damageIsProcessed && shieldProps.postDamageDelay.HasValue)
			{
				var apparels = pawn.apparel?.WornApparel?.ToList();
				if (apparels != null)
				{
					foreach (var apparel in apparels)
					{
						var hediffComp = apparel.GetComp<CompAdjustHediffs>();
						if (hediffComp != null && hediffComp.Props.resourceSettings != null)
						{
							var newDelayTicks = (int)(shieldProps.postDamageDelay.Value * hediffComp.Props.postDamageDelayMultiplier);
							foreach (var hediffOption in hediffComp.Props.resourceSettings)
							{
								var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
								Log.Message(apparel + " - hediffResource: " + hediffResource);
								Log.Message(apparel + " - hediffResource.CanHaveDelay(newDelayTicks): " + hediffResource?.CanHaveDelay(newDelayTicks));
								if (hediffResource != null && hediffResource.CanHaveDelay(newDelayTicks))
								{
									Log.Message(" - ProcessDamage - hediffResource.AddDelay(newDelayTicks);; - 32", true);
									hediffResource.AddDelay(newDelayTicks);
								}
							}
						}
					}
				}

				var equipments = pawn.equipment.AllEquipmentListForReading;
				if (equipments != null)
				{
					foreach (var equipment in equipments)
					{
						Log.Message(" - ProcessDamage - var hediffComp = equipment.GetComp<CompAdjustHediffs>(); - 37", true);
						var hediffComp = equipment.GetComp<CompAdjustHediffs>();
						Log.Message(" - ProcessDamage - if (hediffComp != null && hediffComp.Props.hediffOptions != null) - 38", true);
						if (hediffComp != null && hediffComp.Props.resourceSettings != null)
						{
							var newDelayTicks = (int)(shieldProps.postDamageDelay.Value * hediffComp.Props.postDamageDelayMultiplier);
							Log.Message(" - ProcessDamage - foreach (var hediffOption in hediffComp.Props.hediffOptions) - 40", true);
							foreach (var hediffOption in hediffComp.Props.resourceSettings)
							{
								Log.Message(" - ProcessDamage - var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource; - 41", true);
								var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
								if (hediffResource != null && hediffResource.CanHaveDelay(newDelayTicks))
								{
									Log.Message(" - ProcessDamage - hediffResource.AddDelay(newDelayTicks);; - 42", true);
									hediffResource.AddDelay(newDelayTicks);
								}
							}
						}
					}
				}
			}
		}
	}
}
