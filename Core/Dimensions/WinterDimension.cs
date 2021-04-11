using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using xTile.Dimensions;
using xTile.Tiles;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using StardewModdingAPI;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed.Dimensions
{
    internal class WinterDimension : GameLocation
    {
        // TODO consider changing trees into TerrainFeature
        private const string modData_openedPresents = "WinterDimension_openedPresents";
        private const string modData_randomPresents = "WinterDimension_randomPresents";
        private const string modData_stageOpened = "WinterDimension_stageOpened";
        public const string mail_santaLetter = "WinterDimension_santaLetter";

        private readonly Building building;
        private readonly DimensionInfo info;

        /// <summary>
        /// Minutes to freeze per present
        /// </summary>
        private const float freezeMinsPerPresent = 0.25f;
        /// <summary>
        /// Time that we froze at
        /// </summary>
        private ulong freezeTime;

        private const int plainTree = 0;
        private const int baubles = 1;
        private const int lightsAndStar = 2;
        private const int starTileId = 10;
        private const int presentTileIndexBase = 728;
        private readonly Dictionary<int, List<Rectangle>> presentTreeFoliage = new Dictionary<int, List<Rectangle>>()
        {
            {1, new List<Rectangle>() { new Rectangle(13, 6, 7, 8) } },
            {2, new List<Rectangle>() { new Rectangle(43, 6, 7, 8) } },
            {3, new List<Rectangle>() { new Rectangle(43, 13, 7, 8) } },
            {4, new List<Rectangle>() { new Rectangle(25, 44, 7, 8) } },
            {6, new List<Rectangle>() {
                new Rectangle(10, 40, 7, 12), // trunk center at 13,49, exclude upper left 2x2 from presents
                new Rectangle(21, 23, 7, 12), // center at 24,32
                new Rectangle(26, 52, 7, 12), // center at 29,61
                new Rectangle(44, 30, 7, 12), // center at 47,39
            } },
        };

        /// <summary>
        /// Tracks when a stage was unlocked/presents opened
        /// </summary>
        private uint daysPlayedAtStageOpen;

        /// <summary>
        /// Default location for the presents to appear in stages 1-5, randomly assigned locations for stage 6
        /// </summary>
        private Dictionary<int, Dictionary<Point, int>> presents = new Dictionary<int, Dictionary<Point, int>>()
        {
            {1, new Dictionary<Point, int>() {
                { new Point(16, 14), 3 },
            } },
            {2, new Dictionary<Point, int>() {
                { new Point(45,16), 0 },
                { new Point(46,16), 1 },
                { new Point(47,16), 2 },
            } },
            {3, new Dictionary<Point, int>() {
                { new Point(44,22), 3 },
                { new Point(44,23), 1 },
                { new Point(46,24), 2 },
                { new Point(47,24), 3 },
                { new Point(48,23), 0 },
            } },
            {4, new Dictionary<Point, int>() {
                { new Point(25,53), 0 },
                { new Point(26,52), 1 },
                { new Point(27,54), 2 },
                { new Point(28,51), 0 },
                { new Point(28,54), 3 },
                { new Point(28,55), 0 },
                { new Point(29,54), 1 },
                { new Point(30,52), 0 },
                { new Point(30,53), 3 },
            } },
            {5, new Dictionary<Point, int>() {
                { new Point(70,8), 2 },
            } },
            {6, new Dictionary<Point, int>() },
        };
        /// <summary>
        /// Record presents that have been opened in stages 1-5
        /// </summary>
        Dictionary<int, HashSet<Point>> openedPresents = new Dictionary<int, HashSet<Point>>()
        {
            {1, new HashSet<Point>() },
            {2, new HashSet<Point>() },
            {3, new HashSet<Point>() },
            {4, new HashSet<Point>() },
            {5, new HashSet<Point>() },
        };
        /// <summary>
        /// Naughtiness meter
        /// </summary>
        private int punishmentStage;

        public WinterDimension(Building building, DimensionInfo info) : base("Maps/" + info.MapName + Math.Max(1, info.DimensionImplementation.CurrentStage()), info.MapName)
        {
            this.building = building;
            this.info = info;
            seasonOverride = "winter";
            isOutdoors.Value = true;
            ignoreDebrisWeather.Value = true;
            ignoreOutdoorLighting.Value = true;
            punishmentStage = info.DimensionImplementation.CurrentStage() == 5 && openedPresents[5].Count() == 0 ? 3 : 0;
            info.DimensionImplementation.StageChanged += StageChanged_UpdateStage;

            // Initialize presents
            if (building.modData.ContainsKey(modData_openedPresents))
            {
                openedPresents = JsonConvert.DeserializeObject<Dictionary<int, HashSet<Point>>>(building.modData[modData_openedPresents]);
            }
            if (building.modData.ContainsKey(modData_randomPresents))
            {
                presents[6] = JsonConvert.DeserializeObject<Dictionary<Point, int>>(building.modData[modData_randomPresents]);
            }
            if (building.modData.ContainsKey(modData_stageOpened))
            {
                daysPlayedAtStageOpen = Convert.ToUInt32(building.modData[modData_stageOpened]);
            }
            else
            {
                daysPlayedAtStageOpen = Game1.stats.DaysPlayed;
                building.modData[modData_stageOpened] = Convert.ToString(daysPlayedAtStageOpen);
            }
            if (info.DimensionImplementation.CurrentStage() > 0)
            {
                updatePresents();
            }

            Utility.Helper.ConsoleCommands.Add("winter", "", (a, b) =>
            {
                var days = (uint)1;
                if (b.Length == 1)
                {
                    days = Convert.ToUInt32(b[0]);
                }
                //daysPlayedAtStageOpen -= days;
                Game1.stats.DaysPlayed += days;
                DayUpdate(Game1.dayOfMonth);
            });
            Utility.Helper.ConsoleCommands.Add("santa", "", (a, b) =>
            {
                Game1.addMail(mail_santaLetter, true, true);
                StageChanged_UpdateStage(this, new DimensionStageChangedArgs(5, 6));
            });

        }

        /// <summary>
        /// Moves objects in the level when the stage changes, since the size of the map changes
        /// </summary>
        private void StageChanged_UpdateStage(object sender, DimensionStageChangedArgs e)
        {
            daysPlayedAtStageOpen = Game1.stats.DaysPlayed;
            building.modData[modData_stageOpened] = Convert.ToString(daysPlayedAtStageOpen);
            var prev = e.OldStage;
            var current = e.NewStage;
            // essentially setting upgrade level but not in the base class
            mapPath.Set("Maps/" + info.MapName + Math.Max(1, current));
            updateMap();
            updateWarps();
            punishmentStage = current == 5 ? 3 : 0;
            if (prev != 0)
            {
                while (prev < current)
                {
                    switch (prev)
                    {
                        case 1:
                            shiftObjects(30, 0);
                            break;
                        case 2:
                            shiftObjects(0, 7);
                            break;
                        case 3:
                            shiftObjects(0, 8);
                            break;
                        case 4:
                            shiftObjects(-100, -100);
                            break;
                        case 5:
                            // Shift back into view + 1x
                            shiftObjects(99, 100);
                            break;
                        default:
                            throw new InvalidOperationException($"You can't upgrade a stage {prev} {typeof(WinterDimension).Name}");
                    }
                    ++prev;
                }
            }
            updatePresents();
        }

        /*********
         ** Punishment functions
         *********/

        public override int getExtraMillisecondsPerInGameMinuteForThisLocation()
        {
            return punishmentStage > 0 ? 3600000 : 0;
        }

        public override void UpdateWhenCurrentLocation(GameTime time)
        {
            if (punishmentStage > 0 && Game1.player.movedDuringLastTick())
            {
                if (punishmentStage == 3)
                {
                    if (freezeTime == 0)
                    { // Set "stuck" flag, then check that going forward
                        freezeTime = Game1.player.millisecondsPlayed;
                    }
                    if (freezeTime > 0)
                    {
                        if (Game1.player.millisecondsPlayed - freezeTime < openedPresents.Values.Select(d => d.Count()).Sum() * freezeMinsPerPresent * 60 * 1000)
                        {
                            // Move the snow/sprites in the direction opposite of attempted movement
                            var spriteDirection = Game1.player.getMostRecentMovementVector() * -1;
                            // Freeze player in place, but modify the dirt poofs and snow to create the illusion of movement
                            Game1.player.Position = Game1.player.lastPosition;
                            temporarySprites.ForEach(ts =>
                            {
                                if (ts.Texture.Name.Replace('\\', '/') == "TileSheets/animations")
                                {
                                    int row = ts.sourceRect.Y / 64;
                                    // Dirt poofs are rows 46 12 and 16, from Utility.addDirtPuffs
                                    if (row == 46 || row == 12 || row == 16)
                                    {
                                        ts.motion = spriteDirection;
                                    }
                                }
                            });
                            // Make the weather also move accordingly
                            Game1.snowPos += spriteDirection;
                        }
                        else
                        {
                            punishmentStage--;
                            warps.RemoveAt(0);
                        }
                    }

                }
                else if (punishmentStage == 2)
                {
                    if (Game1.player.Position.X / Game1.tileSize < 16)
                    {
                        // Remove right side shroud
                        // Rectangle of 45,0,35,65
                        for (var x = 45; x < 80; ++x)
                        {
                            for (var y = 0; y < 65; ++y)
                            {
                                removeTile(x, y, "AlwaysFront");
                            }
                        }
                        punishmentStage--;
                    }
                }
                else if (punishmentStage == 1)
                {
                    if (Game1.player.Position.Y / Game1.tileSize < 11)
                    {
                        // Remove bottom side shroud
                        // Rectangle of 0,43,80,22
                        for (var x = 0; x < 80; ++x)
                        {
                            for (var y = 42; y < 65; ++y)
                            {
                                removeTile(x, y, "AlwaysFront");
                            }
                        }
                        punishmentStage--;
                    }
                }
            }
            base.UpdateWhenCurrentLocation(time);
        }

        public override void tryToAddCritters(bool onlyIfOnScreen = false)
        {
            // Get rid of birds and so on that will screw up the visual impression
            if (punishmentStage > 0)
            {
                // This isn't working?
                return;
            }
            base.tryToAddCritters(onlyIfOnScreen);
        }

        /*********
         ** Location items functions
         *********/

        public override void spawnObjects()
        {
            numberOfSpawnedObjectsOnMap -= 2 * (info.DimensionImplementation.CurrentStage() - 1);
            base.spawnObjects();
        }

        private readonly Rectangle StarPondDownFacing = new Rectangle(15, 0, 18, 10);
        private readonly Rectangle StarPondRightFacing = new Rectangle(9, 6, 4, 17);
        private readonly Rectangle StarPondGeneral = new Rectangle(14, 11, 25, 19);
        private readonly Rectangle StarPondWater = new Rectangle(15, 6, 24, 20);
        private readonly Rectangle CandyPondLeftFacing = new Rectangle(66, 34, 6, 10);
        private readonly Rectangle CandyPondGeneral = new Rectangle(43, 28, 24, 35);
        private readonly Rectangle CandyPondWater = new Rectangle(43, 28, 25, 35);
        private const int Ocean = 1;
        private const int Mountain = 2;
        private const int River = 4;
        private const int FacingUp = 0;
        private const int FacingRight = 1;
        private const int FacingDown = 2;
        private const int FacingLeft = 3;
        public override int getFishingLocation(Vector2 tile)
        {
            if (info.DimensionImplementation.CurrentStage() == 6)
            {
                // Maybe there's a better way but if you're standing on each other hopefully you're fishing the same spot
                var who = farmers.Where(f => f.getTileLocation() == tile).First();
                var point = tile.ToPoint();
                if (who.FacingDirection == FacingDown && StarPondDownFacing.Contains(point) ||
                    who.facingDirection == FacingRight && StarPondRightFacing.Contains(point) ||
                    StarPondGeneral.Contains(point))
                {
                    return Mountain;
                }
                if (who.facingDirection == FacingLeft && CandyPondLeftFacing.Contains(point) ||
                    CandyPondGeneral.Contains(point))
                {
                    return River;
                }
                return Ocean;
            }
            return base.getFishingLocation(tile);
        }

        public override void drawWaterTile(SpriteBatch b, int x, int y)
        {
            // Don't draw water on the void "ocean" tiles
            var point = new Point(x, y);
            if (StarPondWater.Contains(point) || CandyPondWater.Contains(point))
            {
                base.drawWaterTile(b, x, y);
            }
        }

        public static string LocationFishData(IDictionary<string, string> locationsData)
        {
            var fish = new Dictionary<string, HashSet<int>>();
            var forestRiver = locationsData["Forest"].Split('/')[7].Split(' ');
            for (var i = 0; i < forestRiver.Length; i = i + 2)
            {
                if (forestRiver[i + 1] == "0")
                {
                    if (!fish.ContainsKey(forestRiver[i]))
                    {
                        fish[forestRiver[i]] = new HashSet<int>();
                    }
                    fish[forestRiver[i]].Add(River);
                }
            }
            var townRiver = locationsData["Town"].Split('/')[7].Split(' ');
            for (var i = 0; i < townRiver.Length; i = i + 2)
            {
                if (!fish.ContainsKey(townRiver[i]))
                {
                    fish[townRiver[i]] = new HashSet<int>();
                }
                fish[townRiver[i]].Add(River);
            }
            var beach = locationsData["Beach"].Split('/')[7].Split(' ');
            for (var i = 0; i < beach.Length; i = i + 2)
            {
                if (!fish.ContainsKey(beach[i]))
                {
                    fish[beach[i]] = new HashSet<int>();
                }
                fish[beach[i]].Add(Ocean);
            }
            var mountain = locationsData["Mountain"].Split('/')[7].Split(' ');
            for (var i = 0; i < mountain.Length; i = i + 2)
            {
                if (!fish.ContainsKey(mountain[i]))
                {
                    fish[mountain[i]] = new HashSet<int>();
                }
                fish[mountain[i]].Add(Mountain);
            }
            return "-1/-1/-1/" + string.Join(" ", fish.SelectMany(kvp => kvp.Value.Select(loc => kvp.Key + " " + Convert.ToString(loc))));
        }

        /*********
         ** Present-related functions
         *********/

        public override bool checkAction(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            var location = new Point(tileLocation.X, tileLocation.Y);
            var stage = info.DimensionImplementation.CurrentStage();
            if (Game1.didPlayerJustRightClick())
            {
                if ((stage == 1 || stage == 5 || Game1.stats.DaysPlayed - daysPlayedAtStageOpen >= 2) && presents.ContainsKey(stage) && presents[stage].ContainsKey(location))
                {
                    openPresent(stage, location);
                    return true;
                }
            }
            return base.checkAction(tileLocation, viewport, who);
        }
        // TODO should probably add these dimensions to Game1.locations.. maybe. for warping. but do we wanna? idk

        public override void DayUpdate(int dayOfMonth)
        {
            var stage = info.DimensionImplementation.CurrentStage();
            if (stage == 5 && presents[5].Count() == openedPresents[5].Count())
            {
                Game1.addMail(mail_santaLetter, true, true);
                stage = info.DimensionImplementation.CurrentStage();
            }
            if (stage == 6)
            {
                if (!mapPath.Value.EndsWith("6"))
                {
                    // Do the update to stage 6
                    StageChanged_UpdateStage(this, new DimensionStageChangedArgs(5, 6));
                }
                Random r = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed);
                // Baubles are shown at 1, lights at 2+; days are reset when all presents are open, don't add more presents until 7+
                Utility.Log($"Played {Game1.stats.daysPlayed} vs {daysPlayedAtStageOpen}");
                if (Game1.stats.DaysPlayed - daysPlayedAtStageOpen >= 6)
                {
                    // Generate presents
                    if (r.NextDouble() < 0.25)
                    {
                        var count = 1;
                        if (r.NextDouble() < 0.5) // 1 in 2
                        {
                            count = 3;
                            if (r.NextDouble() < 0.1) // 1 in 20
                            {
                                count = 5;
                                if (r.NextDouble() < 0.2) // 1 in 100
                                {
                                    count = 8;
                                }
                            }
                        }
                        // Randomly choose tree
                        var tree = presentTreeFoliage[6][r.Next(4)];
                        // gaussian in 18x2 square around tree - 2x2 in upper left (16x2)
                        for (var i = 0; i < count; ++i)
                        {
                            for (var tries = 5; tries > 0; --tries)
                            {
                                // Range of 18, we can throw out the lowest 2x2, keeps it centered under the tree and easy numbers
                                // Gets coordinates in a 18 x 2 square, which we map around the tree trunk (2x6), so y ends up being distance from the trunk, x is radial location. Ish.
                                var x = Convert.ToInt32(Utility.RandomGaussian(r, 9, 3));
                                var y = r.Next(2);
                                Point present;
                                switch (x)
                                {
                                    // 2x2 left of the trunk
                                    case 2:
                                    case 3:
                                        Utility.Log($"Case {x} putting point at {tree.X}+{y}({tree.X + y}), {tree.Y}+6+{x}({tree.Y + 6 + x})");
                                        present = new Point(tree.X + y, tree.Y + 6 + x);
                                        break;
                                    // 7x2 at the bottom, under the trunk
                                    case 4:
                                    case 5:
                                    case 6:
                                    case 7:
                                    case 8:
                                    case 9:
                                    case 10:
                                        Utility.Log($"Case {x} putting point at {tree.X}-4+{x}({tree.X - 4 + x}), {tree.Y}+10+{y}({tree.Y + 10 + y})");
                                        present = new Point(tree.X - 4 + x, tree.Y + 10 + y);
                                        break;
                                    // 2x2 right of the trunk
                                    case 11:
                                    case 12:
                                        Utility.Log($"Case {x} putting point at {tree.X}+5+{y}({tree.X + 5 + y}), {tree.Y}-3+{x}({tree.Y - 3 + x})");
                                        present = new Point(tree.X + 5 + y, tree.Y - 3 + x);
                                        break;
                                    // 5x2 behind the trunk and to the right, ignore 2x2 on the left (throw out 0, 1, 18)
                                    case 13:
                                    case 14:
                                    case 15:
                                    case 16:
                                    case 17:
                                        Utility.Log($"Case {x} putting point at {tree.X}+19-{x}({tree.X + 19 - x}), {tree.Y}+7-{y}({tree.Y + 7 - y})");
                                        present = new Point(tree.X + 19 - x, tree.Y + 7 - y);
                                        break;
                                    default:
                                        continue;
                                }
                                if (!isTileLocationTotallyClearAndPlaceable(present.X, present.Y))
                                {
                                    continue;
                                }
                                presents[6][present] = r.Next(4);
                            }
                        }
                        // Set up for baubles
                        daysPlayedAtStageOpen = Game1.stats.DaysPlayed - 1;
                        building.modData[modData_stageOpened] = Convert.ToString(daysPlayedAtStageOpen);
                        building.modData[modData_randomPresents] = JsonConvert.SerializeObject(presents[6]);
                    }
                    else
                    {
                        Utility.Log("Roll failed");
                    }
                }
            }
            updatePresents();
            base.DayUpdate(dayOfMonth);
        }

        /// <summary>
        /// Updates the map to show presents on the buildings layer and baubles/lights/nothing on the tree foliage (Front/AlwaysFront) layers accordingly.
        /// </summary>
        /// <remarks>
        /// For stage 1 and stage 5, presents are added automatically without a day passing. For all other stages, baubles will be shown a
        /// day before presents are shown, and lights will be shown along with presents.
        /// </remarks>
        private void updatePresents()
        {
            var stage = info.DimensionImplementation.CurrentStage();
            var presentsAvailable = stage == 6 && presents[6].Count() > 0 || stage != 6 && presents[stage].Count() != openedPresents[stage].Count();
            var daysPassedSinceUnlock = Game1.stats.DaysPlayed - daysPlayedAtStageOpen;
            var treeType = !presentsAvailable ? plainTree : (stage == 1 || daysPassedSinceUnlock >= 2 ? lightsAndStar : daysPassedSinceUnlock == 1 ? baubles : plainTree);
            if (stage == 1 || stage == 5 || daysPassedSinceUnlock >= 2)
            {
                presents[stage].Keys.ToList().ForEach(l =>
                {
                    setPresent(stage, l, stage == 6 || !openedPresents[stage].Contains(l) ? presents[stage][l] : -1);
                });
            }
            if (presentTreeFoliage.ContainsKey(stage))
            {
                presentTreeFoliage[stage].ForEach(r => convertTree(r, stage <= 5 || presents[6].Keys.Any(p => r.Contains(p.X, p.Y)) ? treeType : plainTree));
            }
        }

        /// <summary>
        /// "Opens" a present at the specified location, special logic for stage 5
        /// </summary>
        private void openPresent(int stage, Point tileLocation)
        {
            if (stage == 5)
            {
                // TODO externalize
                Game1.drawObjectDialogue("There's a colorful note taped to the present box, next to the bow.");
                Game1.afterDialogues += () =>
                {
                    var message = "I know you have been nice.^But you have also been" +
                        $"{(openedPresents.Values.Select(d => d.Count()).Sum() > 10 ? " very" : "")} naughty.^" +
                        "Now that you have had some time to think, I hope you try to be less greedy.   ^   -S. C.";
                    // Something is wrong with this message apparently
                    Game1.activeClickableMenu = new SantaLetterViewerMenu(message, $"To: {Game1.player.Name} From: S. C.", () =>
                    {
                        presentMenu(stage, tileLocation);
                        // Remove the walls blocking movement
                        // Nearest wall at 68,12,10,1
                        for (var x = 68; x < 78; ++x)
                        {
                            removeTile(x, 12, "Buildings");
                        }
                        // All other walls in 12,12,56,36
                        for (var x = 12; x < 68; ++x)
                        {
                            for (var y = 12; y < 48; ++y)
                            {
                                removeTile(x, y, "Buildings");
                            }
                        }
                    });
                };
            }
            else
            {
                presentMenu(stage, tileLocation);
            }
        }


        /// <summary>
        /// Shows the item contents of an opened present for the player to take.
        /// </summary>
        private void presentMenu(int stage, Point tileLocation)
        {
            playSound("shwip");
            playSound("leafrustle");

            // From Game1.createRadialDebris
            var debrisOrigin = new Vector2(tileLocation.X * 64 + 64, tileLocation.Y * 64 + 64);
            var debrisType = 12; // Wood debris
            var color = presents[stage][tileLocation] switch { 0 => Color.DodgerBlue, 1 => Color.Red, 2 => Color.LimeGreen, _ => Color.MediumPurple, };
            var bits = new List<Debris>()
            {
                new Debris(debrisType, 2, debrisOrigin, debrisOrigin + new Vector2(-64f, 0f), 0),
                new Debris(debrisType, 2, debrisOrigin, debrisOrigin + new Vector2(64f, 0f), 0),
                new Debris(debrisType, 2, debrisOrigin, debrisOrigin + new Vector2(0f, -64f), 0),
                new Debris(debrisType, 2, debrisOrigin, debrisOrigin + new Vector2(0f, 64f), 0),
            };
            bits.ForEach(d =>
            {
                d.chunksColor.Value = color;
                debris.Add(d);
            });

            Game1.activeClickableMenu = new ItemGrabMenu(getPresentContents(stage, tileLocation), this).setEssential(essential: true);
            (Game1.activeClickableMenu as ItemGrabMenu).source = 3;
            setPresent(stage, tileLocation);
        }

        /// <summary>
        /// Gets random present contents from a defined set for each type of present
        /// </summary>
        private IList<Item> getPresentContents(int stage, Point tileLocation)
        {
            var type = presents[stage][tileLocation];
            Random r = new Random((int)Game1.stats.DaysPlayed * stage + (int)Game1.uniqueIDForThisGame / 2);
            List<Item> contents = new List<Item>();
            if (stage == 5)
            {
                var coal = new Object(382, 5);
                contents.Add(coal);
            }
            else
            {
                switch (type)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        var fruit = new Object(414, r.Next(stage * 5, stage * 12));
                        contents.Add(fruit);
                        break;
                }
            }
            return contents;
            // TODO more treasure table stuff
        }

        /// <summary>
        /// Converts tree representation on the map betwen different Christmas themes
        /// </summary>
        /// <param name="type"></param>
        private void convertTree(Rectangle location, int type)
        {
            Utility.Log($"Converting tree at {location.X},{location.Y} to {type}");
            var alwaysFrontLayer = map.GetLayer("AlwaysFront");
            var alwaysFront = alwaysFrontLayer.Tiles;
            var copy = map.GetLayer(type == 0 ? "Plain" : type == 1 ? "Baubles" : "Lights").Tiles;

            for (var x = location.X; x <= location.X + location.Width; ++x)
            {
                for (var y = location.Y; y <= location.Y + location.Height; ++y)
                {
                    if (copy[x, y] == null)
                    {
                        continue;
                    }
                    if (alwaysFront[x, y] != null)
                    {
                        alwaysFront[x, y] = copy[x, y].Clone(alwaysFrontLayer);
                    }
                }
            }
        }

        /// <summary>
        /// Adds (<paramref name="presentType"/> >= 0) or removes (<paramref name="presentType"/> < 0) a present from the buildings layer of the map and related dictionaries.
        /// </summary>
        private void setPresent(int stage, Point location, int presentType = -1)
        {
            if (presentType > -1)
            {
                var buildings = map.GetLayer("Buildings");
                var tilesheet = map.GetTileSheet("zz_festival");
                buildings.Tiles[new Location(location.X, location.Y)] = new StaticTile(buildings, tilesheet, BlendMode.Alpha, tileIndex: presentTileIndexBase + presentType);
                presents[stage][location] = presentType;
                if (stage == 6)
                {
                    building.modData[modData_randomPresents] = JsonConvert.SerializeObject(presents[stage]);
                }
            }
            else
            {
                removeTile(location.X, location.Y, "Buildings");
                if (stage == 6)
                {
                    presents[6].Remove(location);
                    building.modData[modData_randomPresents] = JsonConvert.SerializeObject(presents[6]);
                    if (presents[6].Count() == 0)
                    {
                        daysPlayedAtStageOpen = (uint)(Game1.stats.DaysPlayed + Convert.ToInt32(Utility.RandomGaussian(0, 2d/3)));
                    }
                }
                else
                {
                    openedPresents[stage].Add(location);
                    building.modData[modData_openedPresents] = JsonConvert.SerializeObject(openedPresents);
                }
            }
        }
    }
}