using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System.Collections.Generic;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class ShedDimension : Shed
    {
        public ShedDimension(string m, string name) : base(m, name)
        {
		}

        public override List<Rectangle> getWalls()
        {
            return base.getWalls();
        }

        public override List<Rectangle> getFloors()
        {
            return base.getFloors();
        }

        public override bool canFishHere()
        {
            Utility.Log("fish check");
            return base.canFishHere();
        }

        // Hacky way to direct warp per-character because
        // GameLocation.isCollidingWithWarp is not virtual,
        // called in Farmer.movePosition

        private void Player_Warped(object sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            if (e.IsLocalPlayer && e.OldLocation == this)
            {
                if (e.NewLocation == Game1.getFarm())
                {
                    Utility.Log("Warp thing in ShedDimension");
                    //ModEntry.DimensionData.
                }
            }
        }
    }
}