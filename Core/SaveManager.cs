using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    /// <summary>
    /// Self contained manager that calls <c>ISaveHandler</c>s pre and post save/load. 
    /// </summary>
    internal class SaveManager
    {
        private readonly List<ISaveHandler> saveableObjectsHandlers = (
            from type in Utility.GetAllTypes()
            where !type.IsInterface && !type.IsAbstract && type.GetInterfaces().Any(i => (!i.IsGenericType && i.IsAssignableFrom(typeof(ISaveHandler))) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConvertingSaveHandler<>)))
            let priority = type.GetCustomAttributes(typeof(PriorityAttribute), false).Select(attr => ((PriorityAttribute)attr).Priority).SingleOrDefault()
            orderby priority descending
            select (ISaveHandler)Activator.CreateInstance(type)
            ).ToList();
        private Dictionary<ISaveHandler, IEnumerable<object>> reconversions;

        internal SaveManager()
        {
            Utility.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded_InitializeObjects;
            Utility.Helper.Events.GameLoop.Saving += GameLoop_Saving_PrepareObjectsForSaving;
            Utility.Helper.Events.GameLoop.Saved += GameLoop_Saved_RecoverObjectsAfterSaving;
        }

        private void OnSaveLoaded_InitializeObjects(object sender, SaveLoadedEventArgs e)
        {
            Utility.TraceLog("Loading");
            foreach (var handler in saveableObjectsHandlers)
            {
                Utility.TraceLog($"Handler of type {handler.GetType().Name}");
                handler.InitializeAfterLoad();
            }
            Utility.TraceLog("Loaded");
        }

        private void GameLoop_Saving_PrepareObjectsForSaving(object sender, SavingEventArgs e)
        {
            //var saveableTypes = from type in Utility.GetAllTypes()
            //                    where typeof(ISaveableType).IsAssignableFrom(type)
            //                    select type;
            // Save static types
            //saveableTypes.ToList().ForEach(type => type.GetMethod(nameof(ISaveableType.StoreStaticData)).Invoke(null, null));

            Utility.TraceLog("Preparing objects for saving");
            reconversions = saveableObjectsHandlers.ToDictionary(obj => obj, obj => obj.PrepareForSaving());
            Utility.TraceLog("Ready to save");
        }

        private void GameLoop_Saved_RecoverObjectsAfterSaving(object sender, SavedEventArgs e)
        {
            Utility.TraceLog("Saved: Recovering custom objects");
            foreach (var pair in reconversions)
            {
                var handler = pair.Key;
                var objects = pair.Value;
                if (objects != null && handler is IConvertingSaveHandler converter)
                {
                    converter.AfterSaved(objects);
                }
            }
            reconversions = null;
            Utility.TraceLog("Custom objects recovered");
        }
    }
}
