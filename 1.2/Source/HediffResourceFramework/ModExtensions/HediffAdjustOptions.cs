﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class HediffOption
    {
        public HediffOption()
        {

        }

        public HediffDef hediff;
        public float severityOffset;
        public int verbIndex = -1;
        public string verbLabel;
        public bool disableOnEmptyOrMissingHediff;
        public float minimumSeverityCastRequirement = -1f;
        public string disableReason;
    }
    public class HediffAdjustOptions : DefModExtension
    {
        public List<HediffOption> hediffOptions;
    }
}
