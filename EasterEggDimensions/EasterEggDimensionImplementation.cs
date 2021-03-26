using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed.EasterEggDimensions
{
    internal class EasterEggDimensionImplementation : DimensionImplementation
    {
        public EasterEggDimensionImplementation(IInterdimensionalShedExtraDimensionModConfig config, DimensionInfo info, Item item, int dimensionIndex) : base(config, info, item, dimensionIndex)
        {
        }

        public override bool HintAllowed()
        {
            return false;
        }
        public override bool CanAdd(Item item)
        {
            return (this.item.Stack == 0 || this.item.Stack == int.MaxValue) && item.Stack == dimensionInfo.StageRequirement(1) && base.CanAdd(item);
        }
    }
}
