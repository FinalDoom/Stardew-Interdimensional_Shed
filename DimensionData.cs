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

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class DimensionData
    {
        private const int ItemId_VoidEssence = 769;
        private const int voidEssenceCount = 100; // should this be init from config, but then how to resolve config changes?
        private const string ModData_DimensionItemsKey = "InterdimensionalShedItems";
        internal const string ModData_ShedDimensionKey = "InterdimensionalShedLinkedDimensionName";

        private readonly List<DimensionInfo> dimensionInfo = new List<DimensionInfo>();

        private readonly List<Item> dimensionItems = new List<Item>();

        /// <summary>
        /// Items associated with dimensions that have been discovered/unlocked.
        /// </summary>
        public List<Item> UnlockedDimensions { get => dimensionItems.Where(item => item.Stack != int.MaxValue).ToList(); }

        /// <summary>
        /// Items associated with undiscovered dimensions that can be hinted according to configuration.
        /// </summary>
        public List<Item> HintedDimensions { get => dimensionInfo.Where(di => di.dimensionImplementation.HintAllowed()).Select(di => di.dimensionImplementation.Item).ToList(); }

        /// <summary>
        /// Total count of all dimensions configured.
        /// </summary>
        public int DimensionCount { get => dimensionInfo.Count(); }

        /// <summary>
        /// Returns <c>true</c> if the multiple dimensions provided by this mod have been initialized on this save.
        /// </summary>
        public static bool FarmLinkedToMultipleDimensions
        {
            get
            {
                return Game1.getFarm().buildings.Any(b => b.modData.ContainsKey(ModData_ShedDimensionKey));
            }
        }

        private Point warpReturn;

        /********
         * Property Utility Functions
         ********/

        // TODO come up with a solution for handling un-loaded dimensions
        // Eg. freeze time in them or whatever will prevent updates, if possible.. move them out of the farm to local store (restore on save), etc. Not sure yet.
        // Make sure they can't be entered obviously

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
        public DimensionInfo getDimensionInfo(int itemId)
        {
            return dimensionInfo.Find(di => di.ItemId == itemId);
        }
        /// <summary>
        /// Gets the info for the dimension associated with the passed building ID.
        /// </summary>
        public DimensionInfo getDimensionInfo(string buildingId)
        {
            return dimensionInfo.Find(di => di.BuildingId == buildingId);
        }

        /// <summary>
        /// Gets the item instance for the passed dimension info.
        /// </summary>
        public Item getDimensionItem(DimensionInfo info) => getDimensionItem(info.ItemId);
        /// <summary>
        /// Gets the item instance for the passed item id.
        /// </summary>
        public Item getDimensionItem(int id)
        {
            return dimensionItems.Where(item => item.ParentSheetIndex == id).First();
        }

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

        internal void doDimensionWarp(Item selectedDimensionItem, Farmer who, Point warpTarget)
        {
            var info = getDimensionInfo(selectedDimensionItem);
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
        internal void InitializeDimensionBuildings()
        {
            if (FarmLinkedToMultipleDimensions)
            {
                return;
            }
            Utility.TraceLog("Initializing configured dimension buildings");
            var farmhouseWarp = Game1.getFarm().GetMainFarmHouseEntry();
            var buildings = Game1.getFarm().buildings;
            dimensionInfo.ForEach(info =>
            {
                var b = new DimensionBuilding(info, new BluePrint("Big Shed"), new Vector2(-100, -100));
                foreach (var warp in b.indoors.Value.warps)
                {
                    // Give the warp back sensible defaults, since these are off the map
                    warp.TargetX = farmhouseWarp.X;
                    warp.TargetY = farmhouseWarp.Y;
                }
                buildings.Add(b);
            });
            Utility.TraceLog($"Dimension buildings initialized {FarmLinkedToMultipleDimensions}");
        }

        /*********
         ** Save data Functions
         *********/

        [Priority(100)] // Make sure this loads before other things, since it's the base data structure
        internal class DimensionDataSaveHandler :  ISaveHandler
        {
            private readonly DimensionData dd = ModEntry.DimensionData;

            /// <summary>
            /// Initializes object reference lists on save load.
            /// </summary>
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
                // TODO resolve new/old dimension info

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
                dd.dimensionItems.Clear();
                dd.dimensionInfo.ForEach(info =>
                {
                    var unlocked = itemKVPs.ContainsKey(info.ItemId);
                    var item = SDVUtility.getItemFromStandardTextDescription("O " + info.ItemId + " 0", null);
                    item.Stack = unlocked ? itemKVPs[info.ItemId] : item.ParentSheetIndex == ItemId_VoidEssence ? voidEssenceCount : int.MaxValue;
                    if (!info.IgnoreQuality && item is Object i)
                    {
                        i.Quality = info.Quality;
                    }
                    //Utility.TraceLog($"Initializing dimension item: id {item.ParentSheetIndex} named {item.DisplayName} category {item.getCategoryName()} number {item.Category}");
                    info.dimensionImplementation = (IDimensionImplementation)Activator.CreateInstance(info.DimensionImplementationClass, info, item, dd.dimensionItems.Count());
                    dd.dimensionItems.Add(item);
                });
            }

            /// <summary>
            /// Serializes this object's data for storage by the standard save routines
            /// </summary>
            public IEnumerable<object> PrepareForSaving()
            {
                // Just don't save anything if we've not even unlocked anything
                if (FarmLinkedToMultipleDimensions)
                {   
                    var kvps = dd.UnlockedDimensions.Select(item => string.Format("{0}={1}", item.ParentSheetIndex, item.Stack));
                    Game1.getFarm().modData[ModData_DimensionItemsKey] = string.Join(",", kvps);
                }
                return null;
            }
        }
    }
}
