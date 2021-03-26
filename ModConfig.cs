using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class ModConfig
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
        public Hint DimensionHints { get; set; } = Hint.None;





        public KeybindList ToggleShaftsKey { get; set; } = KeybindList.Parse("OemTilde");
        public bool ForceShafts { get; set; } = false;
        public Color HighlightRectangleRGBA { get; set; } = Color.Lime;
        public string HighlightImageFilename { get; set; } = "cracked.png";


        public enum Hint
        {
            None,
            Random,
            Daily,
            All
        }
    }
}
