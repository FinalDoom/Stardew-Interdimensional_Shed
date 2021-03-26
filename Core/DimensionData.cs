using StardewModdingAPI.Events;
using StardewValley;
using SDVUtility = StardewValley.Utility;
using Object = StardewValley.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Buildings;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class DimensionData
    {
        private const int ItemId_VoidEssence = 769;
        private const int voidEssenceCount = 100; // should this be init from config, but then how to resolve config changes?
        private const string ModData_DimensionItemsKey = "InterdimensionalShedItems";
        internal const string ModData_ShedDimensionKey = "InterdimensionalShedLinkedDimensionName";

        private readonly List<DimensionInfo> dimensionInfo = new List<DimensionInfo>();

        /// <summary>
        /// Items associated with dimensions that have not been discovered.
        /// </summary>
        public List<DimensionInfo> UndiscoveredDimensions { get => dimensionInfo.Where(info => info.DimensionImplementation.Item.Stack == int.MaxValue).ToList(); }

        /// <summary>
        /// Items associated with dimensions that have been discovered.
        /// </summary>
        public List<DimensionInfo> DiscoveredDimensions { get => dimensionInfo.Where(info => info.DimensionImplementation.Item.Stack != int.MaxValue).ToList(); }

        /// <summary>
        /// Items associated with undiscovered dimensions that can be hinted according to configuration.
        /// </summary>
        public List<DimensionInfo> HintedDimensions { get => dimensionInfo.Where(info => info.DimensionImplementation.HintAllowed()).ToList(); }

        /// <summary>
        /// Total count of all dimensions configured.
        /// </summary>
        public int DimensionCount { get => dimensionInfo.Count(); }

        private Point warpReturn;

        /********
         * Property Utility Functions
         ********/

        /// <summary>
        /// Returns true if dimension info for the specified building name has been loaded.
        /// </summary>
        public bool isDimensionInfoLoaded(string buildingId) => dimensionInfo.Any(di => di.BuildingId == buildingId);
        /// <summary>
        /// Gets the info for the dimension associated with the passed item.
        /// </summary>
        public DimensionInfo getDimensionInfo(Item item) => getDimensionInfo(item.ParentSheetIndex);
        /// <summary>
        /// Gets info for the dimension associated with the passed item ID.
        /// </summary>
        public DimensionInfo getDimensionInfo(int itemId) => dimensionInfo.Find(di => di.ItemId == itemId);
        /// <summary>
        /// Gets the info for the dimension associated with the passed building ID.
        /// </summary>
        public DimensionInfo getDimensionInfo(string buildingId) => dimensionInfo.Find(di => di.BuildingId == buildingId);

        /********
         * Actual Functions
         ********/

        private GameLocation getDimensionLocation(DimensionInfo info)
        {
            return (from building in Game1.getFarm().buildings
                    where building is DimensionBuilding db && db.ContainsDimension(info)
                    select building.indoors.Value)
                    .SingleOrDefault();
        }

        internal void doDimensionWarp(DimensionInfo info, Farmer who, Point warpTarget)
        {
            Utility.Log("Doing dimension warp to " + info.DisplayName);
            var lcl_indoors = getDimensionLocation(info);
            Utility.TraceLog("Got indoors " + (lcl_indoors == null ? "null" : lcl_indoors.NameOrUniqueName));
            var warp = lcl_indoors.warps[0];
            warp.TargetX = warpTarget.X;
            warp.TargetY = warpTarget.Y;
            // Store the warped from info per farmer
            warpReturn = warpTarget;
            Utility.TraceLog($"Warping farmer to {lcl_indoors.uniqueName.Value} {warp.X} {warp.Y - 1}");
            Game1.warpFarmer(lcl_indoors.uniqueName.Value, warp.X, warp.Y - 1, who.FacingDirection, isStructure: true);
            Utility.Helper.Events.Player.Warped += Player_Warped_FixWarpTarget;
        }

        private void Player_Warped_FixWarpTarget(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer 
                // TODO Fix this location check && e.OldLocation is Shed 
                && e.NewLocation is Farm)
            {
                // consider Myst sound here
                e.Player.setTileLocation(new Vector2(warpReturn.X, warpReturn.Y));
                Utility.Helper.Events.Player.Warped -= Player_Warped_FixWarpTarget;

            }
        }

        /// <summary>
        /// Adds dimension containing buildings if there were none. Should be called when first Interdimensional Shed is built.
        /// </summary>
        internal void InitializeDimensionBuilding(DimensionInfo info)
        {
            if (info == null || getDimensionLocation(info) != null)
            {
                return;
            }

            // Possibly unnecessary. Depends on host event rules
            Game1.addMail(ModEntry.ShedPurchaseMailId, true, true);

            Utility.TraceLog($"Initializing dimension building for {info.DisplayName}");
            var farmhouseWarp = Game1.getFarm().GetMainFarmHouseEntry();
            var buildings = Game1.getFarm().buildings;

            var b = new DimensionBuilding(info, new BluePrint("Big Shed"), new Vector2(-100, -100));
            foreach (var warp in b.indoors.Value.warps)
            {
                // Give the warp back sensible defaults, since these are off the map
                warp.TargetX = farmhouseWarp.X;
                warp.TargetY = farmhouseWarp.Y;
            }
            buildings.Add(b);

            Utility.TraceLog($"Dimension building initialized for {info.DisplayName}");
        }

        /*********
         ** Save data Functions
         *********/

        [Priority(100)] // Make sure this loads before other things, since it's the base data structure
        internal class DimensionDataSaveHandler :  ISaveHandler
        {
            private readonly DimensionData dd = ModEntry.DimensionData;
            private Dictionary<int, int> unloadedItemCounts;

            /// <summary>
            /// Initializes object reference lists on save load.
            /// </summary>
            /// <remarks>
            /// If a particular dimension has been saved and its mod has been removed, this will
            /// preseve the items used in making that dimension, but will remove the dimension's data from
            /// being considered by the game. (That is, no time will pass, etc.)
            /// </remarks>
            public void InitializeAfterLoad()
            {
                // Initialize all DimensionInfo
                dd.dimensionInfo.Clear();
                Utility.Log($"Loading dimensions: Interdimensional Shed (Default)");
                dd.dimensionInfo.AddRange(Utility.Helper.Content.Load<List<DimensionInfo>>("assets/Dimensions.json", ContentSource.ModFolder));
                var dimensionInfoProviders =
                    from type in Utility.GetAllTypes()
                    where !type.IsInterface && !type.IsAbstract && type.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IDimensionInfoProvider)))
                    select (IDimensionInfoProvider)Activator.CreateInstance(type);
                foreach (var dip in dimensionInfoProviders)
                {
                    Utility.Log($"Loading dimensions: {dip.DimensionCollectionName}");
                    dd.dimensionInfo.AddRange(dip.Dimensions);
                }

                // Initialize base data maps and such
                var farmModData = Game1.getFarm().modData;
                var itemKVPs = new Dictionary<int, int>();
                if (farmModData.ContainsKey(ModData_DimensionItemsKey))
                {
                    foreach (var kvp in farmModData[ModData_DimensionItemsKey].Split(','))
                    {
                        var split = kvp.Split('=');
                        if (!itemKVPs.ContainsKey(Convert.ToInt32(split[0])))
                            itemKVPs.Add(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]));
                    }
                }
                dd.dimensionInfo.ForEach(info =>
                {
                    var unlocked = itemKVPs.ContainsKey(info.ItemId);
                    var item = SDVUtility.getItemFromStandardTextDescription("O " + info.ItemId + " 0", null);
                    item.Stack = unlocked ? itemKVPs[info.ItemId] : item.ParentSheetIndex == ItemId_VoidEssence ? voidEssenceCount : int.MaxValue;
                    itemKVPs.Remove(info.ItemId);
                    if (!info.IgnoreQuality && item is Object i)
                    {
                        i.Quality = info.Quality;
                    }
                    //Utility.TraceLog($"Initializing dimension: {info.DisplayName} type: {info.DimensionImplementationClass}");
                    info.DimensionImplementation = (IDimensionImplementation)Activator.CreateInstance(info.DimensionImplementationClass, ModEntry.config, info, item, dd.dimensionInfo.IndexOf(info));
                });
                // Store any remainder items for mods that have been unloaded
                unloadedItemCounts = itemKVPs;
                Utility.TraceLog($"Had {unloadedItemCounts.Count()} unloaded items");
                // Fill in the last DimensionImplementationInfo
                var totalDimensionCount = dd.dimensionInfo.Count();
                var discoveredDimensionsCount = dd.DiscoveredDimensions.Count();
                dd.dimensionInfo.Select(di => di.DimensionImplementation).ToList().ForEach(impl =>
                {
                    impl.TotalDimensionCount = totalDimensionCount;
                    impl.DiscoveredDimensionCount = discoveredDimensionsCount;
                });
            }

            /// <summary>
            /// Serializes this object's data for storage by the standard save routines.
            /// </summary>
            /// <remarks>
            /// This also saves the items stored from dimensions that do not have a matching mod loaded.
            /// </remarks>
            public IEnumerable<object> PrepareForSaving()
            {
                var kvps = dd.DiscoveredDimensions.Select(info => string.Format("{0}={1}", info.DimensionImplementation.Item.ParentSheetIndex, info.DimensionImplementation.Item.Stack));
                Utility.Log($"{kvps.Count()} raw kvps");
                // Save our stored unloaded dimension items also
                kvps = kvps.Concat(unloadedItemCounts.Select(pair => string.Format("{0}={1}", pair.Key, pair.Value)));
                Utility.Log($"{kvps.Count()} total kvps");
                Game1.getFarm().modData[ModData_DimensionItemsKey] = string.Join(",", kvps);
                return null;
            }
        }
    }
}
