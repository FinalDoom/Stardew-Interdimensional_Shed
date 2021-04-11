using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    /// <summary>
    /// Self contained manager that calls <see cref="ISaveHandler"/>s pre and post save/load and <see cref="ILaunchHandler"/>s on game launch.
    /// </summary>
    public class SaveManager
    {
        private readonly List<ILaunchHandler> launchHandlers;
        private readonly List<ISaveHandler> saveableObjectsHandlers;

        public SaveManager()
        {
            launchHandlers = (
               from type in Utility.GetAllTypes()
               where !type.IsInterface && !type.IsAbstract && type.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ILaunchHandler)))
               let priority = type.GetCustomAttributes(typeof(PriorityAttribute), false).Select(attr => ((PriorityAttribute)attr).Priority).SingleOrDefault()
               orderby priority descending
               select (ILaunchHandler)typeof(Singleton<>).MakeGenericType(type).GetProperty("Instance").GetValue(null, null)
               ).ToList();
            saveableObjectsHandlers = (
                from type in Utility.GetAllTypes()
                where !type.IsInterface && !type.IsAbstract && type.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ISaveHandler)))
                let priority = type.GetCustomAttributes(typeof(PriorityAttribute), false).Select(attr => ((PriorityAttribute)attr).Priority).SingleOrDefault()
                orderby priority descending
                select (ISaveHandler)typeof(Singleton<>).MakeGenericType(type).GetProperty("Instance").GetValue(null, null)
                ).ToList();

            Utility.Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched_InitializeObjects;
            Utility.Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded_InitializeObjectsOnLoad;
            Utility.Helper.Events.GameLoop.Saving += GameLoop_Saving_PrepareObjectsForSaving;
            Utility.Helper.Events.GameLoop.Saved += GameLoop_Saved_RecoverObjectsAfterSaving;
        }

        private void GameLoop_GameLaunched_InitializeObjects(object sender, GameLaunchedEventArgs e)
        {
            Utility.TraceLog("Running Launch Handlers");
            foreach (var handler in launchHandlers)
            {
                Utility.TraceLog($"Handler of type {handler.GetType().Name}");
                handler.InitializeAfterLaunch();
            }
            Utility.TraceLog("Ran Launch Handlers");
        }

        private void GameLoop_SaveLoaded_InitializeObjectsOnLoad(object sender, SaveLoadedEventArgs e)
        {
            Utility.TraceLog("Save Loading");
            foreach (var handler in saveableObjectsHandlers)
            {
                Utility.TraceLog($"Handler of type {handler.GetType().Name}");
                handler.InitializeAfterLoad();
            }
            Utility.TraceLog("Save Loaded");
        }

        private void GameLoop_Saving_PrepareObjectsForSaving(object sender, SavingEventArgs e)
        {
            Utility.TraceLog("Saving: Preparing objects");
            saveableObjectsHandlers.ForEach(obj => obj.PrepareForSaving());
            Utility.TraceLog("Ready to save");
        }

        private void GameLoop_Saved_RecoverObjectsAfterSaving(object sender, SavedEventArgs e)
        {
            Utility.TraceLog("Saved: Recovering custom objects");
            saveableObjectsHandlers.OfType<IConvertingSaveHandler>().ToList().ForEach(converter => converter.AfterSaved());
            Utility.TraceLog("Custom objects recovered");
        }
    }
}
