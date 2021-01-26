﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class CompProperties_WeaponAdjustHediffs : CompProperties
    {
        public List<HediffOption> hediffOptions;

        public CompProperties_WeaponAdjustHediffs()
        {
            this.compClass = typeof(CompWeaponAdjustHediffs);
        }
    }
    public class CompWeaponAdjustHediffs : ThingComp
    {
        public CompProperties_WeaponAdjustHediffs Props
        {
            get
            {
                return (CompProperties_WeaponAdjustHediffs)this.props;
            }
        }

        private CompEquippable compEquippable;
        private CompEquippable CompEquippable
        {
            get
            {
                if (compEquippable is null)
                {
                    compEquippable = this.parent.GetComp<CompEquippable>();
                }
                return compEquippable;
            }
        }
        public override void CompTick()
        {
            base.CompTick();

            if (this.parent is ThingWithComps equipment && CompEquippable.PrimaryVerb.CasterPawn != null)
            {
                if (CompEquippable.PrimaryVerb.CasterPawn.IsHashIntervalTick(200))
                {
                    foreach (var option in Props.hediffOptions)
                    {
                        float num = option.resourceOffset;
                        num *= 0.00333333341f;
                        if (option.qualityScalesResourceOffset && equipment.TryGetQuality(out QualityCategory qc))
                        {
                            num *= HediffResourceUtils.GetQualityMultiplier(qc);
                        }
                        HediffResourceUtils.AdjustResourceAmount(CompEquippable.PrimaryVerb.CasterPawn, option.hediff, num, option);
                    }
                }
            }
        }
    }

}
