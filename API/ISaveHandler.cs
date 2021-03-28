using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    /// <summary>
    /// Implementing types will be called by the InterdimensionalShed core's <c>SaveManager</c> to handle saving persistent data.
    /// </summary>
    public interface ISaveHandler
    {
        /// <summary>
        /// Contains logic for preparing a collection of objects for saving.
        /// </summary>
        /// <remarks>
        /// This should store persistent information in modData on default Stardew objects and return null.
        /// Unless object is implementing <see cref="IConvertingSaveHandler"/>, in which case this should also convert custom types into vanilla equivalents.
        /// The custom types should be stored for restoration in <see cref="IConvertingSaveHandler.AfterSaved()"/>. Vanilla types should be removed and replaced
        /// in that method.
        /// </remarks>
        void PrepareForSaving();
        /// <summary>
        /// Contains logic for preparing an object or collection of objects after loading.
        /// </summary>
        /// <remarks>
        /// Generally this will load persistent data from modData and execute any conversions.
        /// For types implementing <c>IConvertingSaveHandler</c>, this will probably share logic with
        /// <see cref="IConvertingSaveHandler.AfterSaved()"/>
        /// </remarks>
        void InitializeAfterLoad();
    }

    /// <summary>
    /// Implementing types will be called by the InterdimensionalShed core's <c>SaveManager</c> to handle converting custom types for saving, as well as persisting data
    /// </summary>
    public interface IConvertingSaveHandler : ISaveHandler
    {
        /// <summary>
        /// Contains logic for converting back to custom types after saving is done.
        /// </summary>
        /// <remarks>
        /// Generally this should undo any conversions made in the <see cref="ISaveHandler.PrepareForSaving()"/> method.
        /// This is also a good place for common code involved in loading, which can be called from <see cref="ISaveHandler.InitializeAfterLoad()"/>.
        /// </remarks>
        void AfterSaved();
    }

    /// <summary>
    /// Sets the loading priority for <see cref="ISaveHandler"/>s. Higher is loaded earlier.
    /// </summary>
    /// <remarks>
    /// Probably best not to stick to less than 100 as that'll break dimension loading. This generally shouldn't be needed anyway.
    /// </remarks>
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

