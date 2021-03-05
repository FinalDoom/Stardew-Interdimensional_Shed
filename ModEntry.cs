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
        private int shedCount;
        private ModConfig _config;
        private CarpenterMenu _currentMenu;
        private ModData _data;

        public override void Entry(IModHelper helper)
        {
            shedCount = 0;
            _config = Helper.ReadConfig<ModConfig>();
            Helper.Events.Display.MenuChanged += OnMenuChanged;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded_InitializeBuildings;
            Helper.Events.GameLoop.Saved += Saved_SaveModData;
            //Helper.Events.Player.Warped += Player_Warped;
            Helper.Events.GameLoop.Saving += Saving_PrepareModDataDictionaryObjects;
        }

        private void Saving_PrepareModDataDictionaryObjects(object sender, SavingEventArgs e)
        {
            foreach (var idsb in Game1.getFarm().buildings.Where(b => b is InterdimensionalShedBuilding).Cast<InterdimensionalShedBuilding>())
            {
                idsb.PrepareModDataDictionaryObjects();
            }
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is Farm farm)
            {
                foreach (Building b in farm.buildings.Where(b => b.indoors.Value != null && b.indoors.Value.NameOrUniqueName.ToLower().Contains("shed")))
                {
                    Monitor.Log($"shed {b.nameOfIndoors} or {b.nameOfIndoorsWithoutUnique}", LogLevel.Debug);
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
                Helper.Events.GameLoop.UpdateTicked += InterceptBuildingUpgrade;
            }

        }

        public void InterceptBuildingUpgrade(object sender, UpdateTickedEventArgs e)
        {
            if (Helper.Reflection.GetField<bool>(_currentMenu, "upgrading").GetValue() && Helper.Reflection.GetField<bool>(_currentMenu, "freeze").GetValue())
            {
                var toUpgrade = Game1.getFarm().getBuildingAt(new Vector2((Game1.viewport.X + Game1.getOldMouseX(ui_scale: false)) / 64, (Game1.viewport.Y + Game1.getOldMouseY(ui_scale: false)) / 64));
                if (toUpgrade != null && _currentMenu.CurrentBlueprint.name != null && toUpgrade.buildingType.Equals(_currentMenu.CurrentBlueprint.nameOfBuildingToUpgrade))
                {
                    var indoors = toUpgrade.indoors;

                    // Do the upgrade immediately
                    toUpgrade.daysUntilUpgrade.Value = 0;
                    BluePrint CurrentBlueprint = new BluePrint("Interdimensional Shed");
                    indoors.Value.mapPath.Value = "Maps\\" + CurrentBlueprint.mapToWarpTo;
#pragma warning disable AvoidNetField // Avoid Netcode types when possible
                    indoors.Value.name.Value = CurrentBlueprint.mapToWarpTo;
#pragma warning restore AvoidNetField // Avoid Netcode types when possible
                    toUpgrade.buildingType.Value = CurrentBlueprint.name;
                    toUpgrade.resetTexture();
                    shedCount++;
                    toUpgrade.modData.Add(SaveKey, "true");
                    //toUpgrade.modData.Add("InterdimensionalIndoorsDictionary", toUpgrade.indoors.Serializer)
                    // Do something like this to get the indoors values as xml or something....
                    /*
                    MemoryStream mstream1 = new MemoryStream(1024);
                    MemoryStream mstream2 = new MemoryStream(1024);
                    if (CancelToTitle)
                    {
                        throw new TaskCanceledException();
                    }
                    yield return 2;
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.CloseOutput = false;
                    Console.WriteLine("Saving without compression...");
                    _ = mstream2;
                    XmlWriter writer = XmlWriter.Create(mstream1, settings);
                    writer.WriteStartDocument();
                    serializer.Serialize(writer, saveData);
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                    mstream1.Close();
                    byte[] buffer1 = mstream1.ToArray();
                    */

                    if (shedCount <= 1)
                    {
                        // First shed purchased
                        Game1.addMail(ShedPurchaseMailId, true, true);
                    }

                    //var isb = _mapper.Map<InterdimensionalShedBuilding>(toUpgrade);
                    //_data.CustomBuildings.Add(new Point(toUpgrade.tileX.Value, toUpgrade.tileY.Value), isb);

                    //farm.buildings.Remove(toUpgrade);
                    //farm.buildings.Add(isb);
                    
                    Helper.Events.GameLoop.UpdateTicked -= InterceptBuildingUpgrade;
                }
            }
        }

        public void OnSaveLoaded_InitializeBuildings(object sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("loaded", LogLevel.Debug);
            _data = Helper.Data.ReadSaveData<ModData>(SaveKey);
            if (_data == null)
            {
                _data = new ModData();
            }
            var farm = (Farm)Game1.getLocationFromName("Farm");
            foreach (var pair in _data.CustomBuildings)
            {
                var point = pair.Key;
                var idsb = pair.Value;
                var baseBuilding = farm.getBuildingAt(new Vector2(point.X, point.Y));
                farm.buildings.Remove(baseBuilding);
                farm.buildings.Add(idsb);
            }
        }

        public void Saved_SaveModData(object sender, SavedEventArgs e)
        {
            Helper.Data.WriteSaveData(SaveKey, _data);
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
                if (_config.SecondItemId is int SecondItemId && _config.SecondItemCount is int SecondItemCount)
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
                if (_config.ThirdItemId is int ThirdItemId && _config.ThirdItemCount is int ThirdItemCount)
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