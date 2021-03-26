﻿using StardewValley;
using Object = StardewValley.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    public class DimensionImplementation : IDimensionImplementation
    {
        protected readonly IInterdimensionalShedExtraDimensionModConfig config;
        protected readonly DimensionInfo dimensionInfo;
        protected readonly Item item;
        protected readonly int maxItemsConsumed;
        protected readonly int dimensionIndex;
        protected int totalDimensionCount;
        public int TotalDimensionCount { get => totalDimensionCount; set => totalDimensionCount = value; }
        protected int unlockedDimensionCount;
        public int DiscoveredDimensionCount { get => unlockedDimensionCount; set => unlockedDimensionCount = value; }

        public Item Item { get => item; }

        public DimensionImplementation(IInterdimensionalShedExtraDimensionModConfig config, DimensionInfo info, Item item, int dimensionIndex)
        {
            this.config = config;
            dimensionInfo = info;
            this.item = item;
            maxItemsConsumed = Math.Min(dimensionInfo.StageRequirement(dimensionInfo.Stages.Max()), this.item.maximumStackSize());
            this.dimensionIndex = dimensionIndex;
        }

        public virtual int CurrentStage()
        {
            if (item.Stack == int.MaxValue)
            {
                return -1;
            }
            var stage = 0;
            foreach (var s in dimensionInfo.Stages)
            {
                if (dimensionInfo.StageRequirement(s) > item.Stack)
                {
                    break;
                }
                stage = s;
            }
            return stage;
        }

        public virtual bool HintAllowed()
        {
            var hintConfig = config.DimensionHints;
            if (hintConfig == HintConfig.All)
            {
                return true;
            }
            if (hintConfig == HintConfig.Daily)
            {
                var daysPlayed = Game1.stats.daysPlayed;
                // Plus or minus one day
                if (dimensionIndex >= ((daysPlayed + totalDimensionCount - 1) % totalDimensionCount) && dimensionIndex <= ((daysPlayed + totalDimensionCount + 1) % totalDimensionCount))
                {
                    return true;
                }
            }
            if (hintConfig == HintConfig.Random)
            {
                var r = new Random((int)Game1.stats.daysPlayed * unlockedDimensionCount + (int)Game1.uniqueIDForThisGame / 2);
                var count = r.Next(4) + 1;
                for (var i = 0; i < count; ++i)
                {
                    if (dimensionIndex == r.Next(totalDimensionCount))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual string CurrentDescription()
        {
            var stage = CurrentStage();
            if (stage == -1)
            {
                return HintAllowed() ? dimensionInfo.Hint : null;
            }
            if (stage >= 3 && dimensionInfo.Description3 != null && dimensionInfo.Description3 != "")
            {
                return dimensionInfo.Description3;
            }
            if (stage >= 2 && dimensionInfo.Description2 != null && dimensionInfo.Description2 != "")
            {
                return dimensionInfo.Description2;
            }
            return dimensionInfo.Description1;
        }

        public virtual bool CanAdd(Item item)
        {
            if (dimensionInfo.IgnoreQuality && item is Object o && this.item is Object t)
            {
                var actualQuality = o.Quality;
                o.Quality = t.Quality;
                var canStack = t.canStackWith(o) && o.Stack >= 1 && (t.Stack == int.MaxValue || t.Stack <= maxItemsConsumed);
                o.Quality = actualQuality;
                return canStack;
            }
            return this.item.canStackWith(item) && item.Stack >= 1 && (this.item.Stack == int.MaxValue || this.item.Stack <= maxItemsConsumed);
        }

        public virtual Item Add(Item item)
        {
            if (!CanAdd(item))
            {
                return item;
            }

            if (this.item.Stack == int.MaxValue)
            {
                this.item.Stack = 0;
            }
            int remainder;
            if (dimensionInfo.IgnoreQuality && item is Object o && this.item is Object t)
            {
                var actualQuality = o.Quality;
                o.Quality = t.Quality;
                remainder = t.addToStack(o);
                o.Quality = actualQuality;
            }
            else
            {
                remainder = this.item.addToStack(item);
            }
            if (this.item.Stack > maxItemsConsumed)
            {
                remainder += this.item.Stack - maxItemsConsumed;
                this.item.Stack = maxItemsConsumed;
            }
            if (remainder <= 0)
            {
                return null;
            }
            item.Stack = remainder;
            return item;
        }
    }
}