using FinalDoom.StardewValley.InterdimensionalShed.API;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System.Collections.Generic;

namespace FinalDoom.StardewValley.InterdimensionalShed.Dimensions
{
    internal class UnimplementedDimension : Shed
    {
        public UnimplementedDimension(DimensionInfo info) : base("Maps/Shed2", info.DisplayName)
        {
        }
    }
}
