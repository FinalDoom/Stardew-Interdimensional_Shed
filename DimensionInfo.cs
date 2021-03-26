using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    [JsonObject(MemberSerialization.Fields)]
    public class DimensionInfo
    {
#pragma warning disable 0649 // Fields are just serialized into
#pragma warning disable IDE0044 // Add readonly modifier (same thing, don't care)
        private int itemId;
        public int ItemId { get => itemId; }
        private bool ignoreQuality;
        public bool IgnoreQuality { get => ignoreQuality; }
        private int quality;
        public int Quality { get => quality; }
        private string displayName;
        public string DisplayName { get => displayName; }
        private string hint;
        public string Hint { get => hint; }
        private string description1;
        public string Description1 { get => description1; }
        private string description2;
        public string Description2 { get => description2; }
        private string description3;
        public string Description3 { get => description3; }
        private string buildingId;
        private string textShadowColor;
        private float textShadowAlpha;
        public Color TextShadowColor {
            get
            {
                var prop = typeof(Color).GetProperty(textShadowColor);
                if (prop != null)
                    return (Color)prop.GetValue(null, null) * textShadowAlpha;
                return Game1.textShadowColor;
            }
        }
        private string dimensionImplementationClass;
        public Type DimensionImplementationClass { get => GetType().Assembly.GetType(dimensionImplementationClass); }
        public string BuildingId { get => buildingId; }
        private string mapNameBase;
        public string MapName { get => mapNameBase; }
        private Dictionary<int, int> stageCounts;
        public List<int> Stages { get => stageCounts.Keys.ToList(); }
        public int StageRequirement(int stage)
        {
            return stageCounts[stage];
        }
        [JsonIgnore]
        internal IDimensionImplementation dimensionImplementation;

        private DimensionInfo() { }
    }
}
