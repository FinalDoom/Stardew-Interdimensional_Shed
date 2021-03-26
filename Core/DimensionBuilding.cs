using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    public class DimensionBuilding : Building
    {
        private DimensionInfo dimensionInfo;
        public bool IsAccessible { get => dimensionInfo.DimensionImplementation.Item.Stack != int.MaxValue && dimensionInfo.DimensionImplementation.Item.Stack > 0; }

        private DimensionBuilding(Building building) : base(new BluePrint(building.buildingType.Value), new Vector2(building.tileX.Value, building.tileY.Value))
        {
            daysOfConstructionLeft.Value = 0;
            modData = building.modData;
            dimensionInfo = ModEntry.DimensionData.getDimensionInfo(modData[DimensionData.ModData_ShedDimensionKey]);
            indoors.Value = getIndoors(dimensionInfo.MapName);
            Utility.TransferObjects(building.indoors.Value, indoors.Value);
        }

        public DimensionBuilding(DimensionInfo info, BluePrint blueprint, Vector2 location) : base(blueprint, location)
        {
            modData[DimensionData.ModData_ShedDimensionKey] = info.BuildingId;
            Utility.TraceLog($"Built dimension: {info.DisplayName}");
            daysOfConstructionLeft.Value = 0;
            dimensionInfo = info;
            indoors.Value = getIndoors(dimensionInfo.MapName);// Still a problem with dimensions post-convert
        }

        internal bool ContainsDimension(DimensionInfo info)
        {
            return dimensionInfo == info;
        }

        protected override GameLocation getIndoors(string nameOfIndoorsWithoutUnique)
        {
            if (dimensionInfo == null)
            {
                return null;
            }
            var lcl_indoors = new ShedDimension("Maps\\" + nameOfIndoorsWithoutUnique, buildingType);
            lcl_indoors.uniqueName.Value = nameOfIndoorsWithoutUnique + Guid.NewGuid().ToString();
            lcl_indoors.IsFarm = true;
            lcl_indoors.isStructure.Value = true;
            updateInteriorWarps(lcl_indoors);
            return lcl_indoors;
        }

        public override void dayUpdate(int dayOfMonth)
        {
            // Don't do any updates to objects and whatever if the linking items are missing
            if (IsAccessible)
            {
                base.dayUpdate(dayOfMonth);
            }
        }
        internal class DimensionBuildingSaveHandler : IConvertingSaveHandler<Building>
        {
            private List<Building> unloadedDimensionBuildings;

            public IEnumerable<Building> PrepareForSaving()
            {
                var farmBuildings = Game1.getFarm().buildings;
                // Return the unloaded buildings to the pool to be saved
                unloadedDimensionBuildings.ForEach(b => farmBuildings.Add(b));
                // Downconvert our buildings to a savable type
                var savable = farmBuildings.OfType<DimensionBuilding>().ToList().Select(building =>
                {
                    var baseBuilding = new Building(new BluePrint(building.buildingType.Value), new Vector2(building.tileX.Value, building.tileY.Value));
                    baseBuilding.modData = building.modData;
                    baseBuilding.daysOfConstructionLeft.Value = 0;
                    Utility.TraceLog($"Unconverting {building.dimensionInfo.DisplayName} building and interior");
                    Utility.TraceLog($"Unconverted interior has {building.indoors.Value.furniture.Count()} objects");
                    baseBuilding.indoors.Value.TransferDataFromSavedLocation(building.indoors.Value);
                    Utility.TransferObjects(building.indoors.Value, baseBuilding.indoors.Value);
                    Utility.TraceLog($"Base interior has {baseBuilding.indoors.Value.furniture.Count()} objects");
                    farmBuildings.Remove(building);
                    farmBuildings.Add(baseBuilding);
                    return baseBuilding;
                }).ToList();
                return savable;
            }

            IEnumerable<object> ISaveHandler.PrepareForSaving()
            {
                return PrepareForSaving();
            }

            public void AfterSaved(IEnumerable<Building> buildings)
            {
                var farmBuildings = Game1.getFarm().buildings;
                // Remove the unloaded buildings again
                unloadedDimensionBuildings.ForEach(b => farmBuildings.Remove(b));
                // Upconvert the loaded buildings back to our type
                foreach (var building in buildings)
                {
                    var db = new DimensionBuilding(building);
                    farmBuildings.Remove(building);
                    farmBuildings.Add(db);
                }
            }

            public void AfterSaved(IEnumerable<object> obj)
            {
                AfterSaved((IEnumerable<Building>)obj);
            }

            public void InitializeAfterLoad()
            {
                var farmBuildings = Game1.getFarm().buildings;
                // Take any buildings for which mods have been unloaded out of the pool
                unloadedDimensionBuildings = farmBuildings.Where(building => building.modData.ContainsKey(DimensionData.ModData_ShedDimensionKey) && ModEntry.DimensionData.getDimensionInfo(building.modData[DimensionData.ModData_ShedDimensionKey]) == null).ToList();
                if (unloadedDimensionBuildings.Count() > 0)
                {
                    Utility.Log($"Hiding buildings which are missing mod data: {string.Join(", ", unloadedDimensionBuildings.Select(b => b.modData[DimensionData.ModData_ShedDimensionKey]))}");
                    unloadedDimensionBuildings.ForEach(b => farmBuildings.Remove(b));
                }
                AfterSaved(farmBuildings.Where(building => building.modData.ContainsKey(DimensionData.ModData_ShedDimensionKey)).ToList());
            }
        }
    }
}
