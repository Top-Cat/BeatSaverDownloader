using HMUI;
using SongCore.OverrideClasses;
using System.Collections.Generic;
using System.Linq;
using BS_Utils.Utilities;
using IPA.Utilities;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    public static class CustomHelpers
    {
        public static void RefreshTable(this TableView tableView, bool callbackTable = true)
        {
            HashSet<int> rows = new HashSet<int>(tableView.GetField<HashSet<int>, TableView>("_selectedCellIdxs"));

            tableView.ReloadData();
            if (rows.Count > 0)
                tableView.SelectCellWithIdx(rows.First(), callbackTable);
        }

        //bananbread levelpacks

        public static SongCoreCustomBeatmapLevelPack GetLevelPackWithLevels(CustomPreviewBeatmapLevel[] levels, string packName = null, Sprite packCover = null, string packID = null)
        {
            SongCoreCustomLevelCollection levelCollection = new SongCoreCustomLevelCollection(levels.ToArray());

            SongCoreCustomBeatmapLevelPack pack = new SongCoreCustomBeatmapLevelPack(string.IsNullOrEmpty(packID) ? "" : packID,
                string.IsNullOrEmpty(packName) ? "Custom Songs" : packName, packCover ?? Sprites.BeastSaberLogo, levelCollection);
            //       pack.SetPrivateField("_packName", string.IsNullOrEmpty(packName) ? "Custom Songs" : packName);
            //       pack.SetPrivateField("_coverImage", packCover ?? Sprites.BeastSaberLogo);
            //       pack.SetPrivateField("_packID", string.IsNullOrEmpty(packID) ? "" : packID);
            //       pack.SetPrivateField("_isPackAlwaysOwned", true);
            return pack;
        }

        private static char[] hexChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        public static string CheckHex(string input)
        {
            input = input.ToUpper();
            if (input.All(x => hexChars.Contains(x)))
            {
                return input;
            }
            else
            {
                return "";
            }
        }
    }
}