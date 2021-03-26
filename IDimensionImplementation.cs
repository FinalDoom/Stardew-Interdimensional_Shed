﻿using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    public interface IDimensionImplementation
    {
        public int CurrentStage();
        public string CurrentDescription();
        public bool HintAllowed();
        public bool CanAdd(Item item);
        public Item Add(Item item);
        public Item Item { get; }
    }
}