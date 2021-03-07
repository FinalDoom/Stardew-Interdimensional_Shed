using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterdimensionalShed
{
    partial class ModEntry
    {
        private const string ModId = "8181";

        private const int ItemId_QiGem = 858;
        private const int ItemId_IridiumBar = 337;
        private const int ItemId_QiFruit = 889;
        private const int ItemId_RadioactiveBar = 910;
        private const int ItemId_VoidEssence = 769;

        public const string SaveKey = "InterdimensionalShed";
        public const string DimensionItemsKey = "InterdimensionalShedItems";
        private const string BlueprintId = "Interdimensional Shed";
        private const string ShedPurchaseMailId = "InterdimensionalShedPurchased";

        public const string ShedDimensionModDataKey = "InterdimensionalShedLinkedDimensionName";
        public static readonly Dictionary<int, string> ShedDimensionKeys = new Dictionary<int, string>
        {
            [769] = "InterdimensionalShedVoidDimension",
            [305] = "InterdimensionalShedEggDimension",
            [422] = "InterdimensionalShedMushroomDimension",
            [768] = "InterdimensionalShedSunDimension",
            [155] = "InterdimensionalShedStrangeVegetableDimension",
            [373] = "InterdimensionalShedGourdDimension",
            [279] = "InterdimensionalShedCandyDimension",
            [279] = "InterdimensionalShedMarbleDimension",
            [74] = "InterdimensionalShedRainbowDimension",
            [466] = "InterdimensionalShedFuzzyDimension",
            [80] = "InterdimensionalShedGlassDimension",
            [216] = "InterdimensionalShedBreadDimension",
            [418] = "InterdimensionalShedFlowerDimension",
            [414] = "InterdimensionalShedWinterDimension",
            [724] = "InterdimensionalShedStickyDimension",
            [904] = "InterdimensionalShedBananaDimension",
            // dimension with ponds and or fish
            // Gem Dimensions
            [60] = "InterdimensionalShedDimension1",
            [62] = "InterdimensionalShedDimension2",
            [64] = "InterdimensionalShedDimension3",
            [66] = "InterdimensionalShedDimension4",
            [68] = "InterdimensionalShedDimension5",
            [70] = "InterdimensionalShedDimension6",
            [72] = "InterdimensionalShedDimension7",
            // Consider geodes, maybe the most valuable or something
        };

        private static readonly Dictionary<string, string> FarmEventsData = new Dictionary<string, string>
        {
            [ModId + "0001/v Abigail/e " + ModId + "0000"] =
                // Start at the shed entrance and have Abigail talk to you (currently house, make it dynamic based on shed)
                "continue/64 15/farmer 64 16 2 Abigail 64 18 0/pause 1500" +
                "/speak Abigail \"Hi @. I was passing by and noticed the black mist seeping from this shed." +
                "#$b" +
                "#I hope you don't mind, but I peeked in the window while you were in there... There's some weird stuff going on in there.$u" +
                "#$b" +
                "#$q " + ModId + "0002 null#Do you have any idea what's going on in the shed?" +
                "#$r " + ModId + "0002 0 " + ModId + "Event_InterdimensionalShed_1#No clue." +
                "#$r " + ModId + "0002 0 " + ModId + "Event_InterdimensionalShed_2#There was a page in the Wizard's book about making my shed bigger. It doesn't seem any bigger to me..." +
                "#$r " + ModId + "0002 0 " + ModId + "Event_InterdimensionalShed_3#All my items that were in the shed are missing!\"" +
                "/switchEvent " + ModId + "0005_Enter_Shed",
            // Enter Shed
            [ModId + "0005_Enter_Shed"] =
                "move farmer 1 0 3/move Abigail 0 -1 0/playSound doorOpen/pause 500/warp Abigail -100 -100/move farmer -1 0 0/warp farmer -100 -101/playSound doorClose/pause 500/ChangeLocation Shed" +
            // Search shed
                "/playMusic spirits_eve/viewport 9 9/warp farmer 9 16/faceDirection farmer 0/warp Abigail 9 15/faceDirection Abigail 0/pause 1000/faceDirection Abigail 1/pause 500/faceDirection Abigail 3/emote Abigail 8/pause 800/faceDirection Abigail 2" +
                "/speak Abigail \"Woah... Well, let's see what we can find. I'll look on the right, you look on the left.$u\"" +
                "/move Abigail 0 -3 1/move farmer 0 -1 0/move Abigail 1 0 1/move farmer -3 0 3" +
                // animate farmer too looking in bottom left hole
                // This doesn't work, you have to use animation I guess. Figure that out, numbers are for the id on spritesheet
                //"/changeSprite Abigail 51" +
                "/pause 1000/emote Abigail 40/move farmer 0 -6 3/pause 1000/move Abigail 0 -1 1/move Abigail 1 0 0/move Abigail 0 -1 1/move Abigail 1 0 0" +
                "/move farmer 0 -1 0/move Abigail 0 -5 1/pause 500/facedirection Abigail 3/emote Abigail 40/move farmer -2 0 3/pause 800/move farmer 0 -4 3/move Abigail 2 0 1/move Abigail 0 -1 0/move Abigail 3 0 0" +
                "/emote Abigail 8/pause 2000/move Abigail 0 3 2/move farmer 3 0 2/move Abigail -2 0 2" +
                //"/changeSprite 25" +
                "/pause 800/move Abigail 1 0 1/faceDirection Abigail 2/faceDirection farmer 1/pause 1500/emote Abigail 16/emote Abigail 8/pause 800" +
                "/move Abigail 0 2 2/emote farmer 16/jump farmer/pause 500/emote Abigail 32/pause 800/move Abigail 0 2 2/move Abigail -2 0 3/move Abigail 0 3 1/pause 500" +
                //"/changeSprite Abigail 51" +
                "/pause 1500/move Abigail -3 0 3/pause 500/faceDirection Abigail 0/pause 500" +
                "/move Abigail 0 1 2/pause 1000/emote Abigail 40/pause 1500/emote Abigail 8/pause 1000" +
                //"/playSound secret1/playSound woodyHit/playSound button1/playSound bigSelect/playSound bigDeSelect/playSound coin/playSound pickUpItem/playsound shiny4/playSound smallSelect/playSound thudStep/playSound money/playSound 00000060/playSound 00000061" +
                "/emote Abigail 16/speak Abigail \"Huzzah blah blah blah whee yay let's go outside harumph\"/end",
            // sound secret1
            // sound woodyHit
            // sound stumpCrack
            // sound button1
            // bigSelect bigDeSelect coin pickUpItem shiny4 smallSelect thudStep fishBite bob money Ship 00000060 and 00000061
        };
        private static readonly Dictionary<string, string> InterdimensionalShedEventsData = new Dictionary<string, string>
        {
            // Initial entry to shed
            [ModId + "0000/Hn " + ShedPurchaseMailId] =
                "continue/9 9/farmer 9 16 0/skippable/pause 800/emote farmer 16/jump farmer/pause 600/emote farmer 8/pause 800" +
                "/move farmer 0 -5 0/pause 250/faceDirection farmer 3/faceDirection farmer 2/faceDirection farmer 1/playSound dwop/emote farmer 8/pause 500" +
                "/move farmer 0 -1 3/move farmer -6 0 3/pause 250/faceDirection farmer 2/pause 200/emote farmer 40/pause 500" +
                "/speed farmer 4/move farmer 0 -4 0/pause 250/faceDirection farmer 3/emote farmer 8/pause 400" +
                "/speed farmer 6/move farmer 1 0 1/move farmer 0 -1 0/move farmer 1 0 1/move farmer 0 -1 0/move farmer 10 0 1/pause 250/faceDirection farmer 2/emote farmer 12/pause 300" +
                "/move farmer -3 0 2/move farmer 0 6 3/speed farmer 4/move farmer -3 0 2/speed farmer 1/move farmer 0 6 0/emote farmer 28/pause 1000/end",
            //[ModId + "0006/v Abigail/e " + ModId + "0005_Enter_Shed"] = "",
        };
        private static readonly Dictionary<string, string> AbigailDialogueCharacters = new Dictionary<string, string>
        {
            [ModId + "Event_InterdimensionalShed_1"] = "Hm... Let's explore together and see see if we can figure anything out.$n",
            [ModId + "Event_InterdimensionalShed_2"] = "Well, it may not be bigger, but it certainly is weirder and oozing black mist... Let's take a look inside.$6",
            [ModId + "Event_InterdimensionalShed_3"] = "Oh no!$7#$b#I bet we can figure out where they went if we look together... Come on!$h",
        };
    }
}
