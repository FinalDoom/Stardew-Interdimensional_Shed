using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    public interface IDimensionInfoProvider
    {
        /// <summary>
        /// Descriptive name for the dimension package.
        /// </summary>
        /// <remarks>This is just for logging right now.</remarks>
        public string DimensionCollectionName { get; }
        /// <summary>
        /// Returns a set of DimensionInfo that can be consumed by the core InterdimensionalShed code.
        /// </summary>
        /// <example>
        /// This should generally be pretty simple, just a call to SMAPI to load the json you've defined
        /// for your extension dimensions.
        /// 
        /// <c>
        /// return Utility.Helper.Content.Load<List<DimensionInfo>>("assets/Dimensions.json", ContentSource.ModFolder));
        /// </c>
        /// </example>
        public IEnumerable<DimensionInfo> Dimensions { get; }
    }
}
