using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class CarpenterMenuCustomizer
    {
        internal CarpenterMenuCustomizer()
        {
            Utility.Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is CarpenterMenu menu && Utility.Helper.Reflection.GetField<bool>(menu, "magicalConstruction").GetValue())
            {
                Utility.TraceLog("Adding blueprint to Wizard Book CarpenterMenu");
                Utility.Helper.Reflection.GetField<List<BluePrint>>(menu, "blueprints").GetValue().Add(new BluePrint(ModEntry.BlueprintId));
                Utility.Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked_InterceptBuildingUpgrade;
            }

        }

        private void GameLoop_UpdateTicked_InterceptBuildingUpgrade(object sender, UpdateTickedEventArgs e)
        {
            var currentMenu = Game1.activeClickableMenu;
            if (currentMenu == null || currentMenu is not CarpenterMenu)
            {
                Utility.Helper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked_InterceptBuildingUpgrade;
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

                    ModEntry.DimensionData.InitializeDimensionBuilding(idsb.SelectedDimension);

                    Utility.Helper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked_InterceptBuildingUpgrade;
                }
            }
        }
    }
}
