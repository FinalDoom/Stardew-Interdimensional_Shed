using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using System.Collections.Generic;
using System;
using FinalDoom.StardewValley.InterdimensionalShed.API;

namespace FinalDoom.StardewValley.InterdimensionalShed.Dimensions
{
    internal class ShedDimension : Shed
    {
        private readonly DimensionInfo info;

        public ShedDimension(Building building, DimensionInfo info) : base("Maps/" + info.MapName + Math.Max(1, info.DimensionImplementation.CurrentStage()), info.DisplayName) // Displayname might need to be something else.. it's mostly only used for upgrades and stuff though, which won't exist
        {
            this.info = info;

            // Repeat the logic of Shed constructor since info wasn't available when it called
            wallPaper.SetCountAtLeast(getWalls().Count);
            floor.SetCountAtLeast(getFloors().Count);

            info.DimensionImplementation.StageChanged += StageChanged_UpdateStage;

            
            // set the ambient light to something tinted to our gem TODO
        }

        private void StageChanged_UpdateStage(object sender, DimensionStageChangedArgs e)
        {
            var prev = e.OldStage;
            var current = e.NewStage;
            if (prev != 0)
            {
                while (prev < current)
                {
                    switch (prev)
                    {
                        case 1:
                        case 4:
                            // Shift right to be in front of the door again
                            shiftObjects(19, 0);
                            break;
                        case 2:
                        case 3:
                            // Shift down to be in front of the door again
                            shiftObjects(0, 16);
                            break;
                        case 5:
                            // Shift down twice to be in front of the door again
                            shiftObjects(0, 32);
                            break;
                        default:
                            throw new InvalidOperationException($"You can't upgrade a stage {prev} {typeof(ShedDimension).Name}");
                    }
                    ++prev;
                }
            }
            // essentially setting upgrade level but not in the base class
            mapPath.Set("Maps/" + info.MapName + Math.Max(1, info.DimensionImplementation.CurrentStage()));
            updateMap();
            updateLayout();
        }
        protected override void resetLocalState()
        {
            base.resetLocalState();
            //if (Game1.isDarkOut())
            //{
            //    Game1.ambientLight = new Color(180, 180, 0);
            //}
            var color = info.LightingColor;
            color.R = (byte)(255 - color.R);
            color.G = (byte)(255 - color.G);
            color.B = (byte)(255 - color.B);
            var light = Color.Lerp(color, Color.Black, (Math.Min(Math.Min(color.R, color.G), color.B) / 255));
            Game1.ambientLight = light;
        }

        public override List<Rectangle> getWalls()
        {
            var walls = new List<Rectangle>();
            if (info == null)
            {
                return walls;
            }
            switch (info.DimensionImplementation.CurrentStage())
            {
                case 6:
                    // new row (below)
                    // (1,4) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(1, 49, 6, 3));
                    walls.Add(new Rectangle(12, 49, 6, 3));
                    // left connector, +1 right (2,4) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(18, 53, 2, 3));
                    walls.Add(new Rectangle(20, 49, 6, 3));
                    walls.Add(new Rectangle(31, 49, 6, 3));
                    // left connector, +1 right (3,4) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(37, 53, 2, 3));
                    walls.Add(new Rectangle(39, 49, 6, 3));
                    walls.Add(new Rectangle(50, 49, 6, 3));
                    // left connector, +1 right (4,4) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(56, 53, 2, 3));
                    walls.Add(new Rectangle(58, 49, 6, 3));
                    walls.Add(new Rectangle(69, 49, 6, 3));
                    // left connector, +1 right (5,4) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(75, 53, 2, 3));
                    walls.Add(new Rectangle(77, 49, 6, 3));
                    walls.Add(new Rectangle(88, 49, 6, 3));
                    // new row (below)
                    // (1,5) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(1, 65, 6, 3));
                    walls.Add(new Rectangle(12, 65, 6, 3));
                    // left connector, +1 right (2,5) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(18, 69, 2, 3));
                    walls.Add(new Rectangle(20, 65, 6, 3));
                    walls.Add(new Rectangle(31, 65, 6, 3));
                    // left connector, +1 right (3,5) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(37, 69, 2, 3));
                    walls.Add(new Rectangle(39, 65, 6, 3));
                    walls.Add(new Rectangle(50, 65, 6, 3));
                    // left connector, +1 right (4,5) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(56, 69, 2, 3));
                    walls.Add(new Rectangle(58, 65, 6, 3));
                    walls.Add(new Rectangle(69, 65, 6, 3));
                    // left connector, +1 right (5,5) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(75, 69, 2, 3));
                    walls.Add(new Rectangle(77, 65, 6, 3));
                    walls.Add(new Rectangle(88, 65, 6, 3));
                    goto case 5;
                case 5:
                    // new column, top row
                    // left connector, +1 right back wall (4,1)
                    walls.Add(new Rectangle(56, 5, 2, 3));
                    walls.Add(new Rectangle(58, 1, 17, 3));
                    // left connector, +1 right back wall (5,1)
                    walls.Add(new Rectangle(75, 5, 2, 3));
                    walls.Add(new Rectangle(77, 1, 17, 3));
                    // new row (below at new col)
                    // left connector, +1 right (4,2) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(56, 21, 2, 3));
                    walls.Add(new Rectangle(58, 17, 6, 3));
                    walls.Add(new Rectangle(69, 17, 6, 3));
                    // left connector, +1 right (5,2) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(75, 21, 2, 3));
                    walls.Add(new Rectangle(77, 17, 6, 3));
                    walls.Add(new Rectangle(88, 17, 6, 3));
                    // new row (below at new col)
                    // left connector, +1 right (4,3) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(56, 37, 2, 3));
                    walls.Add(new Rectangle(58, 33, 6, 3));
                    walls.Add(new Rectangle(69, 33, 6, 3));
                    // left connector, +1 right (5,3) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(75, 37, 2, 3));
                    walls.Add(new Rectangle(77, 33, 6, 3));
                    walls.Add(new Rectangle(88, 33, 6, 3));
                    goto case 4;
                case 4:
                    // new row (below)
                    // +1 right (1,3) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(1, 33, 6, 3));
                    walls.Add(new Rectangle(12, 33, 6, 3));
                    // left connector, +1 right (2,3) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(18, 37, 2, 3));
                    walls.Add(new Rectangle(20, 33, 6, 3));
                    walls.Add(new Rectangle(31, 33, 6, 3));
                    // left connector, +1 right (3,3) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(37, 37, 2, 3));
                    walls.Add(new Rectangle(39, 33, 6, 3));
                    walls.Add(new Rectangle(50, 33, 6, 3));
                    goto case 3;
                case 3:
                    // new row (below)
                    // +1 right (1,2) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(1, 17, 6, 3));
                    walls.Add(new Rectangle(12, 17, 6, 3));
                    // left connector, +1 right (2,2) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(18, 21, 2, 3));
                    walls.Add(new Rectangle(20, 17, 6, 3));
                    walls.Add(new Rectangle(31, 17, 6, 3));
                    // left connector, +1 right (3,2) back wall left half, (space for up connector) right half
                    walls.Add(new Rectangle(37, 21, 2, 3));
                    walls.Add(new Rectangle(39, 17, 6, 3));
                    walls.Add(new Rectangle(50, 17, 6, 3));
                    goto case 2;
                case 2:
                    // left connector, +1 right back wall (2,1)
                    walls.Add(new Rectangle(18, 5, 2, 3));
                    walls.Add(new Rectangle(20, 1, 17, 3));
                    // left connector, +1 right back wall (3,1)
                    walls.Add(new Rectangle(37, 5, 2, 3));
                    walls.Add(new Rectangle(39, 1, 17, 3));
                    goto case 1;
                case 1:
                    // initial room back wall (1,1)
                    walls.Add(new Rectangle(1, 1, 17, 3));
                    break;
                default:
                    break;
            }
            return walls;
        }

        public override List<Rectangle> getFloors()
        {
            var floors = new List<Rectangle>();
            if (info == null)
            {
                return floors;
            }
            switch (info.DimensionImplementation.CurrentStage())
            {
                case 6:
                    // new row (below)
                    // room (1,4), up connector
                    floors.Add(new Rectangle(1, 51, 17, 14));
                    floors.Add(new Rectangle(7, 49, 5, 2));
                    // left connector, +1 right room (2,4), up connector
                    floors.Add(new Rectangle(18, 55, 2, 5));
                    floors.Add(new Rectangle(20, 51, 17, 14));
                    floors.Add(new Rectangle(26, 49, 5, 2));
                    // left connector, +1 right room (3,4), up connector
                    floors.Add(new Rectangle(37, 55, 2, 5));
                    floors.Add(new Rectangle(39, 51, 17, 14));
                    floors.Add(new Rectangle(45, 49, 5, 2));
                    // left connector, +1 right room (4,4), up connector
                    floors.Add(new Rectangle(56, 55, 2, 5));
                    floors.Add(new Rectangle(58, 51, 17, 14));
                    floors.Add(new Rectangle(64, 49, 5, 2));
                    // left connector, +1 right room (5,4), up connector
                    floors.Add(new Rectangle(75, 55, 2, 5));
                    floors.Add(new Rectangle(77, 51, 17, 14));
                    floors.Add(new Rectangle(83, 49, 5, 2));
                    // new row (below)
                    // room (1,5), up connector
                    floors.Add(new Rectangle(1, 67, 17, 14));
                    floors.Add(new Rectangle(7, 65, 5, 2));
                    // left connector, +1 right room (2,5), up connector
                    floors.Add(new Rectangle(18, 71, 2, 5));
                    floors.Add(new Rectangle(20, 67, 17, 14));
                    floors.Add(new Rectangle(26, 65, 5, 2));
                    // left connector, +1 right room (3,5), up connector
                    floors.Add(new Rectangle(37, 71, 2, 5));
                    floors.Add(new Rectangle(39, 67, 17, 14));
                    floors.Add(new Rectangle(45, 65, 5, 2));
                    // left connector, +1 right room (4,5), up connector
                    floors.Add(new Rectangle(56, 71, 2, 5));
                    floors.Add(new Rectangle(58, 67, 17, 14));
                    floors.Add(new Rectangle(64, 65, 5, 2));
                    // left connector, +1 right room (5,5), up connector
                    floors.Add(new Rectangle(75, 71, 2, 5));
                    floors.Add(new Rectangle(77, 67, 17, 14));
                    floors.Add(new Rectangle(83, 65, 5, 2));
                    goto case 5;
                case 5:
                    // new column, top row
                    // left connector, +1 right room (4,1), left connector, +1 right room (5,1)
                    floors.Add(new Rectangle(56, 7, 2, 5));
                    floors.Add(new Rectangle(58, 3, 17, 14));
                    floors.Add(new Rectangle(75, 7, 2, 5));
                    floors.Add(new Rectangle(77, 3, 17, 14));
                    // new row (below at new col)
                    // left connector, +1 right room (4,2), up connector
                    floors.Add(new Rectangle(56, 23, 2, 5));
                    floors.Add(new Rectangle(58, 19, 17, 14));
                    floors.Add(new Rectangle(64, 17, 5, 2));
                    // left connector, +1 right room (5,2), up connector
                    floors.Add(new Rectangle(75, 23, 2, 5));
                    floors.Add(new Rectangle(77, 19, 17, 14));
                    floors.Add(new Rectangle(83, 17, 5, 2));
                    // new row (below at new col)
                    // left connector, +1 right room (4,3), up connector
                    floors.Add(new Rectangle(56, 39, 2, 5));
                    floors.Add(new Rectangle(58, 35, 17, 14));
                    floors.Add(new Rectangle(64, 33, 5, 2));
                    // left connector, +1 right room (5,3), up connector
                    floors.Add(new Rectangle(75, 39, 2, 5));
                    floors.Add(new Rectangle(77, 35, 17, 14));
                    floors.Add(new Rectangle(83, 33, 5, 2));
                    goto case 4;
                case 4:
                    // new row (below)
                    // room (1,3), up connector
                    floors.Add(new Rectangle(1, 35, 17, 14));
                    floors.Add(new Rectangle(7, 33, 5, 2));
                    // left connector, +1 right room (2,3), up connector
                    floors.Add(new Rectangle(18, 39, 2, 5));
                    floors.Add(new Rectangle(20, 35, 17, 14));
                    floors.Add(new Rectangle(26, 33, 5, 2));
                    // left connector, +1 right room (3,3), up connector
                    floors.Add(new Rectangle(37, 39, 2, 5));
                    floors.Add(new Rectangle(39, 35, 17, 14));
                    floors.Add(new Rectangle(45, 33, 5, 2));
                    goto case 3;
                case 3:
                    // new row (below)
                    // room (1,2), up connector
                    floors.Add(new Rectangle(1, 19, 17, 14));
                    floors.Add(new Rectangle(7, 17, 5, 2));
                    // left connector, +1 right room (2,2), up connector
                    floors.Add(new Rectangle(18, 23, 2, 5));
                    floors.Add(new Rectangle(20, 19, 17, 14));
                    floors.Add(new Rectangle(26, 17, 5, 2));
                    // left connector, +1 right room (3,2), up connector
                    floors.Add(new Rectangle(37, 23, 2, 5));
                    floors.Add(new Rectangle(39, 19, 17, 14));
                    floors.Add(new Rectangle(45, 17, 5, 2));
                    goto case 2;
                case 2:
                    // left connector, +1 right room (2,1)
                    floors.Add(new Rectangle(18, 7, 2, 5));
                    floors.Add(new Rectangle(20, 3, 17, 14));
                    // left connector, +1 right room (3,1)
                    floors.Add(new Rectangle(37, 7, 2, 5));
                    floors.Add(new Rectangle(39, 3, 17, 14));
                    goto case 1;
                case 1:
                    // initial room floor (1,1)
                    floors.Add(new Rectangle(1, 3, 17, 14));
                    break;
                default:
                    break;
            }
            return floors;
        }
    }
}