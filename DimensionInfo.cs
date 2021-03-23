using Newtonsoft.Json;
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
        private string displayName;
        public string DisplayName { get => displayName; }
        private string description;
        public string Description { get => description; }
        private string buildingId;
        public string BuildingId { get => buildingId; }
        private string mapNameBase;
        public string MapName { get => mapNameBase; }
        private Dictionary<int, int> stageCounts;
        public List<int> Stages { get => stageCounts.Keys.ToList(); }
        public int StageRequirement(int stage)
        {
            return stageCounts[stage];
        }

        private DimensionInfo() { }
    }
}
