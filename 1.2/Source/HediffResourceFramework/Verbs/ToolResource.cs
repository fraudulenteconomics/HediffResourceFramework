﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HediffResourceFramework
{
    public class ToolResource : Tool
    {
        public List<HediffOption> resourceSettings;

        public List<HediffOption> targetResourceSettings;

        public List<ChargeSettings> chargeSettings;
    }
}
