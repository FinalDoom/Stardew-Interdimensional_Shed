using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace FinalDoom.StardewValley.InterdimensionalShed.EasterEggDimensions
{
    internal class ModEntry : Mod
    {
        private static IModHelper helper;
        new public static IModHelper Helper { get => helper; }
        public override void Entry(IModHelper helper)
        {
            ModEntry.helper = helper;
        }
    }
}