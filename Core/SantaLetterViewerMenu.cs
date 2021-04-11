using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalDoom.StardewValley.InterdimensionalShed
{
    class SantaLetterViewerMenu : LetterViewerMenu
    {
        private static readonly Texture2D santaLetterTexture = null;
        private readonly Action afterClose;

        public SantaLetterViewerMenu(string mailText, string title, Action afterClose) : base(mail: mailText, mailTitle: title)
        {
            //letterTexture = santaLetterTexture;
            this.afterClose = afterClose;
        }

        public override int getTextColor()
        {
            return 0; // white? not sure yet
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();
            afterClose.Invoke();
        }
    }
}
