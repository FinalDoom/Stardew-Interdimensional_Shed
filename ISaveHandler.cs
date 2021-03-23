using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    /// <summary>
    /// Implementing types will be called by <c>SaveManager</c> to handle saving persistent data.
    /// </summary>
    public interface ISaveHandler
    {
        /// <summary>
        /// Contains logic for preparing a collection of objects for saving.
        /// </summary>
        /// <remarks>This should store persistent information in modData on default Stardew objects and return null.
        /// Unless object is implementing <c>IConvertingSaveHandler</c>, in which case this should call the typed equivalent and return its return.</remarks>
        /// <returns>null or the result of a call to <c>IConvertingSaveHandler.PrepareForSaving()</c></returns>
        IEnumerable<object> PrepareForSaving();
        /// <summary>
        /// Contains logic for preparing an object or collection of objects after loading.
        /// </summary>
        /// <remarks>Generally this will load persistent data from modData and execute any conversions.
        /// For types implementing <c>IConvertingSaveHandler</c>, this should generally involve a call to
        /// <c>IConvertingSaveHandler.AfterSaved(obj)</c>.</remarks>
        void InitializeAfterLoad();
    }
    /// <summary>
    /// Implementing types will be called by <c>SaveManager</c> to handle converting custom types for saving, as well as persisting data
    /// </summary>
    public interface IConvertingSaveHandler : ISaveHandler
    {
        /// <summary>
        /// Contains logic for converting back to custom types after saving is done.
        /// </summary>
        /// <remarks>Generally this should undo any conversions made in the <c>PrepareForSaving</c> method.
        /// This is also a good place for common code involved in loading, which can be called from <c>InitializeAfterLoad()</c>.</remarks>
        /// <param name="obj">Will be passed any objects returned from <c>PrepareForSaving</c></param>
        void AfterSaved(IEnumerable<object> obj);
    }
    /// <summary>
    /// Implementing types will be called by <c>SaveManager</c> to handle converting custom types for saving, as well as persisting data
    /// </summary>
    /// <typeparam name="B">Vanilla Stardew type that will be </typeparam>
    public interface IConvertingSaveHandler<B> : IConvertingSaveHandler
    {
        /// <summary>
        /// Contains logic for preparing a collection of objects for saving.
        /// </summary>
        /// <remarks>This should store persistent information in modData on default Stardew objects and convert custom objects
        /// into their derived vanilla equivalents. The converted vanilla versions should be returned for reconversion in <c>AfterSaved(obj)</c></remarks>
        /// <returns>The vanilla Stardew objects that were converted to. This will be passed to <c>AfterSaved(obj)</c> for reconversion.</returns>
        new IEnumerable<B> PrepareForSaving();
        /// <summary>
        /// Contains logic for converting back to custom types after saving is done.
        /// </summary>
        /// <remarks>Generally this should undo any conversions made in the <c>PrepareForSaving</c> method.
        /// This is also a good place for common code involved in loading, which can be called from <c>InitializeAfterLoad()</c>.</remarks>
        /// <param name="obj">Will be passed any objects returned from <c>PrepareForSaving</c></param>
        void AfterSaved(IEnumerable<B> obj);
    }

    /// <summary>
    /// Sets the loading priority for <c>ISaveHandler</c>s. Higher is loaded earlier.
    /// </summary>
    /// <remarks>Probably best not to exceed 100 as that'll break dimension loading.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PriorityAttribute : Attribute
    {
        private int priority;
        public int Priority { get => priority; }

        public PriorityAttribute(int priority)
        {
            this.priority = priority;
        }
    }

}

