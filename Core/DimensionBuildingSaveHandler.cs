using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System.Collections.Generic;
using System.Linq;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    public class DimensionBuildingSaveHandler : IConvertingSaveHandler
    {
        private readonly DimensionData data = Singleton<DimensionData>.Instance;

        private readonly List<DimensionBuilding> customBuildings = new List<DimensionBuilding>();
        private readonly List<Building> vanillaBuildings = new List<Building>();
        private readonly List<Building> unloadedDimensionBuildings = new List<Building>();

        public void PrepareForSaving()
        {
            var farmBuildings = Game1.getFarm().buildings;
            // Return the unloaded buildings to the pool to be saved
            unloadedDimensionBuildings.ForEach(b => farmBuildings.Add(b));
            // Downconvert our buildings to a savable type
            farmBuildings.OfType<DimensionBuilding>().ToList().ForEach(building =>
            {
                var baseBuilding = new Building(new BluePrint(building.buildingType.Value), new Vector2(building.tileX.Value, building.tileY.Value));
                baseBuilding.modData = building.modData;
                baseBuilding.daysOfConstructionLeft.Value = 0;
                Utility.TraceLog($"Unconverting {building.DisplayName} building and interior");
                Utility.TraceLog($"Unconverted interior has {building.indoors.Value.furniture.Count()} objects");
                baseBuilding.indoors.Value.TransferDataFromSavedLocation(building.indoors.Value);
                Utility.TransferObjects(building.indoors.Value, baseBuilding.indoors.Value);
                Utility.TraceLog($"Base interior has {baseBuilding.indoors.Value.furniture.Count()} objects");
                farmBuildings.Remove(building);
                farmBuildings.Add(baseBuilding);
                customBuildings.Add(building);
                vanillaBuildings.Add(baseBuilding);
            });
        }

        public void AfterSaved()
        {
            var farmBuildings = Game1.getFarm().buildings;
            // Remove the unloaded buildings again
            unloadedDimensionBuildings.ForEach(b => farmBuildings.Remove(b));
            // Add our custom types back into the pool
            vanillaBuildings.ForEach(building => farmBuildings.Remove(building));
            vanillaBuildings.Clear();
            customBuildings.ForEach(building => farmBuildings.Add(building));
            customBuildings.Clear();
        }

        public void InitializeAfterLoad()
        {
            var farmBuildings = Game1.getFarm().buildings;
            // Take any buildings for which mods have been unloaded out of the pool
            unloadedDimensionBuildings.AddRange(farmBuildings.Where(building => building.modData.ContainsKey(DimensionBuilding.ModData_ShedDimensionKey) && data.getDimensionInfo(building.modData[DimensionBuilding.ModData_ShedDimensionKey]) == null));
            if (unloadedDimensionBuildings.Count() > 0)
            {
                Utility.Log($"Hiding buildings which are missing mod data: {string.Join(", ", unloadedDimensionBuildings.Select(b => b.modData[DimensionBuilding.ModData_ShedDimensionKey]))}");
            }
            // The vanilla saved versions of our custom buildings
            vanillaBuildings.AddRange(Game1.getFarm().buildings.Where(building => building.modData.ContainsKey(DimensionBuilding.ModData_ShedDimensionKey)));
            // Change the vanilla into custom versions
            customBuildings.AddRange(vanillaBuildings.Select(building =>
            {
                var db = new DimensionBuilding(
                    data.getDimensionInfo(building.modData[DimensionBuilding.ModData_ShedDimensionKey]),
                    new BluePrint(building.buildingType.Value),
                    new Vector2(building.tileX.Value, building.tileY.Value));
                db.modData = building.modData;
                Utility.TransferObjects(building.indoors.Value, db.indoors.Value);
                return db;
            }));
            AfterSaved();
        }
    }
}
