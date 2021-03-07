using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdimensionalShed
{
    public class InterdimensionalShedBuilding : Building
    {
        private static readonly Point ItemSlotTileOffset = new Point(1, 0);
        private static Dictionary<int, int> DimensionItemCounts = new Dictionary<int, int>();
        static InterdimensionalShedBuilding()
        {
            var farmModData = Game1.getFarm().modData;
            if (farmModData.ContainsKey(ModEntry.DimensionItemsKey))
            {
                foreach (var kvp in farmModData[ModEntry.DimensionItemsKey].Split(','))
                {
                    var split = kvp.Split('=');
                    DimensionItemCounts[int.Parse(split[0])] = int.Parse(split[1]);
                }
            }
            else
            {
                DimensionItemCounts[769] = 100;
            }
        }

        private bool _itemSlotMenuOpen = false;
        public bool ItemSlotMenuOpen {
            get
            {
                return _itemSlotMenuOpen = Game1.activeClickableMenu != null;
            }
            set
            {
                _itemSlotMenuOpen = value;
            }
        }
        private int? _selectedDimensionItem = null;
        public int? SelectedDimensionItemId
        {
            get
            {
                return _selectedDimensionItem;
            }
            set
            {
                _selectedDimensionItem = value;
            }
        }

        public InterdimensionalShedBuilding(Building building) : base(new BluePrint(building.buildingType.Value), new Vector2(building.tileX.Value, building.tileY.Value))
        {
            indoors.Value = building.indoors.Value;
            daysOfConstructionLeft.Value = 0;
            modData = building.modData;
            modData[ModEntry.SaveKey] = "true";
            if (modData.ContainsKey("SelectedDimensionItem"))
            {
                var item = modData["SelectedDimensionItem"];
                _selectedDimensionItem = item.Equals("none") ? (int?)null : int.Parse(item);
            }
        }

        protected override GameLocation getIndoors(string nameOfIndoorsWithoutUnique)
        {
            return null; // Don't build an interior, this is so the constructor can set it from the parent building.
        }

        public override bool doAction(Vector2 tileLocation, Farmer who)
        {
            if (who.IsLocalPlayer && tileLocation.X == (float)(ItemSlotTileOffset.X + humanDoor.X + tileX.Value) && tileLocation.Y == (float)(ItemSlotTileOffset.Y + humanDoor.Y + tileY.Value))
            {
                //if (!ItemSlotMenuOpen)
                //    Game1.activeClickableMenu = new ItemSlotMenu(null); 
                //new ItemSlotMenu(logMenuAction);
                // Open up the item slot gui
                // Consider a flag if the gui is currently open to prevent other players opening/duping/whatever
                if (_selectedDimensionItem is int)
                {
                    _selectedDimensionItem = null;
                }
                else
                {
                    _selectedDimensionItem = 769;
                }
            }
            else if (_selectedDimensionItem is int dimensionItemId && who.IsLocalPlayer && tileLocation.X == (float)(humanDoor.X + tileX.Value) && tileLocation.Y == (float)(humanDoor.Y + tileY.Value))
            {
                var dimension = ModEntry.ShedDimensionKeys[dimensionItemId];
                var lcl_indoors = Game1.getFarm().buildings.Where(b => b.modData.ContainsKey(ModEntry.ShedDimensionModDataKey)).First(b => b.modData[ModEntry.ShedDimensionModDataKey].Equals(dimension)).indoors.Value;
                var warp = lcl_indoors.warps[0];
                warp.TargetX = tileX.Value + humanDoor.X;
                warp.TargetY = tileY.Value + humanDoor.Y + 1;
                // Consider Myst sound here
                who.currentLocation.playSoundAt("doorClose", tileLocation);
                Game1.warpFarmer(lcl_indoors.uniqueName.Value, warp.X, warp.Y - 1, Game1.player.FacingDirection, isStructure: true);
                return true;
            }
            // Warp to normal interior handled by base class
            return base.doAction(tileLocation, who);
        }
        public void logMenuAction(string s, int i)
        {
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

        public override void performToolAction(Tool t, int tileX, int tileY)
        {
            // Can probably do like, use pickaxe to get item out -- but check if anyone's inside and pop up a message if there is.. or not? might not be necessary if the door warp still leads to the farm
        }


        internal static void StoreStaticData()
        {
            var kvps = DimensionItemCounts.Select(x => String.Format("{0}={1}", x.Key, x.Value));
            Game1.getFarm().modData[ModEntry.DimensionItemsKey] = String.Join(",", kvps);
        }
        internal void StoreObjectData()
        {
        }
    }
}
