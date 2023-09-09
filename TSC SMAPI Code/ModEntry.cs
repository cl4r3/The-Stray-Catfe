using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.TerrainFeatures;
using StardewModdingAPI.Utilities;
using StardewValley;
using SpaceCore.Events;

namespace StrayCatfe
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor { get; set; }
        internal new static IModHelper Helper { get; set; }

        internal static class Log
        {
            internal static void Error(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Error);
            internal static void Alert(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Alert);
            internal static void Warn(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Warn);
            internal static void Info(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Info);
            internal static void Debug(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Debug);
            internal static void Trace(string msg) => ModEntry.ModMonitor.Log(msg, StardewModdingAPI.LogLevel.Trace);
            internal static void Verbose(string msg) => ModEntry.ModMonitor.VerboseLog(msg);

        }

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            Helper = helper;

            helper.Events.GameLoop.SaveLoaded += SetGreenhouse;
            helper.Events.GameLoop.DayStarted += CheckTheoCrops;
            SpaceEvents.BeforeGiftGiven += TheoReceiveSeeds;
        }

        private static void SetGreenhouse(object sender, SaveLoadedEventArgs e)
        {
            GameLocation location = Game1.getLocationFromName("Custom_StrayCatfe_TheoGreenhouse");
            if (location == null)
            {
                Log.Error("TSC: Theo's greenhouse could not be found!");
            }
            location.isGreenhouse.Value = true;
            Log.Trace("TSC: Theo's greenhouse set to greenhouse");
            if (!location.modData.ContainsKey("TSC.seeds"))
                location.modData["TSC.seeds"] = "N/A";
            Log.Trace("TSC: Theo's greenhouse modData is " + location.modData["TSC.seeds"]);
        }

        private static void TheoReceiveSeeds(object sender, EventArgsBeforeReceiveObject e)
        {
            //if (!Game1.player.eventsSeen.Contains(779)) return;
            Log.Verbose("TSC: Starting BeforeGiftGiven event");
            NPC giftee = e.Npc;
            if (giftee.Name != "Theo") return;
            StardewValley.Object gift = e.Gift;
            if (gift.Category != -74) return;
            Log.Verbose("TSC: Seed item given");
            e.Cancel = true;
            Dictionary<int, string> cropData = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");
            var yield_id = int.Parse(cropData[gift.ParentSheetIndex].Split('/')[3]);
            Log.Verbose("TSC: Seed yield ID is " + yield_id);
            Dictionary<int, string> objectInfo = Game1.content.Load<Dictionary<int, string>>("Data\\ObjectInformation");
            if (objectInfo[yield_id].Split('/')[3] != "Basic -80")
            {
                Log.Verbose("TSC: " + yield_id + " is not flower");
                giftee.CurrentDialogue.Clear();
                giftee.CurrentDialogue.Push(new Dialogue(Helper.Translation.Get("Theo.WrongSeeds"), giftee));
                Game1.drawDialogue(giftee);
                return;
            }
            Log.Verbose("TSC: Yield object name is flower!");
            GameLocation greenhouse = Game1.getLocationFromName("Custom_StrayCatfe_TheoGreenhouse");
            greenhouse.modData["TSC.seeds"] = gift.ParentSheetIndex.ToString();
            Log.Trace($"TSC: Theo will now grow {gift.Name}, item ID {gift.ParentSheetIndex}");
            Game1.player.reduceActiveItemByOne();
            giftee.CurrentDialogue.Clear();
            giftee.CurrentDialogue.Push(new Dialogue(Helper.Translation.Get("Theo.ChangeSeeds").ToString().Replace("{name}", gift.DisplayName), giftee));
            Game1.drawDialogue(giftee);
        }

        private void CheckTheoCrops(object sender, DayStartedEventArgs e)
        {
            Log.Trace($"TSC: Checking Theo crops");
            GameLocation greenhouse = Game1.getLocationFromName("Custom_StrayCatfe_TheoGreenhouse");
            Vector2[] tiles = {
                new Vector2(10,8), new Vector2(11,8), new Vector2(12,8),
                new Vector2(10,9), new Vector2(11,9), new Vector2(12,9),
                new Vector2(10,10), new Vector2(11,10), new Vector2(12,10)
            };
            var features = greenhouse.terrainFeatures;
            foreach(var tile in tiles)
            {
                if (!features.ContainsKey(tile))
                {
                    greenhouse.makeHoeDirt(tile);
                }
            }
            foreach (var pair in features.Pairs)
            {
                Vector2 tile = pair.Value.currentTileLocation;
                HoeDirt dirt = pair.Value as HoeDirt;
                dirt.state.Value = 1;
                if (dirt.crop == null && !greenhouse.isObjectAtTile((int)tile.X, (int)tile.Y))
                {
                    dirt.plant(int.Parse(greenhouse.modData["TSC.seeds"]), (int)tile.X, (int)tile.Y, Game1.player, false, greenhouse);
                    Log.Trace($"TSC: Planting seed ID {greenhouse.modData["TSC.seeds"]} at {tile.X}, {tile.Y}");
                }
            }
            Log.Trace($"TSC: Done checking Theo crops");
        }
    }
}
