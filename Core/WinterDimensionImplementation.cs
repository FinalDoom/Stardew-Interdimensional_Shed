using FinalDoom.StardewValley.InterdimensionalShed.Dimensions;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class WinterDimensionImplementation : DefaultDimensionImplementation
    {
        public WinterDimensionImplementation(DimensionInfo info, Item item, int dimensionIndex) : base(info, item, dimensionIndex)
        {
        }

        public override int CurrentStage()
        {
            var stage = base.CurrentStage();
            if (stage == 5 && Game1.player.mailReceived.Contains(WinterDimension.mail_santaLetter))
            {
                stage = 6;
            }
            return stage;
        }
    }
}
