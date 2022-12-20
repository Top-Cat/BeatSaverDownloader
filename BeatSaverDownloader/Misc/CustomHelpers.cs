using HMUI;
using SongCore.OverrideClasses;
using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    public static class CustomHelpers
    {
        public static void RefreshTable(this TableView tableView, bool callbackTable = true)
        {
            var rows = new HashSet<int>(tableView.GetField<HashSet<int>, TableView>("_selectedCellIdxs"));

            tableView.ReloadData();
            if (rows.Count > 0)
                tableView.SelectCellWithIdx(rows.First(), callbackTable);
        }

        public static SongCoreCustomBeatmapLevelPack GetLevelPackWithLevels(IEnumerable<CustomPreviewBeatmapLevel> levels, string packName = null, Sprite packCover = null, string packID = null)
        {
            var levelCollection = new SongCoreCustomLevelCollection(levels.ToArray());

            var pack = new SongCoreCustomBeatmapLevelPack(string.IsNullOrEmpty(packID) ? "" : packID,
                string.IsNullOrEmpty(packName) ? "Custom Songs" : packName, packCover ? packCover : Sprites.BeastSaberLogo, levelCollection);
            return pack;
        }

        private static readonly char[] HexChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        public static string CheckHex(string input)
        {
            input = input.ToUpper();
            return input.All(x => HexChars.Contains(x)) ? input : "";
        }
    }
}