﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using SDVUtility = StardewValley.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class Utility
    {
        private static Mod _mod;
        public static Mod Mod {
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
        public static IMonitor Monitor
        {
            get
            {
                return _mod.Monitor;
            }
        }
        public static IManifest ModManifest
        {
            get
            {
                return _mod.ModManifest;
            }
        }
        public static string ModId
        {
            get
            {
                return ModManifest.UpdateKeys[0].Split(':')[1];
            }
        }

        public static Texture2D OverlayModTextureOverVanillaTexture(string modFilename, string vanillaTextureName)
        {
            Texture2D modded = Texture2DToPremultiplied(Helper.Content.Load<Texture2D>(modFilename, ContentSource.ModFolder));
            Texture2D vanilla = Texture2DToPremultiplied(Helper.Content.Load<Texture2D>(vanillaTextureName, ContentSource.GameContent));
            // TODO Throw an error if the sizes aren't the same

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

        public static void TraceLog(string message)
        {
            Monitor.Log(message, LogLevel.Debug);
        }

        public static void Log(string message)
        {
            Monitor.Log(message, LogLevel.Debug);
        }

        /// <summary>
        /// Helper for transferring objects between <c>GameLocation</c>s such as Sheds.
        /// Beware this is destructive to the <c>to</c> location's contents.
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
    }
}
