using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdimensionalShed
{
    class ModData
    {
        public Dictionary<Point, InterdimensionalShedBuilding> CustomBuildings { get; set; } = new Dictionary<Point, InterdimensionalShedBuilding>();
    }
}
