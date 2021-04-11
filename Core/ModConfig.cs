using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class ModConfig
    {
        /// <summary>
        /// True if the first build cost item should be overridden
        /// </summary>
        public bool OverrideFirstItem { get; set; } = false;
        /// <summary>
        /// Override item id for the first build cost item
        /// </summary>
        public int? FirstItemId { get; set; } = null;
        /// <summary>
        /// Override count for the first build cost item
        /// </summary>
        public int? FirstItemCount { get; set; } = null;
        /// <summary>
        /// True if the second build cost item should be overridden
        /// </summary>
        public bool OverrideSecondItem { get; set; } = false;
        /// <summary>
        /// Override item id for the second build cost item
        /// </summary>
        public int? SecondItemId { get; set; } = null;
        /// <summary>
        /// Override count for the second build cost item
        /// </summary>
        public int? SecondItemCount { get; set; } = null;
        /// <summary>
        /// True if the third build cost item should be overridden
        /// </summary>
        public bool OverrideThirdItem { get; set; } = false;
        /// <summary>
        /// Override item id for the third build cost item
        /// </summary>
        public int? ThirdItemId { get; set; } = null;
        /// <summary>
        /// Override count for the third build cost item
        /// </summary>
        public int? ThirdItemCount { get; set; } = null;
        /// <summary>
        /// The gold cost of the shed upgrade
        /// </summary>
        public int GoldCost { get; set; } = 75000;
        /// <summary>
        /// What hints should be given for default dimensions.
        /// </summary>
        public HintConfig DimensionHints { get; set; } = HintConfig.None;
    }
}
