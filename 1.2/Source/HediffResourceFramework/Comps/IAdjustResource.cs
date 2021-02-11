﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HediffResourceFramework
{
	public interface IAdjustResource
	{
        void Register();
        void Deregister();
        void Notify_Removed();
        void ResourceTick();
        Dictionary<Verb, VerbDisable> PostUseDelayTicks { get; }
        Thing Parent { get; }
        List<HediffAdjust> ResourceSettings { get; }
        string DisablePostUse { get; }

        bool TryGetQuality(out QualityCategory qc);
    }
}