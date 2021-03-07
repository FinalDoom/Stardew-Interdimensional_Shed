using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace InterdimensionalShed
{
    internal partial class ModEntry : Mod, IAssetLoader, IAssetEditor
    {
        private ModConfig _config;
        private CarpenterMenu _currentMenu;

        public override void Entry(IModHelper helper)
        {
            _config = Helper.ReadConfig<ModConfig>();
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded_InitializeBuildings;
            Helper.Events.GameLoop.Saving += GameLoop_Saving_UnconvertBuildings;
            Helper.Events.GameLoop.Saved += GameLoop_Saved_ReconvertBuildings;
            //Helper.Events.Player.Warped += Player_Warped;
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is Farm farm)
            {
                foreach (Building b in farm.buildings.Where(b => b.indoors.Value != null && b.indoors.Value.NameOrUniqueName.ToLower().Contains("shed")))
                {
                    Monitor.Log($"shed {b.GetType().FullName} in {b.nameOfIndoors} or {b.nameOfIndoorsWithoutUnique} warps {String.Join(" / ", b.indoors.Value.warps.Select(w => w.TargetName + " x:" + w.TargetX + " y:" + w.TargetY))}", LogLevel.Debug);
                }
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is CarpenterMenu menu && Helper.Reflection.GetField<bool>(menu, "magicalConstruction").GetValue())
            {
                Monitor.Log("Adding blueprint to Wizard Book CarpenterMenu", LogLevel.Debug);
                Helper.Reflection.GetField<List<BluePrint>>(menu, "blueprints").GetValue().Add(new BluePrint(BlueprintId));
                _currentMenu = menu;
                Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked_InterceptBuildingUpgrade;
            }

        }

        public void GameLoop_UpdateTicked_InterceptBuildingUpgrade(object sender, UpdateTickedEventArgs e)
        {
            if (Helper.Reflection.GetField<bool>(_currentMenu, "upgrading").GetValue() && Helper.Reflection.GetField<bool>(_currentMenu, "freeze").GetValue())
            {
                var farm = Game1.getFarm();
                var toUpgrade = farm.getBuildingAt(new Vector2((Game1.viewport.X + Game1.getOldMouseX(ui_scale: false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(ui_scale: false)) / 64));
                if (toUpgrade != null && _currentMenu.CurrentBlueprint.name != null && toUpgrade.buildingType.Equals(_currentMenu.CurrentBlueprint.nameOfBuildingToUpgrade))
                {
                    Helper.Events.GameLoop.UpdateTicked -= GameLoop_UpdateTicked_InterceptBuildingUpgrade;
                    // Keep the correct indoor in the shed, but override the warp to go to one of the other sheds we create

                    // Do the upgrade immediately then swap for our type
                    toUpgrade.buildingType.Value = BlueprintId;
                    toUpgrade.resetTexture();
                    var idsb = new InterdimensionalShedBuilding(toUpgrade);
                    farm.buildings.Remove(toUpgrade);
                    farm.buildings.Add(idsb);

                    if (!FarmLinkedToMultipleDimensions)
                    {
                        InitializeDimensionBuildings();
                    }
                }
            }
        }

        private void InitializeDimensionBuildings()
        {
            Monitor.Log("Initializing configured dimension buildings", LogLevel.Debug);
            var farmhouseWarp = Game1.getFarm().GetMainFarmHouseEntry();
            var buildings = Game1.getFarm().buildings;
            foreach (var dimension in ShedDimensionKeys.Values)
            {
                var b = new Building(new BluePrint("Big Shed"), new Vector2(-100, -100));
                b.modData[ShedDimensionModDataKey] = dimension;
                b.daysOfConstructionLeft.Value = 0;
                foreach (var warp in b.indoors.Value.warps)
                {
                    // Give the warp back sensible defaults, since these are off the map
                    warp.TargetX = farmhouseWarp.X;
                    warp.TargetY = farmhouseWarp.Y;
                }
                buildings.Add(b);
            }
            Monitor.Log($"Dimension buildings initialized {FarmLinkedToMultipleDimensions}", LogLevel.Debug);
        }

        private void ConvertBuildingsToCustomType()
        {
            var farm = Game1.getFarm();
            foreach (var building in farm.buildings.Where(b => b.modData.ContainsKey(SaveKey)).ToList())
            {
                farm.buildings.Remove(building);
                farm.buildings.Add(new InterdimensionalShedBuilding(building));
            }
        }

        private void UnconvertBuildingsFromCustomType()
        {
            InterdimensionalShedBuilding.StoreStaticData();
            var farm = Game1.getFarm();
            foreach (var building in farm.buildings.OfType<InterdimensionalShedBuilding>().ToList())
            {
                building.StoreObjectData();
                var baseBuilding = new Building(new BluePrint(building.buildingType.Value), new Vector2(building.tileX.Value, building.tileY.Value));
                baseBuilding.modData = building.modData;
                baseBuilding.daysOfConstructionLeft.Value = 0;
                farm.buildings.Remove(building);
                farm.buildings.Add(baseBuilding);
            }
        }

        public void OnSaveLoaded_InitializeBuildings(object sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("loaded", LogLevel.Debug);
            if (FarmLinkedToMultipleDimensions)
            {
                Game1.addMail(ShedPurchaseMailId, true, true);
            }
            ConvertBuildingsToCustomType();
        }

        public void GameLoop_Saving_UnconvertBuildings(object sender, SavingEventArgs e)
        {
            Monitor.Log("Unconverting", LogLevel.Debug);
            UnconvertBuildingsFromCustomType();
        }

        public void GameLoop_Saved_ReconvertBuildings(object sender, SavedEventArgs e)
        {
            Monitor.Log("Reconverting", LogLevel.Debug);
            ConvertBuildingsToCustomType();
        }

        public bool CanLoad<T>(IAssetInfo asset)
        {
            return asset.AssetNameEquals($"Buildings/{BlueprintId}") || asset.AssetNameEquals($"Buildings/{BlueprintId}_PaintMask")
                || asset.AssetNameEquals($"Data/Events/{BlueprintId}") || asset.AssetNameEquals($"Maps/{BlueprintId}");
        }

        public T Load<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals($"Buildings/{BlueprintId}") && typeof(T) == typeof(Texture2D))
            {
                //Monitor.Log($" type is {typeof(T).FullName}", LogLevel.Debug);
                return (T)(object)OverlayModTextureOverVanillaTexture("assets/InterdimensionalShed.png", "Buildings/Big Shed");
            }
            else if (asset.AssetNameEquals($"Buildings/{BlueprintId}_PaintMask"))
            {
                return Helper.Content.Load<T>("Buildings/Big Shed_PaintMask", ContentSource.GameContent);
            }
            else if (asset.AssetNameEquals($"Data/Events/{BlueprintId}") && typeof(T) == typeof(Dictionary<string, string>))
            {
                return (T)(object)InterdimensionalShedEventsData;
            }
            else if (asset.AssetNameEquals($"Maps/{BlueprintId}"))
            {
                return Helper.Content.Load<T>("assets/VoidShed.tmx", ContentSource.ModFolder);
            }

            throw new InvalidOperationException($"Unexpected asset '{asset.AssetName}'.");
        }

        public Texture2D OverlayModTextureOverVanillaTexture(string modFilename, string vanillaTextureName)
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
            Monitor.Log($"Overlaid texture from {modFilename} on top of vanilla texture {vanillaTextureName}", LogLevel.Debug);
            return overlaid;
        }

        public Texture2D Texture2DToPremultiplied(Texture2D texture)
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
                    $"/7/3/3/2/-1/-1/{BlueprintId}/Mysterious Shed/Connects your shed with the void. The interior can be decorated and is changed by setting items./Upgrades/Big Shed/96/96/20/null/Farm/{_config.GoldCost}/true";
            }
            else if (asset.AssetNameEquals("Data/Events/Farm"))
            {
                editAssetData(asset, FarmEventsData);
            }
            else if (asset.AssetNameEquals("Characters/Dialogue/Abigail"))
            {
                editAssetData(asset, AbigailDialogueCharacters);
            }
            Monitor.Log(ModManifest.UpdateKeys[0].Split(':')[1], LogLevel.Debug);
        }

        private void editAssetData(IAssetData asset, Dictionary<string, string> data)
        {
            var shed = ((Farm)Game1.getLocationFromName("Farm")).buildings.FirstOrDefault(b => b.nameOfIndoorsWithoutUnique.Equals(BlueprintId));
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
            if (_config.OverrideFirstItem)
            {
                if (_config.FirstItemId is int FirstItemId && _config.FirstItemCount is int FirstItemCount)
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
            if (_config.OverrideSecondItem)
            {
                if (_config.SecondItemId is int SecondItemId && _config.SecondItemCount is int SecondItemCount && SecondItemCount > 0)
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
            if (_config.OverrideThirdItem)
            {
                if (_config.ThirdItemId is int ThirdItemId && _config.ThirdItemCount is int ThirdItemCount && ThirdItemCount > 0)
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

        public bool FarmLinkedToMultipleDimensions
        {
            get
            {
                return Game1.getFarm().buildings.Any(b => b.modData.ContainsKey(ShedDimensionModDataKey) && b.modData[ShedDimensionModDataKey].Equals(ShedDimensionKeys[769]));
            }
        }
    }
}