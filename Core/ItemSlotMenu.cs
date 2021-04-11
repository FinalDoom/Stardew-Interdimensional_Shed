using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using SDVUtility = StardewValley.Utility;
using SDVObject = StardewValley.Object;
using StardewValley.Menus;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley.BellsAndWhistles;
using StardewValley.Tools;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    internal class ItemSlotMenu : MenuWithInventory
    {
        private const string nodimensionplaceholder = "default";
        private readonly DimensionData data = Singleton<DimensionData>.Instance;

        /*********
        ** Fields
        *********/

        /// <summary>Greyscaled object sprite sheet for use in showing inactive but unlocked</summary>
        private static readonly Texture2D greyscaleObjectSpriteSheet = Utility.getGrayscaledSpriteSheet(Game1.objectSpriteSheet);

        /// <summary>The unlocked item dimension buttons</summary>
        private readonly List<ClickableTextureComponent> DimensionButtons = new List<ClickableTextureComponent>();
        /// <summary>The item dimension buttons for dimensions currently showing hints</summary>
        private readonly List<ClickableTextureComponent> HintButtons = new List<ClickableTextureComponent>();

        // Make sure that only one person can alter dimension contents at a time
        private static readonly NetMutex mutex = new NetMutex();

        // TODO also add a check so that you can't alter a dimension that someone is inside rn
        // Maybe a little person icon if someone's inside?
        // You can add items but not remove? yeah -- more of this state should probably be in the building code

        /// <summary>The dimension info associated with the currently selected dimension or null for default shed</summary>
        private DimensionInfo CurrentDimension;
        /// <summary>The callback to invoke when the birthday value changes.</summary>
        private readonly Action<DimensionInfo> ChangeDimensionSelection;

        // Scrolling components and data
        private bool scrolling;
        private ClickableTextureComponent upArrow;
        private ClickableTextureComponent downArrow;
        private ClickableTextureComponent scrollBar;
        private Rectangle scrollBarRunner;
        private int currentRow;
        private int rows;
        // Scrolling constants
        private const int rowsDisplayable = 6;
        private const int itemsPerRow = 3;
        private const int itemsDisplayable = itemsPerRow * rowsDisplayable;

        /*********
        ** Public methods
        *********/

        /// <summary>Construct an instance.</summary>
        public ItemSlotMenu(DimensionInfo currentDimension, Action<DimensionInfo> changeDimensionSelection)
            //: base(inventory: new List<Item>() { item }, reverseGrab: false, showReceivingMenu: false, highlightFunction: InventoryMenu.highlightAllItems, 
            //      behaviorOnItemSelectFunction: (item, farmer) => { }, message: null, behaviorOnItemGrab: (item, farmer) => { },
            //      snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true) // ItemGrabMenu base
            : base(InventoryMenu.highlightAllItems, okButton: false, trashCan: false, inventoryXOffset: 16, inventoryYOffset: 132, menuOffsetHack: 0) // MenuWithInventory base
        {
            this.CurrentDimension = currentDimension;
            ChangeDimensionSelection = changeDimensionSelection;
            height += 128;
            yPositionOnScreen -= 128;
            SetUpPositions();
        }

        /// <summary>The method called when the game window changes size.</summary>
        /// <param name="oldBounds">The former viewport.</param>
        /// <param name="newBounds">The new viewport.</param>
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            xPositionOnScreen = Game1.viewport.Width / 2 - (800 + borderWidth * 2) / 2;
            yPositionOnScreen = Game1.viewport.Height / 2 - (600 + borderWidth * 2) / 2 - 128;
            SetUpPositions();
        }

        /// <summary>Regenerate the UI elements.</summary>
        private void SetUpPositions()
        {
            DimensionButtons.Clear();
            HintButtons.Clear();

            // Add clear dimension button
            var xPosition = xPositionOnScreen + borderWidth + 8 + 4;
            var yPosition = yPositionOnScreen + borderWidth + 64 + 8;
            var index = 0;
            var visibleDimensions = data.DiscoveredDimensions;
            var hintedDimensions = data.HintedDimensions;
            // Plus 1 for the default dimension
            rows = (int)Math.Ceiling((float)(visibleDimensions.Count() + hintedDimensions.Count() + 1) / itemsPerRow);
            var toSkip = (currentRow) * itemsPerRow;
            if (toSkip <= 0)
            {
                DimensionButtons.Add(new ClickableTextureComponent(nodimensionplaceholder, new Rectangle(xPosition, yPosition, Game1.tileSize * 1, Game1.tileSize), "", "", Game1.mouseCursors, new Rectangle(320, 496, 16, 16), Game1.pixelZoom));
                index++;
            } 
            else
            {
                toSkip--;
            }
            foreach (var info in visibleDimensions)
            {
                if (toSkip > 0)
                {
                    toSkip--;
                    continue;
                }
                if (index >= itemsDisplayable)
                {
                    break;
                }
                var item = info.DimensionImplementation.Item;
                var xOffset = (index % itemsPerRow) * Game1.tileSize;
                var yOffset = ((index % itemsDisplayable) / itemsPerRow) * (Game1.tileSize + 6);
                var spritesheet = info.DimensionImplementation.CurrentStage() > 0 ? Game1.objectSpriteSheet : greyscaleObjectSpriteSheet;
                DimensionButtons.Add(new ClickableTextureComponent(Convert.ToString(item.ParentSheetIndex), new Rectangle(xPosition + xOffset, yPosition + yOffset, Game1.tileSize, Game1.tileSize), "", "", spritesheet, Game1.getSourceRectForStandardTileSheet(spritesheet, item.ParentSheetIndex, 16, 16), Game1.pixelZoom));
                ++index;
            }
            var questionMarkSourceRectangle = new Rectangle(31 * 8 % SpriteText.spriteTexture.Width, 31 * 8 / SpriteText.spriteTexture.Width * 16, 8, 16);
            foreach (var info in hintedDimensions)
            {
                if (toSkip > 0)
                {
                    toSkip--;
                    continue;
                }
                if (index >= itemsDisplayable)
                {
                    break;
                }
                var item = info.DimensionImplementation.Item;
                var xOffset = (index % itemsPerRow) * Game1.tileSize;
                var yOffset = ((index % itemsDisplayable) / itemsPerRow) * (Game1.tileSize + 6);
                HintButtons.Add(new ClickableTextureComponent(Convert.ToString(item.ParentSheetIndex), new Rectangle(xPosition + xOffset, yPosition + yOffset, Game1.tileSize * 1, Game1.tileSize), "", "", SpriteText.spriteTexture, questionMarkSourceRectangle, Game1.pixelZoom));
                ++index;
            }

            if (rows > rowsDisplayable)
            {
                upArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + 64 + 16, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
                downArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen - 48, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
                scrollBar = new ClickableTextureComponent(new Rectangle(upArrow.bounds.X + 12, upArrow.bounds.Y + upArrow.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
                scrollBarRunner = new Rectangle(scrollBar.bounds.X, upArrow.bounds.Y + upArrow.bounds.Height + 4, scrollBar.bounds.Width, height - 64 - 64 - upArrow.bounds.Height - 28);
            }
            else
            {
                upArrow = null;
                downArrow = null;
                scrollBar = null;
                scrollBarRunner = default;
            }
        }

        /// <summary>Handle a button click.</summary>
        /// <param name="name">The button name that was clicked.</param>
        private void HandleButtonClick(string name)
        {
            if (name == null)
            {
                return;
            }
            if (name == nodimensionplaceholder)
            {
                Utility.TraceLog("Changing dimension to default shed");
                CurrentDimension = null;
            }
            else
            {
                var itemId = Convert.ToInt32(name);
                CurrentDimension = data.getDimensionInfo(itemId);
                Utility.TraceLog($"Changing dimension to {CurrentDimension.DisplayName}");
                ChangeDimensionSelection(CurrentDimension);
            }
            ChangeDimensionSelection(CurrentDimension);
        }

        private void downArrowPressed()
        {
            downArrow.scale = downArrow.baseScale;
            currentRow++;
            setScrollBarToCurrentIndex();
        }

        private void upArrowPressed()
        {
            upArrow.scale = upArrow.baseScale;
            currentRow--;
            setScrollBarToCurrentIndex();
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
            foreach (ClickableTextureComponent button in HintButtons)
            {
                if (button.containsPoint(x, y))
                {
                    HandleButtonClick(button.name);
                    button.scale -= 0.5f;
                    button.scale = Math.Max(3.5f, button.scale);
                }
            }

            // Scrollbar stuff
            if (downArrow.containsPoint(x, y) && currentRow < Math.Max(0, rows - rowsDisplayable))
            {
                downArrowPressed();
                Game1.playSound("shwip");
            }
            else if (upArrow.containsPoint(x, y) && currentRow > 0)
            {
                upArrowPressed();
                Game1.playSound("shwip");
            }
            else if (scrollBar.containsPoint(x, y))
            {
                scrolling = true;
            }
            else if (!downArrow.containsPoint(x, y) && x < scrollBar.bounds.X + 64 && x > scrollBar.bounds.X - 64 && y > scrollBar.bounds.Y && y < downArrow.bounds.Y + downArrow.bounds.Height)
            {// TODO fix this, behavior of scrollbar not clicking directly on it is weird
                scrolling = true;
                leftClickHeld(x, y);
                releaseLeftClick(x, y);
            }

            // Item slot stuff
            var clickedPoint = new Point(x, y);
            var slotClicked = SlotRectangle.Contains(clickedPoint);
            if (slotClicked)
            {
                if (heldItem != null)
                {
                    if (CurrentDimension != null && CurrentDimension.DimensionImplementation.CanAdd(heldItem))
                    {
                        heldItem = CurrentDimension.DimensionImplementation.Add(heldItem);
                        SetUpPositions();
                    }
                    else if (CurrentDimension == null)
                    {
                        Game1.exitActiveMenu();
                        DelayedAction.functionAfterDelay(checkNewItem, 10);
                    }
                }
                else if (CurrentDimension != null && CurrentDimension.DimensionImplementation.Item.Stack > 0 && CurrentDimension.DimensionImplementation.Item.Stack != int.MaxValue && SlotItemBoxRect.Contains(clickedPoint))
                {
                    var item = CurrentDimension.DimensionImplementation.Item;
                    heldItem = item.getOne();
                    heldItem.Stack = item.Stack;
                    item.Stack = 0;
                    SetUpPositions();
                }
            }
            base.receiveLeftClick(x, y, playSound);
        }

        private void checkNewItem()
        {
            var newItem = heldItem;
            heldItem = null;
            var newDimension = data.UndiscoveredDimensions.Where(info => info.DimensionImplementation.CanAdd(newItem)).SingleOrDefault();
            Game1.multipleDialogues(new string[] { "...",
                newDimension != null ? "The Shed glows for a moment.." : "Nothing seems to have happened"});
            Game1.afterDialogues = delegate
            {
                if (newDimension != null)
                {
                    newItem = newDimension.DimensionImplementation.Add(newItem);
                    ChangeDimensionSelection(newDimension);
                    // Some of this heavy dimension logic should probably come out of here, stick more to gui stuff or something. It'll come around. TODO
                    newDimension.DimensionImplementation.InitializeDimensionBuilding();
                }
                if (newItem != null)
                {
                    Game1.playSound("throwDownITem");
                    Game1.createItemDebris(newItem, Game1.player.getStandingPosition(), 2);
                }
            };
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);
            if (scrolling)
            {
                int y2 = scrollBar.bounds.Y;
                scrollBar.bounds.Y = Math.Min(yPositionOnScreen + height - 64 - 12 - scrollBar.bounds.Height, Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + 20));
                float percentage = (float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height;
                currentRow = Math.Min(Math.Max(0, rows - rowsDisplayable), Math.Max(0, (int)((float)(rows - rowsDisplayable) * percentage)));
                setScrollBarToCurrentIndex();
                if (y2 != scrollBar.bounds.Y)
                {
                    Game1.playSound("shiny4");
                }
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);
            scrolling = false;
        }

        private void setScrollBarToCurrentIndex()
        {
            SetUpPositions();
            scrollBar.bounds.Y = scrollBarRunner.Height / Math.Max(1, rows - rowsDisplayable + 1) * currentRow + upArrow.bounds.Bottom + 4;
            if (currentRow == rows - rowsDisplayable)
            {
                scrollBar.bounds.Y = downArrow.bounds.Y - scrollBar.bounds.Height - 4;
            }
        }

        /// <summary>The method invoked when the player right-clicks on the lookup UI.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        /// <param name="playSound">Whether to enable sound.</param>
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            base.receiveRightClick(x, y, playSound);
        }

        /// <summary>The method invoked when the player hovers the cursor over the menu. Makes the dimension buttons pop a bit.</summary>
        /// <param name="x">The X-position of the cursor.</param>
        /// <param name="y">The Y-position of the cursor.</param>
        public override void performHoverAction(int x, int y)
        {
            DimensionButtons.ForEach(button => button.tryHover(x, y));
            HintButtons.ForEach(button => button.tryHover(x, y));

            scrollBar.tryHover(x, y);
            if (scrolling)
            {
                return;
            }

            base.performHoverAction(x, y);
        }

        private Rectangle SlotRectangle { get => new Rectangle(SlotX, SlotY, 256 + 128 - 64, 256 + 128 - 64); }
        private int FirstVertXOffset { get => 64 * 3 + 3 * IClickableMenu.spaceToClearSideBorder; }
        private int SlotX { get => xPositionOnScreen + FirstVertXOffset + 32; }
        private int SlotY { get => yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 64 + 32; }
        private Rectangle SlotItemBoxRect { get => new Rectangle(xPositionOnScreen + (int)(6 * 64), yPositionOnScreen + 6 * 64 - 16, 2 * 64, 2 * 64); }
        private Vector2 SlotItemBox { get => new Vector2(SlotItemBoxRect.X, SlotItemBoxRect.Y); }
        public override void draw(SpriteBatch b)
        {
            drawInventoryAndUpperSections(b);

            // Dimension buttons
            foreach (var dimension in DimensionButtons)
            {
                dimension.draw(b);
            }
            var hintOffset = new Vector2(Game1.tileSize / 4, 0);
            foreach (var dimension in HintButtons)
            {
                var bounds = dimension.getVector2();
                dimension.setPosition(bounds + hintOffset);
                dimension.draw(b);
                dimension.setPosition(bounds);
            }

            // scroll stuff
            if (rows > rowsDisplayable)
            {
                upArrow.draw(b);
                downArrow.draw(b);
                drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollBarRunner.X, scrollBarRunner.Y, scrollBarRunner.Width, scrollBarRunner.Height, Color.White, 4f);
                scrollBar.draw(b);
            }

            // box for item
            b.Draw(Game1.menuTexture, SlotItemBox, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10), Color.White, 0f, new Vector2(8f, 8f), 2f, SpriteEffects.None, 0.5f);
            if (CurrentDimension != null && CurrentDimension.DimensionImplementation.Item.Stack > 0 && CurrentDimension.DimensionImplementation.Item.Stack != int.MaxValue)
            {
                fixedFor2xDrawInMenu(CurrentDimension.DimensionImplementation.Item as SDVObject, b, SlotItemBox + new Vector2(48, 48), 2f, 1f, 0.9f, StackDrawType.Draw, Color.White, drawShadow: true);
            }

            var title = "Big Shed"; // TODO externalize
            var description = "Just a boring old shed.";
            var textShadowColor = Game1.textShadowColor;
            if (CurrentDimension != null)
            {
                title = CurrentDimension.DimensionImplementation.HintAllowed() && CurrentDimension.DimensionImplementation.Item.Stack == int.MaxValue ? "???" : CurrentDimension.DisplayName;
                description = CurrentDimension.DimensionImplementation.CurrentDescription();
                textShadowColor = CurrentDimension.TextShadowColor;
            }
            // Title like item hover text + centered
            var titleSize = Game1.dialogueFont.MeasureString(title);
            drawBoldTitleText(b, Game1.dialogueFont, title, new Vector2(SlotX + Math.Max(spaceToClearSideBorder, (SlotRectangle.Width - titleSize.X) / 2), yPositionOnScreen + spaceToClearTopBorder + 8));
            drawDescriptionText(b, Game1.smallFont, Game1.parseText(description, Game1.smallFont, width - (SlotX - xPositionOnScreen) - 2 * spaceToClearSideBorder - 32), new Vector2(SlotX + spaceToClearSideBorder, yPositionOnScreen + spaceToClearTopBorder + 32 + 16), textColor: null, textShadowColor: textShadowColor, 1f);

            if (hoverText != null && (hoveredItem == null || hoveredItem == null))
            {
                if (hoverAmount > 0)
                {
                    drawToolTip(b, hoverText, "", null, heldItem: true, -1, 0, -1, -1, null, hoverAmount);
                }
                else
                {
                    drawHoverText(b, hoverText, Game1.smallFont);
                }
            }
            if (hoveredItem != null)
            {
                drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem != null);
            }
            if (heldItem != null)
            {
                heldItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
            }
            Game1.mouseCursorTransparency = 1f;
            drawMouse(b);
        }

        /// <summary>
        /// A customized version of the equivalent MenuWithInventory.draw(b,red,green,blue) that draws 
        /// the inventory and some frames above it to show the item slot, dimension info, etc.
        /// </summary>
        private void drawInventoryAndUpperSections(SpriteBatch b, int red = -1, int green = -1, int blue = -1)
        {
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true, null, objectDialogueWithPortrait: false, ignoreTitleSafe: false, red, green, blue);

            drawHorizontalPartition(b, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 256 + 128, small: false, red, green, blue);
                
            drawVerticalUpperIntersectingPartition(b, xPositionOnScreen + FirstVertXOffset, 328 + 128, red, green, blue);
            drawHorizontalRightSideIntersectingPartition(b, xPositionOnScreen + FirstVertXOffset + 8, yPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 64, width - FirstVertXOffset - 8, red, green, blue);
            drawVerticalUpperIntersectingPartialPartition(b, xPositionOnScreen + FirstVertXOffset + 256 + 64 + 16, yPositionOnScreen + 128 + 8, 328 - 8, red, green, blue);

            if (okButton != null)
            {
                okButton.draw(b);
            }
            inventory.draw(b, red, green, blue);
        }

        /// <summary>
        /// A customized version of MenuWithInventory.drawHorizontalPartition(...) which uses no ornamentation on the crossed beams,
        /// similar to the MenuWithInventory.drawVerticalUpperIntersectingPartition(...)'s cross sprites. Also allows customizing the
        /// width without modifying the core width properties.
        /// </summary>
        protected void drawHorizontalRightSideIntersectingPartition(SpriteBatch b, int xPosition, int yPosition, int width, int red = -1, int green = -1, int blue = -1)
        {
            Color tint = ((red == -1) ? Color.White : new Color(red, green, blue));
            Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
            Texture2D customTexture = ((red == -1) ? ModEntry.myMenuTexture : ModEntry.myMenuTextureUncolored);
            b.Draw(customTexture, new Vector2(xPosition, yPosition), Game1.getSourceRectForStandardTileSheet(customTexture, 2), tint);
            b.Draw(texture, new Rectangle(xPosition + 64, yPosition, width - 128, 64), Game1.getSourceRectForStandardTileSheet(texture, 6), tint);
            b.Draw(customTexture, new Vector2(xPosition + width - 64, yPosition), Game1.getSourceRectForStandardTileSheet(customTexture, 1), tint);
        }

        /// <summary>
        /// A customized version of MenuWithInventory.drawVerticalUpperIntersectingPartition(...) where the upper cross sprite
        /// has the coloring of the internal frame pieces rather than the outer. Also allows customizing height and positioninng 
        /// without altering the core height and yPositionOnScreen.
        /// </summary>
        protected void drawVerticalUpperIntersectingPartialPartition(SpriteBatch b, int xPosition, int yPosition, int partitionHeight, int red = -1, int green = -1, int blue = -1)
        {
            Color tint = ((red == -1) ? Color.White : new Color(red, green, blue));
            Texture2D texture = ((red == -1) ? Game1.menuTexture : Game1.uncoloredMenuTexture);
            b.Draw(texture, new Vector2(xPosition, yPosition + 64), Game1.getSourceRectForStandardTileSheet(texture, 59), tint);
            b.Draw(texture, new Rectangle(xPosition, yPosition + 128, 64, partitionHeight - 32), Game1.getSourceRectForStandardTileSheet(texture, 63), tint);
            b.Draw(texture, new Vector2(xPosition, yPosition + partitionHeight + 64), Game1.getSourceRectForStandardTileSheet(texture, 39), tint);
        }

        /// <summary>
        /// Pulled from IClickableMenu.drawHoverText(...):1156. Creates a light shadow down and right from the title.
        /// </summary>
        protected void drawBoldTitleText(SpriteBatch b, SpriteFont font, string boldTitleText, Vector2 position)
        {
            b.DrawString(font, boldTitleText, position + new Vector2(2f, 2f), Game1.textShadowColor);
            b.DrawString(font, boldTitleText, position + new Vector2(0f, 2f), Game1.textShadowColor);
            b.DrawString(font, boldTitleText, position, Game1.textColor);
        }

        protected void drawDescriptionText(SpriteBatch b, SpriteFont font, string text, Vector2 position, Color? textColor = null, Color? textShadowColor = null, float alpha = 1f)
        {
            textColor ??= Game1.textColor;
            textShadowColor ??= Game1.textShadowColor;
            b.DrawString(font, text, position + new Vector2(2f, 2f), (Color)textShadowColor * alpha);
            b.DrawString(font, text, position + new Vector2(0f, 2f), (Color)textShadowColor * alpha);
            b.DrawString(font, text, position + new Vector2(2f, 0f), (Color)textShadowColor * alpha);
            b.DrawString(font, text, position, (Color)textColor * 0.9f * alpha);
        }
        protected void fixedFor2xDrawInMenu(SDVObject item, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            bool shouldDrawStackNumber = ((drawStackNumber == StackDrawType.Draw && item.maximumStackSize() > 1 && item.Stack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scaleSize > 0.3 && item.Stack != int.MaxValue;
            if ((int)item.ParentSheetIndex != 590 && drawShadow)
            {
                spriteBatch.Draw(Game1.shadowTexture, location + new Vector2(32f, 48f), Game1.shadowTexture.Bounds, color * 0.5f, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y) * scaleSize, 3f, SpriteEffects.None, layerDepth - 0.0001f);
            }
            spriteBatch.Draw(Game1.objectSpriteSheet, location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)), Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, item.ParentSheetIndex, 16, 16), color * transparency, 0f, new Vector2(8f, 8f) * scaleSize, 4f * scaleSize, SpriteEffects.None, layerDepth);
            if (shouldDrawStackNumber)
            {
                SDVUtility.drawTinyDigits(item.Stack, spriteBatch, location + new Vector2((float)(64 - SDVUtility.getWidthOfTinyDigitString(item.Stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 1f), 3f * scaleSize, 1f, color);
            }
            if (drawStackNumber != 0 && (int)item.Quality > 0)
            {
                Rectangle quality_rect = (((int)item.Quality < 4) ? new Rectangle(338 + ((int)item.Quality - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8));
                Texture2D quality_sheet = Game1.mouseCursors;
                float yOffset = (((int)item.Quality < 4) ? 0f : (((float)Math.Cos((double)Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1f) * 0.05f));
                spriteBatch.Draw(quality_sheet, location + new Vector2(-44f, -12f) * scaleSize + new Vector2(56f, 64f + yOffset), quality_rect, color * transparency, 0f, new Vector2(4f, 4f), 3f * scaleSize * (1f + yOffset), SpriteEffects.None, layerDepth);
            }
        }
    }
}
