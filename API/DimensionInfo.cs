using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.Buildings;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    /// <summary>
    /// Description of the various immutable parts of a dimension, loaded from JSON.
    /// Mutable parts should go in an <see cref="IDimensionImplementation"/> that will
    /// be instantiated once via reflection and added to this object.
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public class DimensionInfo
    {
#pragma warning disable 0649 // Fields are just serialized into
#pragma warning disable IDE0044 // Add readonly modifier (same thing, don't care)
        /// <summary>
        /// The item ID (<see cref="StardewValley.Object"/> only at this time) of the item that links to this dimension
        /// </summary>
        public int ItemId { get => itemId; }
        private int itemId;
        /// <summary>
        /// The quality expected of the item that links to this dimension. Will be -1 if not specified (ignore).
        /// Quality can also be 0 (normal) 1 (silver) 2 (gold) or 4 (iridium).
        /// </summary>
        public int Quality { get => quality; }
        private int quality = -1;
        /// <summary>
        /// Display name of this dimension. Used as a title in the GUI.
        /// </summary>
        public string DisplayName { get => displayName; }
        private string displayName;
        /// <summary>
        /// Hint text to display for the dimension (if hints are allowed for it).
        /// </summary>
        public string Hint { get => hint; }
        private string hint;
        /// <summary>
        /// First stage/initial discovery description to display for the dimension.
        /// </summary>
        public string Description1 { get => description1; }
        private string description1;
        /// <summary>
        /// Secondary description to display for the dimension, normally at stage 2.
        /// </summary>
        public string Description2 { get => description2; }
        private string description2;
        /// <summary>
        /// Tertiary description to display for the dimension, normally at stage 3.
        /// </summary>
        public string Description3 { get => description3; }
        private string description3;
        /// <summary>
        /// Color to use for the text shadow. This is used for the shadow of the hint/description in the GUI.
        /// </summary>
        public Color TextShadowColor {
            get
            {
                var prop = typeof(Color).GetProperty(textShadowColor);
                if (prop != null)
                    return (Color)prop.GetValue(null, null) * textShadowAlpha;
                return Game1.textShadowColor;
            }
        }
        private string textShadowColor;
        private float textShadowAlpha;
        /// <summary>
        /// Type of the <see cref="IDimensionImplementation"/> that should be used for this dimension's general logic.
        /// </summary>
        public Type DimensionImplementationClass { get => Utility.GetType(dimensionImplementationClass); }
        private string dimensionImplementationClass;
        /// <summary>
        /// The type of the <see cref="GameLocation"/> that should be used for this dimension's "indoors" logic.
        /// </summary>
        public Type DimensionIndoorsClass { get => Utility.GetType(dimensionIndoorsClass); }
        private string dimensionIndoorsClass;
        /// <summary>
        /// The unique descriptive name to use in the <c><see cref="Building"/>.modData</c> to determine a vanilla Building is associated with this dimension.
        /// </summary>
        public string BuildingId { get => buildingId; }
        private string buildingId;
        /// <summary>
        /// The name of the map to use for this dimension. It may be appended with the current stage to get different maps for each unlock stage.
        /// </summary>
        public string MapName { get => mapNameBase; }
        private string mapNameBase;
        /// <summary>
        /// List of the unlockable stage levels (generally 1-6 or a subset range from 1).
        /// </summary>
        public List<int> Stages { get => stageCounts.Keys.ToList(); }
        /// <summary>
        /// The number of items required to unlock the passed stage
        /// </summary>
        public int StageRequirement(int stage)
        {
            return stageCounts[stage];
        }
        private Dictionary<int, int> stageCounts;
        /// <summary>
        /// The logic implementation class that will handle the functionality of this dimension. It is set via reflection from the <see cref="DimensionImplementationClass"/>.
        /// </summary>
        public IDimensionImplementation DimensionImplementation { get => dimensionImplementation; set => dimensionImplementation = value; }
        [JsonIgnore]
        private IDimensionImplementation dimensionImplementation;

        /// <summary>
        /// Used by Json serialization
        /// </summary>
        private DimensionInfo() { }
    }
}
