using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDVUtility = StardewValley.Utility;

namespace FinalDoom.StardewValley.InterdimensionalShed.API
{
    public class Utility
    {
        private static IMod _mod;
        public static IMod Mod
        {
            get
            {
                return _mod;
            }
            set
            {
                if (_mod == null) _mod = value;
            }
        }

        public static IModHelper Helper
        {
            get
            {
                return _mod.Helper;
            }
        }
        public static string ModId
        {
            get
            {
                return _mod.ModManifest.UpdateKeys[0].Split(':')[1];
            }
        }

        /// <summary>
        /// Utility method to log mod operations at a trace level. It's nice to be able to change one place to change the log level for everything.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void TraceLog(string message)
        {
            if (_mod != null)
            {
                _mod.Monitor.Log(message, LogLevel.Debug);
            }
        }

        /// <summary>
        /// Utility method to log mod operations at a debug level. It's nice to be able to change one place to change the log level for everything.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Log(string message)
        {
            if (_mod != null)
            {
                _mod.Monitor.Log(message, LogLevel.Debug);
            }
        }

        /// <summary>
        /// Overlays a mod image on top of a vanilla Stardew texture, to allow modification without republishing vanilla assets, getting permission, etc.
        /// </summary>
        public static Texture2D OverlayModTextureOverVanillaTexture(string modFilename, string vanillaTextureName)
        {
            Texture2D modded = Texture2DToPremultiplied(Helper.Content.Load<Texture2D>(modFilename, ContentSource.ModFolder));
            Texture2D vanilla = Texture2DToPremultiplied(Helper.Content.Load<Texture2D>(vanillaTextureName, ContentSource.GameContent));
            if (modded.Bounds != vanilla.Bounds)
            {
                throw new InvalidOperationException("Textures to overlay must be the same size.");
            }

            Color[] overlaidData = new Color[vanilla.Width * vanilla.Height];
            vanilla.GetData(overlaidData);
            Color[] moddedData = new Color[modded.Width * modded.Height];
            modded.GetData(moddedData);

            for (int i = 0; i < overlaidData.Length; ++i)
            {
                // Using math from https://en.wikipedia.org/wiki/Alpha_compositing#Straight_versus_premultiplied and vars reference that for brain reasons
                var ca = moddedData[i].ToVector4();
                var cb = overlaidData[i].ToVector4();
                var ao = ca.W + cb.W * (1 - ca.W);
                var co = ca + cb * (1 - ca.W);
                co.W = ao;
                overlaidData[i] = new Color(co);
            }
            Texture2D overlaid = new Texture2D(vanilla.GraphicsDevice, vanilla.Width, vanilla.Height);
            overlaid.SetData(overlaidData);
            TraceLog($"Overlaid texture from {modFilename} on top of vanilla texture {vanillaTextureName}");
            return overlaid;
        }

        /// <summary>
        /// Converts a texture from the raw loaded form to premultiplied form.
        /// </summary>
        public static Texture2D Texture2DToPremultiplied(Texture2D texture)
        {
            Color[] textureData = new Color[texture.Width * texture.Height];
            texture.GetData(textureData);
            for (int i = 0; i < textureData.Length; i++)
            {
                textureData[i] = Color.FromNonPremultiplied(textureData[i].ToVector4());
            }
            Texture2D premultipliedTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
            premultipliedTexture.SetData(textureData);
            return premultipliedTexture;
        }

        /// <summary>
        /// Returns a new <see cref="Texture2D"/> that is the input converted to grayscale.
        /// </summary>
        public static Texture2D getGrayscaledSpriteSheet(Texture2D colored)
        {
            var greyscale = new Texture2D(colored.GraphicsDevice, colored.Width, colored.Height);
            var data = new Color[colored.Width * colored.Height];
            colored.GetData(data);
            for (var i = 0; i < data.Length; ++i)
            {
                var vals = data[i].ToVector4();
                var q = (vals.X + vals.Y + vals.Z) / 3;
                q /= vals.W; // optional - undo alpha premultiplication
                data[i] = Color.FromNonPremultiplied(new Vector4(q, q, q, vals.W == 0.0f ? vals.W : 0.5f));
            }
            greyscale.SetData(data);
            return greyscale;
        }

        /// <summary>
        /// Helper for transferring objects between <c>GameLocation</c>s such as Sheds.
        /// Beware this is destructive to the <paramref name="destination"/> location's contents.
        /// This does not remove the objects from the <paramref name="source"/>, merely copies their references.
        /// </summary>
        public static void TransferObjects(GameLocation source, GameLocation destination)
        {
            // Saved objects that should persist
            ClearAndAdd(source.resourceClumps, destination.resourceClumps);
            destination.terrainFeatures.Clear();
            foreach (var tfpair in source.terrainFeatures.Pairs)
            {
                destination.terrainFeatures.Add(tfpair.Key, tfpair.Value);
            }
            ClearAndAdd(source.largeTerrainFeatures, destination.largeTerrainFeatures);
            destination.objects.Clear();
            SDVUtility.transferPlacedObjectsFromOneLocationToAnother(source, destination);
            ClearAndAdd(source.characters, destination.characters);
            ClearAndAdd(source.furniture, destination.furniture);
            // Unsaved objects that should persist
            if (source.IsOutdoors)
            {
                ClearAndAdd(source.critters, destination.critters);
            }
            ClearAndAdd(source.debris, destination.debris);
            source.numberOfSpawnedObjectsOnMap = destination.numberOfSpawnedObjectsOnMap;
        }

        private static void ClearAndAdd<T>(ICollection<T> source, ICollection<T> destination)
        {
            destination.Clear();
            foreach (var x in source)
            {
                destination.Add(x);
            }
        }

        /// <summary>
        /// Short way to get all types for reflection operations without remembering the specifics.
        /// </summary>
        public static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        }

        /// <summary>
        /// Short way to get a specific type by fully qualified name without remembering the specifics.
        /// </summary>
        public static Type GetType(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).Where(t => t != null).Single();
        }
    }
}