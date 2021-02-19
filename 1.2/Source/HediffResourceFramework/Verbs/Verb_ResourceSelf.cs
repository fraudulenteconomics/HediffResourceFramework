﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
    public class Verb_ResourceSelf : Verb_CastBase
    {
        public new VerbResourceProps verbProps => base.verbProps as VerbResourceProps;
        public override bool Available()
        {
            if (verbProps.targetResourceSettings != null)
            {
                foreach (var hediffOption in verbProps.targetResourceSettings)
                {
                    var hediffResource = this.CasterPawn.health.hediffSet.GetFirstHediffOfDef(hediffOption.hediff) as HediffResource;
                    if (hediffResource != null && hediffResource.ResourceAmount == hediffResource.ResourceCapacity)
                    {
                        return false;
                    }
                }

            }
            return true;
        }
        protected override bool TryCastShot()
        {
            if (this.CasterPawn != null)
            {
                if (verbProps.targetResourceSettings != null)
                {
                    foreach (var hediffOption in verbProps.targetResourceSettings)
                    {
                        HRFLog.Message("Giving: " + this.CasterPawn + " - " + hediffOption.hediff + " - " + hediffOption.resourcePerUse);
                        HediffResourceUtils.AdjustResourceAmount(this.CasterPawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                        if (hediffOption.effectRadius != -1f)
                        {
                            foreach (var cell in GenRadial.RadialCellsAround(this.CasterPawn.Position, hediffOption.effectRadius, true))
                            {
                                foreach (var pawn in cell.GetThingList(this.CasterPawn.Map).OfType<Pawn>())
                                {
                                    if (pawn != this.CasterPawn)
                                    {
                                        HediffResourceUtils.AdjustResourceAmount(pawn, hediffOption.hediff, hediffOption.resourcePerUse, hediffOption.addHediffIfMissing);
                                    }
                                }
                            }
                        }

                    }
                }
                if (base.EquipmentSource != null)
                {
                    base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
                    base.EquipmentSource.GetComp<CompReloadable>()?.UsedOnce();
                }
            }
            return true;
        }
    }
}
