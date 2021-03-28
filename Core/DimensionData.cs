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
    [Priority(100)] // Make sure this loads before other things, since it's the base data structure
    internal class DimensionData : ILaunchHandler, ISaveHandler
    {
        private const int ItemId_VoidEssence = 769;
        private const int voidEssenceCount = 100; // should this be init from config, but then how to resolve config changes?
        private const string ModData_DimensionItemsKey = "InterdimensionalShedItems";
        internal const string ModData_ShedDimensionKey = "InterdimensionalShedLinkedDimensionName";

        private static DimensionData instance;
        public static DimensionData Data
        {
            get => instance;
            set
            {
                if (instance != null)
                {
                    throw new InvalidOperationException("DimensionData should only be instantiated once.");
                }
                instance = value;
            }
        }

        public DimensionData()
        {
            Data = this;
        }

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
        public List<DimensionInfo> HintedDimensions { get => dimensionInfo.Where(info => info.DimensionImplementation.Item.Stack == int.MaxValue).Where(info => info.DimensionImplementation.HintAllowed()).ToList(); }

        /// <summary>
        /// Total count of all dimensions configured.
        /// </summary>
        private int DimensionCount { get => dimensionInfo.Count(); }

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
         * Warp-related Functions
         ********/

        private Point warpReturn;

        /// <summary>
        /// Warps the passed farmer into a dimension and records where they came from, 
        /// so they can be warped back, regardless of the accessibility of the dimension at that time.
        /// </summary>
        /// <param name="info">The info for the dimension to be warped to</param>
        /// <param name="who">The Farmer to work</param>
        /// <param name="warpTarget">The point that the farmer is warping from (later to)</param>
        internal void doDimensionWarp(DimensionInfo info, Farmer who, Point warpTarget)
        {
            Utility.Log("Doing dimension warp to " + info.DisplayName);
            var lcl_indoors = info.DimensionImplementation.getDimensionLocation();
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

        /// <summary>
        /// Catches the farmer warp back from the dimension and puts them in the correct location that
        /// they were warped from.
        /// </summary>
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

        /*********
         ** Save data Functions for ILaunchHandler and ISaveHandler
         *********/

        private Dictionary<int, int> unloadedItemCounts;

        /// <summary>
        /// Intializes base mod data and reference lists on game launch.
        /// </summary>
        public void InitializeAfterLaunch()
        {
            // Initialize all DimensionInfo
            Utility.TraceLog($"Loading dimensions: Interdimensional Shed (Default)");
            Data.dimensionInfo.AddRange(Utility.Helper.Content.Load<List<DimensionInfo>>("assets/Dimensions.json", ContentSource.ModFolder));
            var dimensionInfoProviders =
                from type in Utility.GetAllTypes()
                where !type.IsInterface && !type.IsAbstract && type.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IDimensionInfoProvider)))
                select (IDimensionInfoProvider)Activator.CreateInstance(type);
            foreach (var dip in dimensionInfoProviders)
            {
                Utility.Log($"Loading dimensions: {dip.DimensionCollectionName}");
                Data.dimensionInfo.AddRange(dip.Dimensions);
            }
        }

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
            // Initialize save-based data maps and such
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
            DimensionImplementation.resetDimensionCounts();
            Data.dimensionInfo.ForEach(info =>
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
                info.DimensionImplementation = (IDimensionImplementation)Activator.CreateInstance(info.DimensionImplementationClass, info, item, Data.dimensionInfo.IndexOf(info));
            });
            // Store any remainder items for mods that have been unloaded
            unloadedItemCounts = itemKVPs;
            if (itemKVPs.Count() > 0)
            {
                Utility.TraceLog($"Had {unloadedItemCounts.Count()} unloaded items");
            }
        }

        /// <summary>
        /// Serializes this object's data for storage by the standard save routines.
        /// </summary>
        /// <remarks>
        /// This also saves the items stored from dimensions that do not have a matching mod loaded.
        /// </remarks>
        public void PrepareForSaving()
        {
            var kvps = Data.DiscoveredDimensions.Select(info => string.Format("{0}={1}", info.DimensionImplementation.Item.ParentSheetIndex, info.DimensionImplementation.Item.Stack));
            // Save our stored unloaded dimension items also
            kvps = kvps.Concat(unloadedItemCounts.Select(pair => string.Format("{0}={1}", pair.Key, pair.Value)));
            Game1.getFarm().modData[ModData_DimensionItemsKey] = string.Join(",", kvps);
        }
    }
}