using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdimensionalShed
{
    class InterdimensionalShedBuilding : Building
    {
        private static readonly Point ItemSlotTileOffset = new Point(1, 0);
        //public 

        public void PrepareModDataDictionaryObjects()
        {
            modData.Add("x", "y");
        }

        public override List<Item> GetAdditionalItemsToCheckBeforeDemolish()
        {
            // TODO probably also useful with below method
            return base.GetAdditionalItemsToCheckBeforeDemolish();
        }
        public override void BeforeDemolish()
        {
            // TODO add logic for boxing up items in the multiple dimension rooms
            base.BeforeDemolish();
        }

        // todo fix data loaded for CanBePainted()
        public override void dayUpdate(int dayOfMonth)
        {
            // TODO add indoors.DayUpdate(dayOfMonth) for all dimension rooms
            base.dayUpdate(dayOfMonth);
        }

        public override bool doAction(Vector2 tileLocation, Farmer who)
        {
            if (who.IsLocalPlayer && tileLocation.X == (float)(ItemSlotTileOffset.X + humanDoor.X + tileX.Value) && tileLocation.Y == (float)(ItemSlotTileOffset.Y + humanDoor.Y + tileY.Value))
            {
                // Open up the item slot gui
                // Consider a flag if the gui is currently open to prevent other players opening/duping/whatever
            }
            return base.doAction(tileLocation, who);
        }

        // doesTileHaveProperty has something for warps, looks like for the door, but odd
        // draw could be useful for animating smoke
        public override bool isActionableTile(int xTile, int yTile, Farmer who)
        {
            if (xTile == tileX.Value + humanDoor.X + ItemSlotTileOffset.X && yTile == tileY.Value + humanDoor.Y + ItemSlotTileOffset.Y)
            {
                // The item slot tile is actionable
                return true;
            }
            return base.isActionableTile(xTile, yTile, who);
        }

        public override bool CanLeftClick(int x, int y)
        {
            // Figure out if the item slot needs left click or what
            return base.CanLeftClick(x, y);
        }
        public override bool leftClicked()
        {
            return base.leftClicked();
        }

        public override void load()
        {
            // TODO check/consider SaveGame.load wrt this, and this could be useful for swapping out interiors
            base.load();
        }

        public override void performToolAction(Tool t, int tileX, int tileY)
        {
            // Can probably do like, use pickaxe to get item out -- but check if anyone's inside and pop up a message if there is.. or not? might not be necessary if the door warp still leads to the farm
            base.performToolAction(t, tileX, tileY);
        }

    }
}
