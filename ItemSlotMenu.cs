using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class ItemSlotMenu : MenuWithInventory// ItemGrabMenu
    {
        /*********
        ** Fields
        *********/
        private static readonly Texture2D greyscaleObjectSpriteSheet = getGreyscaleObjectSpriteSheet();

        /// <summary>The labels to draw.</summary>
        private readonly List<ClickableComponent> Labels = new List<ClickableComponent>();

        /// <summary>The season buttons to draw.</summary>
        private readonly List<ClickableTextureComponent> ItemDimensionButtons = new List<ClickableTextureComponent>();

        /// <summary>The day buttons to draw.</summary>
        private readonly List<ClickableTextureComponent> DayButtons = new List<ClickableTextureComponent>();

        /// <summary>The unlocked item dimension buttons</summary>
        private readonly List<ClickableTextureComponent> DimensionButtons = new List<ClickableTextureComponent>();

        // Make sure that only one person can alter dimension contents at a time
        public static readonly NetMutex mutex = new NetMutex();
        // TODO also add a check so that you can't alter a dimension that someone is inside rn
        // Maybe a little person icon if someone's inside?
        // You can add items but not remove? yeah -- more of this state should probably be in the building code
        private Item DisplayedItem;
        /// <summary>The callback to invoke when the birthday value changes.</summary>
        private readonly Action<Item> ChangeDimensionSelection;

        private static Texture2D getGreyscaleObjectSpriteSheet()
        {
            var colored = Game1.objectSpriteSheet;
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


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ItemSlotMenu(Item item, Action<Item> changeDimensionSelection)
            //: base(inventory: new List<Item>() { item }, reverseGrab: false, showReceivingMenu: false, highlightFunction: InventoryMenu.highlightAllItems, 
            //      behaviorOnItemSelectFunction: (item, farmer) => { }, message: null, behaviorOnItemGrab: (item, farmer) => { },
            //      snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true) // ItemGrabMenu base
            : base(highlightThisItem, okButton: false, trashCan: false, inventoryXOffset: 0, inventoryYOffset: 0, menuOffsetHack: 0) // MenuWithInventory base
        {
            DisplayedItem = item;
            ChangeDimensionSelection = changeDimensionSelection;
            SetUpPositions();
        }

        public static bool highlightThisItem(Item i)
        {
            return true;
        }

        /// <summary>The method called when the game window changes size.</summary>
        /// <param name="oldBounds">The former viewport.</param>
        /// <param name="newBounds">The new viewport.</param>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            xPositionOnScreen = Game1.viewport.Width / 2 - (632 + borderWidth * 2) / 2;
            yPositionOnScreen = Game1.viewport.Height / 2 - (600 + borderWidth * 2) / 2 - Game1.tileSize;
            SetUpPositions();
        }

        private const string nodimensionplaceholder = "default";
        /// <summary>Regenerate the UI.</summary>
        private void SetUpPositions()
        {
            DimensionButtons.Clear();
            // Add clear dimension button
            var xPosition = xPositionOnScreen + borderWidth; // spaceToClearSideBorder + Game1.tileSize / 4;
            var yPosition = yPositionOnScreen + borderWidth; // + spaceToClearTopBorder + Game1.tileSize / 4;
            DimensionButtons.Add(new ClickableTextureComponent(nodimensionplaceholder, new Rectangle(xPosition, yPosition, Game1.tileSize * 1, Game1.tileSize), "", "", Game1.mouseCursors, new Rectangle(320, 496, 16, 16), Game1.pixelZoom));
            var index = 1;
            foreach (var item in ModEntry.DimensionData.UnlockedDimensions)
            {
                var xOffset = (index % 3) * Game1.tileSize;
                var yOffset = ((index % 9) / 3) * Game1.tileSize;
                if (index >= 9)
                {
                    xOffset += 2 * width / 3;
                }
                var spritesheet = item.Stack > 0 ? Game1.objectSpriteSheet : greyscaleObjectSpriteSheet;
                DimensionButtons.Add(new ClickableTextureComponent(Convert.ToString(item.ParentSheetIndex), new Rectangle(xPosition + xOffset, yPosition + yOffset, Game1.tileSize * 1, Game1.tileSize), "", "", spritesheet, Game1.getSourceRectForStandardTileSheet(spritesheet, item.ParentSheetIndex, 16, 16), Game1.pixelZoom));
                ++index;
            }
        }

        /// <summary>Handle a button click.</summary>
        /// <param name="name">The button name that was clicked.</param>
        private void HandleButtonClick(string name)
        {
            if (name == null)
                return;
            if (name == nodimensionplaceholder)
            {
                Utility.TraceLog("Changing dimension to default shed");
                DisplayedItem = null;
                ChangeDimensionSelection(null);
            }
            else
            {
                var id = Convert.ToInt32(name);
                var item = ModEntry.DimensionData.getDimensionItem(id);
                Utility.TraceLog($"Changing dimension to {item.DisplayName}");
                DisplayedItem = item;
                ChangeDimensionSelection(item);
            }
        }

        /// <summary>The method invoked when the player left-clicks on the menu.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (ClickableTextureComponent button in DimensionButtons)
            {
                if (button.containsPoint(x, y))
                {
                    HandleButtonClick(button.name);
                    button.scale -= 0.5f;
                    button.scale = Math.Max(3.5f, button.scale);
                }
            }
            var clickedPoint = new Point(x, y);
            var slotClicked = new Rectangle(SlotX, SlotY, SlotWidth, SlotHeight).Contains(clickedPoint);
            if (slotClicked)
            {
                if (heldItem != null)
                {
                    if (DisplayedItem != null && heldItem.canStackWith(DisplayedItem))
                    {
                        int stackLeft = DisplayedItem.addToStack(heldItem);
                        if (stackLeft <= 0)
                        {
                            heldItem = null;
                        }
                        SetUpPositions();
                    }
                    else if (DisplayedItem == null && !ModEntry.DimensionData.UnlockedDimensions.Any(item => item.canStackWith(heldItem)))
                    {
                        Game1.exitActiveMenu();
                        DisplayedItem = heldItem;
                        heldItem = null;
                        DelayedAction.functionAfterDelay(checkNewItem, 10);
                    }
                }
                else if (DisplayedItem != null && DisplayedItem.Stack > 0 && SlotItemBoxRect.Contains(clickedPoint))
                {
                    heldItem = DisplayedItem.getOne();
                    heldItem.Stack = DisplayedItem.Stack;
                    DisplayedItem.Stack = 0;
                    SetUpPositions();
                }
            }
            base.receiveLeftClick(x, y, playSound);
        }

        private void checkNewItem()
        {
            var dimensionItem = ModEntry.DimensionData.getDimensionItem(DisplayedItem.ParentSheetIndex);
            var newDimension = dimensionItem != null && dimensionItem.canStackWith(DisplayedItem);
            Game1.multipleDialogues(new string[] { "...",
                newDimension ? "The Shed glows for a moment.." : "Nothing seems to have happened"});
            Game1.afterDialogues = delegate
            {
                if (newDimension)
                {
                    dimensionItem.Stack = 0;
                    dimensionItem.addToStack(DisplayedItem);
                    ChangeDimensionSelection(dimensionItem);
                }
                else
                {
                    Game1.playSound("throwDownITem");
                    Game1.createItemDebris(DisplayedItem, Game1.player.getStandingPosition(), 2);
                }
            };
        }

        /// <summary>The method invoked when the player right-clicks on the lookup UI.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);
        }

        /// <summary>The method invoked when the player hovers the cursor over the menu.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void performHoverAction(int x, int y)
        {
            foreach (ClickableTextureComponent button in DimensionButtons)
            {
                button.scale = button.containsPoint(x, y)
                    ? Math.Min(button.scale + 0.02f, button.baseScale + 0.1f)
                    : Math.Max(button.scale - 0.02f, button.baseScale);
            }
            base.performHoverAction(x, y);
        }
        /*
        /// <summary>Draw the menu to the screen.</summary>
        /// <param name="b">The sprite batch.</param>
        public override void draw(SpriteBatch b)
        {
            // draw menu box
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);
            b.Draw(Game1.daybg, new Vector2((xPositionOnScreen + Game1.tileSize + Game1.tileSize * 2 / 3 - 2), (yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder - Game1.tileSize / 4)), Color.White);

                foreach (ClickableTextureComponent button in DayButtons)
                    button.draw(b);


            // draw labels
            foreach (ClickableComponent label in Labels)
            {
                Color color = Color.Violet;
                Utility.drawTextWithShadow(b, label.name, Game1.smallFont, new Vector2(label.bounds.X, label.bounds.Y), color);
            }
            foreach (ClickableComponent label in Labels)
            {
                string text = "";
                Color color = Game1.textColor;
                Utility.drawTextWithShadow(b, label.name, Game1.smallFont, new Vector2(label.bounds.X, label.bounds.Y), color);
                if (text.Length > 0)
                    Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2((label.bounds.X + Game1.tileSize / 3) - Game1.smallFont.MeasureString(text).X / 2f, (label.bounds.Y + Game1.tileSize / 2)), color);
            }

            // draw OK button
                OkButton.draw(b);

            // draw cursor
            drawMouse(b);
        }
        */

        private int SlotX { get => xPositionOnScreen - borderWidth / 2 + width / 3; }
        private int SlotY { get => yPositionOnScreen + borderWidth - spaceToClearTopBorder + 64 - 64; }
        private int SlotWidth { get => width / 3; }
        private int SlotHeight { get => height - (borderWidth + spaceToClearTopBorder + 192); }
        private Vector2 SlotItemBox { get => new Vector2(xPositionOnScreen + (int)(6 * 64), yPositionOnScreen + 2 * 64 + 32); }
        private Rectangle SlotItemBoxRect { get => new Rectangle(xPositionOnScreen + (int)(6 * 64), yPositionOnScreen + 2 * 64 + 32, 64, 64); }
    public override void draw(SpriteBatch b)
        {
            // Left elevator-style box
            Game1.drawDialogueBox(xPositionOnScreen - borderWidth / 2, yPositionOnScreen + borderWidth - spaceToClearTopBorder + 64 - 64, width / 3, height - (borderWidth + spaceToClearTopBorder + 192), speaker: false, drawOnlyBox: true);
            // Right elevator-style box
            if (DimensionButtons.Count > 16)
            {
                Game1.drawDialogueBox(xPositionOnScreen - borderWidth / 2 + 2 * width / 3, yPositionOnScreen + borderWidth - spaceToClearTopBorder + 64 - 64, width / 3, height - (borderWidth + spaceToClearTopBorder + 192), speaker: false, drawOnlyBox: true);
            }
            // background box
            Game1.drawDialogueBox(SlotX, SlotY, SlotWidth, SlotHeight, speaker: false, drawOnlyBox: true);
            // Dimension buttons
            foreach (var dimension in DimensionButtons)
            {
                dimension.draw(b);
            }
            // box for item
            b.Draw(Game1.menuTexture, SlotItemBox, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);
            if (DisplayedItem != null && DisplayedItem.Stack > 0)
            {
                DisplayedItem.drawInMenu(b, SlotItemBox, 1f);
            }

            base.draw(b, false, false);
            if (heldItem != null)
            {
                heldItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
            }

            // draw cursor
            drawMouse(b);
        }
    }
}
