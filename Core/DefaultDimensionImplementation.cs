using FinalDoom.StardewValley.InterdimensionalShed.API;
using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility = FinalDoom.StardewValley.InterdimensionalShed.API.Utility;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class DefaultDimensionImplementation : DimensionImplementation, IDimensionImplementation
    {
        public DefaultDimensionImplementation(DimensionInfo info, Item item, int dimensionIndex) : base(info, item, dimensionIndex)
        {
        }

        public override bool HintAllowed()
        {
            return HintAllowed(ModEntry.config.DimensionHints);
        }

        /// <summary>
        /// Finds the gamelocation associated with this DimensionInfo.
        /// </summary>
        /// <remarks>
        /// This should depend on <c>Game1.getFarm().buildings</c> as the save/load process
        /// will operate on that collection and may change the actual <c>Building</c> and
        /// <c>GameLocation</c> objects. A separately managed collection may not continue to
        /// reference the actual ingame locations.
        /// </remarks>
        public override GameLocation getDimensionLocation()
        {
            // TODO is there a more generic way to do this? Probably something with modData right?
            return (from building in Game1.getFarm().buildings
                    where building is DimensionBuilding db && db.ContainsDimension(dimensionInfo)
                    select building.indoors.Value)
                    .SingleOrDefault();
        }

        /// <summary>
        /// Adds dimension containing buildings if there were none. Should be called when first Interdimensional Shed is built.
        /// </summary>
        public override void InitializeDimensionBuilding()
        {
            if (dimensionInfo == null || getDimensionLocation() != null)
            {
                return;
            }

            // TODO Possibly unnecessary. Depends on host event rules
            Game1.addMail(ModEntry.ShedPurchaseMailId, true, true);

            Utility.TraceLog($"Initializing dimension building for {dimensionInfo.DisplayName}");
            var farmhouseWarp = Game1.getFarm().GetMainFarmHouseEntry();

            var b = new DimensionBuilding(dimensionInfo, new BluePrint("Big Shed"), new Vector2(-100, -100));
            foreach (var warp in b.indoors.Value.warps)
            {
                // Give the warp back sensible defaults, since these are off the map
                warp.TargetX = farmhouseWarp.X;
                warp.TargetY = farmhouseWarp.Y;
            }
            Game1.getFarm().buildings.Add(b);

            Utility.TraceLog($"Dimension building initialized for {dimensionInfo.DisplayName}");
        }
    }
}
