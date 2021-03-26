using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinalDoom.StardewValley.InterdimensionalShed;
using StardewModdingAPI;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed.EasterEggDimensions
{
    public class EasterEggDimensionInfoProvider : IDimensionInfoProvider
    {
        public string DimensionCollectionName => "Easter Egg Dimensions";
        public IEnumerable<DimensionInfo> Dimensions => ModEntry.Helper.Content.Load<List<DimensionInfo>>("assets/EasterEggDimensions.json", ContentSource.ModFolder);
    }
}
