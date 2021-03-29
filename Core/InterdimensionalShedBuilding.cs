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
using FinalDoom.StardewValley.InterdimensionalShed.API;
using Utility = FinalDoom.StardewValley.InterdimensionalShed.API.Utility;
using StardewModdingAPI.Events;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class InterdimensionalShedBuilding : Building
    {
        private const string ModData_SelectedDimensionItemKey = "SelectedDimensionItemId";
        private static readonly Point ItemSlotTileOffset = new Point(1, 0);

        private DimensionInfo selectedDimension = null;
        internal DimensionInfo SelectedDimension
        {
            get => selectedDimension;
            set 
            {
                selectedDimension = value;
                modData[ModData_SelectedDimensionItemKey] = value == null ? "none" : value.DimensionImplementation.Item.ParentSheetIndex.ToString();
            }
        }

        internal InterdimensionalShedBuilding(Building building) : base(new BluePrint(ModEntry.BlueprintId), new Vector2(building.tileX.Value, building.tileY.Value))
        {
            // Keep the correct indoor in the shed, but override the warp to go to one of the other sheds we create
            indoors.Value = building.indoors.Value;
            daysOfConstructionLeft.Value = 0;
            modData = building.modData;
            modData[ModEntry.SaveKey] = "true";
            var item = modData.ContainsKey(ModData_SelectedDimensionItemKey) ? modData[ModData_SelectedDimensionItemKey] : "769";
            var dimension = item.Equals("none") ? null : DimensionData.Data.getDimensionInfo(Convert.ToInt32(item));
            if (dimension != null && !modData.ContainsKey(ModData_SelectedDimensionItemKey))
            {
                Utility.Log("New " + typeof(InterdimensionalShedBuilding).Name + " dimension set to " + dimension.DisplayName + " dimension");
            }
            SelectedDimension = dimension;
        }

        protected override GameLocation getIndoors(string nameOfIndoorsWithoutUnique)
        {
            return null; // Don't build an interior, this is so the constructor can set it from the parent building.
        }

        public override bool doAction(Vector2 tileLocation, Farmer who)
        {
            if (who.IsLocalPlayer && tileLocation.X == (float)(ItemSlotTileOffset.X + humanDoor.X + tileX.Value) && tileLocation.Y == (float)(ItemSlotTileOffset.Y + humanDoor.Y + tileY.Value))
            {
                // Do something with mutexes here
                if (Game1.activeClickableMenu == null || Game1.activeClickableMenu is not ItemSlotMenu)
                {
                    Game1.activeClickableMenu = new ItemSlotMenu(selectedDimension, info => SelectedDimension = info);
                }
                // Consider a flag if the gui is currently open to prevent other players opening/duping/whatever like chests
                return true;
            }
            else if (selectedDimension != null && who.IsLocalPlayer && tileLocation.X == (float)(humanDoor.X + tileX.Value) && tileLocation.Y == (float)(humanDoor.Y + tileY.Value))
            {
                if (selectedDimension.DimensionImplementation.Item.Stack == 0)
                {
                    return false;
                }
                var warpTargetX = tileX.Value + humanDoor.X;
                var warpTargetY = tileY.Value + humanDoor.Y + 1;
                DimensionData.Data.doDimensionWarp(selectedDimension, who, new Point(warpTargetX, warpTargetY));
                // Consider Myst sound here
                who.currentLocation.playSoundAt("doorClose", tileLocation);
                return true;
            }
            // Warp to normal interior handled by base class
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

        public override void performToolAction(Tool t, int tileX, int tileY)
        {
            // Can probably do like, use pickaxe to get item out -- but check if anyone's inside and pop up a message if there is.. or not? might not be necessary if the door warp still leads to the farm
        }

        internal class InterdimensionalShedBuildingSaveHandler : IConvertingSaveHandler
        {
            private readonly List<InterdimensionalShedBuilding> customBuildings = new List<InterdimensionalShedBuilding>();
            private readonly List<Building> vanillaBuildings = new List<Building>();

            /// <summary>
            /// Converts <c>InterdimensionalShedBuilding</c>s to normal <c>Building</c>s so they can be saved.
            /// </summary>
            public void PrepareForSaving()
            {
                var farmBuildings = Game1.getFarm().buildings;
                farmBuildings.OfType<InterdimensionalShedBuilding>().ToList().ForEach(building =>
                {
                    var baseBuilding = new Building(new BluePrint(new BluePrint(building.buildingType.Value).nameOfBuildingToUpgrade), new Vector2(building.tileX.Value, building.tileY.Value));
                    baseBuilding.modData = building.modData;
                    baseBuilding.daysOfConstructionLeft.Value = 0;
                    baseBuilding.indoors.Value = building.indoors.Value;
                    farmBuildings.Remove(building);
                    farmBuildings.Add(baseBuilding);
                    customBuildings.Add(building);
                    vanillaBuildings.Add(baseBuilding);
                });
            }

            /// <summary>
            /// Converts base <c>Building</c>s back into <c>InterdimensionalShedBuilding</c>s after saving.
            /// </summary>
            public void AfterSaved()
            {
                var farmBuildings = Game1.getFarm().buildings;
                vanillaBuildings.ForEach(building => farmBuildings.Remove(building));
                vanillaBuildings.Clear();
                customBuildings.ForEach(building => farmBuildings.Add(building));
                customBuildings.Clear();
            }

            /// <summary>
            /// Upconvert buildings to the appropriate type after load.
            /// </summary>
            public void InitializeAfterLoad()
            {
                // The vanilla saved versions of our custom buildings
                vanillaBuildings.AddRange(Game1.getFarm().buildings.Where(building => building.modData.ContainsKey(ModEntry.SaveKey)));
                // Change the vanilla into custom versions
                customBuildings.AddRange(vanillaBuildings.Select(building => new InterdimensionalShedBuilding(building)));
                AfterSaved();
            }
        }
    }

    internal class InterdimensionalShedBuildingBluePrintProvider : ICustomBluePrintProvider
    {
        public bool IsMagical => true;

        public BluePrint BluePrint => new BluePrint(ModEntry.BlueprintId);

        /// <summary>
        /// Converts a just-upgraded Big Shed into an InterdimensionalShed
        /// </summary>
        public void InterceptBuildAction(object sender, UpdateTickedEventArgs e)
        {
            var currentMenu = Game1.activeClickableMenu;
            if (currentMenu == null || currentMenu is not CarpenterMenu)
            {
                Utility.Helper.Events.GameLoop.UpdateTicked -= InterceptBuildAction;
            }
            else if (currentMenu is CarpenterMenu carpenterMenu && Utility.Helper.Reflection.GetField<bool>(carpenterMenu, "upgrading").GetValue() && Utility.Helper.Reflection.GetField<bool>(carpenterMenu, "freeze").GetValue())
            {
                var farm = Game1.getFarm();
                var toUpgrade = (from building in farm.buildings
                                 where building.daysUntilUpgrade.Value > 0 && building.buildingType.Equals(carpenterMenu.CurrentBlueprint.nameOfBuildingToUpgrade)
                                 select building)
                                 .FirstOrDefault();
                if (toUpgrade != null)
                {
                    // Do the upgrade immediately then swap for our type
                    toUpgrade.buildingType.Value = ModEntry.BlueprintId;
                    toUpgrade.resetTexture();
                    var idsb = new InterdimensionalShedBuilding(toUpgrade);
                    farm.buildings.Remove(toUpgrade);
                    farm.buildings.Add(idsb);

                    idsb.SelectedDimension.DimensionImplementation.InitializeDimensionBuilding();

                    Utility.Helper.Events.GameLoop.UpdateTicked -= InterceptBuildAction;
                }
            }
        }
    }
}
