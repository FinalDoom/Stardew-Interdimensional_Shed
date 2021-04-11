using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    public class DimensionBuilding : Building
    {
        public const string ModData_ShedDimensionKey = "InterdimensionalShedLinkedDimensionName";
        private readonly DimensionInfo dimensionInfo;
        public string DisplayName { get => dimensionInfo.DisplayName; }

        public DimensionBuilding(DimensionInfo info, BluePrint blueprint, Vector2 location) : base(blueprint, location)
        {
            modData[ModData_ShedDimensionKey] = info.BuildingId;
            Utility.TraceLog($"Built {typeof(DimensionBuilding).Name}: {info.DisplayName}");
            daysOfConstructionLeft.Value = 0;
            dimensionInfo = info;
            indoors.Value = getIndoors(dimensionInfo.MapName);// Still a problem with dimensions post-convert TODO what did this mean? sigh
        }

        public bool ContainsDimension(DimensionInfo info)
        {
            return dimensionInfo == info;
        }

        protected override GameLocation getIndoors(string nameOfIndoorsWithoutUnique)
        {
            if (dimensionInfo == null)
            {
                return null;
            }
            var lcl_indoors = (GameLocation)Activator.CreateInstance(dimensionInfo.DimensionIndoorsClass, this, dimensionInfo);
            lcl_indoors.uniqueName.Value = nameOfIndoorsWithoutUnique + Guid.NewGuid().ToString();
            lcl_indoors.IsFarm = true;
            lcl_indoors.isStructure.Value = true;
            updateInteriorWarps(lcl_indoors);
            return lcl_indoors;
        }
    }
}
