using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class ModConfig : IInterdimensionalShedExtraDimensionModConfig
    {
        public bool OverrideFirstItem { get; set; } = false;
        public int? FirstItemId { get; set; } = null;
        public int? FirstItemCount { get; set; } = null;
        public bool OverrideSecondItem { get; set; } = false;
        public int? SecondItemId { get; set; } = null;
        public int? SecondItemCount { get; set; } = null;
        public bool OverrideThirdItem { get; set; } = false;
        public int? ThirdItemId { get; set; } = null;
        public int? ThirdItemCount { get; set; } = null;
        public int GoldCost { get; set; } = 75000;
        public HintConfig DimensionHints { get; set; } = HintConfig.None;
    }
}
