using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class CarpenterMenuCustomizer
    {
        private readonly List<ICustomBluePrintProvider> bluePrintProviders = (
            from type in Utility.GetAllTypes()
            where !type.IsInterface && !type.IsAbstract && type.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ICustomBluePrintProvider)))
            select (ICustomBluePrintProvider)Activator.CreateInstance(type)
            ).ToList();
        private readonly IModHelper helper = Utility.Helper;

        public CarpenterMenuCustomizer()
        {
            helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        /// <summary>
        /// Adds a custom <see cref="BluePrint"/> to the Wizard's or Robin's <see cref="CarpenterMenu"/>.
        /// </summary>
        /// <remarks>
        /// We just operate with a single <see cref="InterdimensionalShedBuilding"/> and its <see cref="BluePrint"/>,
        /// provided by an ICustomBluePrintProvider but this should be generalizable for any other buildings for custom tilesets or behavior.
        /// </remarks>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is CarpenterMenu menu) 
            {
                var isMagical = helper.Reflection.GetField<bool>(menu, "magicalConstruction").GetValue();
                foreach (var provider in bluePrintProviders.Where(p => p.IsMagical == isMagical))
                {
                    Utility.TraceLog($"Adding blueprint to {(isMagical ? "Wizard Book" : "Robin's")} CarpenterMenu");
                    helper.Reflection.GetField<List<BluePrint>>(menu, "blueprints").GetValue().Add(provider.BluePrint);
                    helper.Events.GameLoop.UpdateTicked += provider.InterceptBuildAction;
                }
            }

        }
    }
}
