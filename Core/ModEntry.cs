using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using Utility = FinalDoom.StardewValley.InterdimensionalShed.API.Utility;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal partial class ModEntry : Mod, IAssetLoader, IAssetEditor
    {
        internal static ModConfig config;
        internal static Texture2D myMenuTexture;
        internal static Texture2D myMenuTextureUncolored;

        public override void Entry(IModHelper helper)
        {
            Utility.Mod = this;

            myMenuTexture = Helper.Content.Load<Texture2D >("assets/MenuTiles.png", ContentSource.ModFolder);
            myMenuTextureUncolored = Helper.Content.Load<Texture2D>("assets/MenuTilesUncolored.png", ContentSource.ModFolder);

            new SaveManager();
            new CarpenterMenuCustomizer(helper);
            config = Helper.ReadConfig<ModConfig>();
            Helper.Events.Input.ButtonPressed += Input_ButtonPressed_DebugHelp;
        }

        KeybindList cheats = KeybindList.Parse("LeftControl + LeftAlt + c");
        KeybindList wiz = KeybindList.Parse("LeftControl + LeftAlt + w");
        KeybindList door = KeybindList.Parse("LeftControl + LeftAlt + a");
        KeybindList slot = KeybindList.Parse("LeftControl + LeftAlt + s");
        KeybindList home = KeybindList.Parse("LeftControl + LeftAlt + d");
        KeybindList localInfoFix = KeybindList.Parse("LeftControl + LeftAlt + i");
        KeybindList mayo = KeybindList.Parse("LeftControl + LeftAlt + m");
        private void Input_ButtonPressed_DebugHelp(object sender, ButtonPressedEventArgs e)
        {
            if (cheats.JustPressed())
            {
                Game1.chatBox.enableCheats = true;
            }
            else if (wiz.JustPressed())
            {
                Game1.chatBox.cheat("warp wiz 2 12");
            }
            else if (door.JustPressed())
            {
                Game1.chatBox.cheat("warp farm 15 15");
            }
            else if (slot.JustPressed())
            {
                Game1.chatBox.cheat("warp farm 16 15");
            }
            else if (home.JustPressed())
            {
                Game1.chatBox.cheat("warp farmhouse");
            }
            else if (localInfoFix.JustPressed())
            {
                LocalInfo();
            }
            else if (mayo.JustPressed())
            {
                Helper.ConsoleCommands.Trigger("player_add", new string[]{
                    "name", "mayonnaise", "69", "4"});
            }
        }

        private void LocalInfo()
        {
            var debugOutput = "";
            int grass = 0;
            int trees = 0;
            int other = 0;
            foreach (TerrainFeature t2 in Game1.currentLocation.terrainFeatures.Values)
            {
                if (t2 is Grass)
                {
                    grass++;
                }
                else if (t2 is Tree)
                {
                    trees++;
                }
                else
                {
                    other++;
                }
            }
            debugOutput = debugOutput + "Grass:" + grass + ",  ";
            debugOutput = debugOutput + "Trees:" + trees + ",  ";
            debugOutput = debugOutput + "Other Terrain Features:" + other + ",  ";
            debugOutput = debugOutput + "Objects: " + Game1.currentLocation.objects.Count() + ",  ";
            debugOutput = debugOutput + "temporarySprites: " + Game1.currentLocation.temporarySprites.Count + ",  ";
            Game1.drawObjectDialogue(debugOutput);
        }

        /*********
         ** Asset Loading and Editing Functions
         *********/

        private static List<string> addedMaps = new List<string>()
        {
            "VoidShed1",
            "DimensionShed1",
            "DimensionShed2",
            "DimensionShed3",
            "DimensionShed4",
            "DimensionShed5",
            "DimensionShed6"
        };

        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals($"Buildings/{BlueprintId}") || asset.AssetNameEquals($"Buildings/{BlueprintId}_PaintMask")
                || asset.AssetNameEquals($"Data/Events/{BlueprintId}") ||
                addedMaps.Where(name => asset.AssetNameEquals($"Maps/{name}")).Any();

        }

        public T Load<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals($"Buildings/{BlueprintId}") && typeof(T) == typeof(Texture2D))
            {
                //Utility.TraceLog($" type is {typeof(T).FullName}", LogLevel.Debug);
                return (T)(object)Utility.OverlayModTextureOverVanillaTexture("assets/InterdimensionalShed.png", "Buildings/Big Shed");
            }
            else if (asset.AssetNameEquals($"Buildings/{BlueprintId}_PaintMask"))
            {
                return Helper.Content.Load<T>("Buildings/Big Shed_PaintMask", ContentSource.GameContent);
            }
            else if (asset.AssetNameEquals($"Data/Events/{BlueprintId}"))
            {
                return (T)(object)InterdimensionalShedEventsData;
            }
            var addedMap = addedMaps.Where(name => asset.AssetNameEquals($"Maps/{name}")).SingleOrDefault();
            if (addedMap != null)
            {
                return Helper.Content.Load<T>($"assets/{addedMap}.tmx", ContentSource.ModFolder);
            }
            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        }

        public bool CanEdit<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals("Data/Blueprints") || asset.AssetNameEquals("Data/Events/Farm")
                || asset.AssetNameEquals("Characters/Dialogue/Abigail");
        }

        public void Edit<T>(IAssetData asset)
        {
            if (asset.AssetNameEquals("Data/Blueprints"))
            {
                var editor = asset.AsDictionary<string, string>();
                editor.Data[BlueprintId] = getItemCost() + 
                    // Footprint, door loc, animal door loc, mapToWarpTo, name, desc, type, upgradeFrom, tilesize, maxOccupants, action (eg MineElevator, how we do this?), BuildableLocation, cost, magical
                    $"/7/3/3/2/-1/-1/VoidShed1/Mysterious Shed/Connects your shed with the void. The interior can be decorated and is changed by setting items./Upgrades/Big Shed/96/96/20/null/Farm/{config.GoldCost}/true";
            }
            else if (asset.AssetNameEquals("Data/Events/Farm"))
            {
                editAssetData(asset, FarmEventsData);
            }
            else if (asset.AssetNameEquals("Characters/Dialogue/Abigail"))
            {
                editAssetData(asset, AbigailDialogueCharacters);
            }
        }

        private void editAssetData(IAssetData asset, Dictionary<string, string> data)
        {
            var shed = Game1.getFarm().buildings.FirstOrDefault(b => b.nameOfIndoorsWithoutUnique.Equals(BlueprintId));
            var editor = asset.AsDictionary<string, string>();
            foreach (var pair in data)
            {
                if (shed != null)
                    editor.Data[pair.Key] = pair.Value.Replace("ChangeLocation Shed", "ChangeLocation " + shed.nameOfIndoors);
                else
                    editor.Data[pair.Key] = pair.Value;
            }
        }

        private string getItemCost()
        {
            var _itemCost = new List<int>();
            if (config.OverrideFirstItem)
            {
                if (config.FirstItemId is int FirstItemId && config.FirstItemCount is int FirstItemCount)
                {
                    _itemCost.Add(FirstItemId);
                    _itemCost.Add(FirstItemCount);
                }
            }
            else
            {
                _itemCost.Add(ItemId_IridiumBar);
                _itemCost.Add(42);
            }
            if (config.OverrideSecondItem)
            {
                if (config.SecondItemId is int SecondItemId && config.SecondItemCount is int SecondItemCount && SecondItemCount > 0)
                {
                    _itemCost.Add(SecondItemId);
                    _itemCost.Add(SecondItemCount);
                }
            }
            else
            {
                _itemCost.Add(ItemId_RadioactiveBar);
                _itemCost.Add(10);
            }
            if (config.OverrideThirdItem)
            {
                if (config.ThirdItemId is int ThirdItemId && config.ThirdItemCount is int ThirdItemCount && ThirdItemCount > 0)
                {
                    _itemCost.Add(ThirdItemId);
                    _itemCost.Add(ThirdItemCount);
                }
            }
            else
            {
                _itemCost.Add(ItemId_VoidEssence);
                _itemCost.Add(100);
            }
            return string.Join(" ", _itemCost);
        }
    }
}