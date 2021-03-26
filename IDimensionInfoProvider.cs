using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    public interface IDimensionInfoProvider
    {
        public string DimensionCollectionName { get; }
        public IEnumerable<DimensionInfo> Dimensions { get; }
    }
}
