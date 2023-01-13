using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BeatSaverDownloader.Misc
{
    internal class Sprites
    {
        public static Sprite AddToFavorites;
        public static Sprite RemoveFromFavorites;
        public static Sprite StarFull;
        public static Sprite StarEmpty;
        public static Sprite DoubleArrow;

        public static Sprite ReviewIcon;

        //https://www.flaticon.com/free-icon/thumbs-up_70420
        public static Sprite ThumbUp;

        //https://www.flaticon.com/free-icon/dislike-thumb_70485
        public static Sprite ThumbDown;

        //https://www.flaticon.com/free-icon/playlist_727239
        public static Sprite PlaylistIcon;

        //https://www.flaticon.com/free-icon/musical-note_727218
        public static Sprite SongIcon;

        //https://www.flaticon.com/free-icon/download_724933
        public static Sprite DownloadIcon;

        //https://www.flaticon.com/free-icon/media-play-symbol_31128
        public static Sprite PlayIcon;

        //https://game-icons.net/1x1/delapouite/perspective-dice-six-faces-three.html
        public static Sprite RandomIcon;

        //https://www.flaticon.com/free-icon/waste-bin_70388
        public static Sprite DeleteIcon;

        public static Sprite BeatSaverIcon;
        public static Sprite ScoreSaberIcon;
        //by elliotttate#9942
        public static Sprite BeastSaberLogo;
        public static void ConvertToSprites()
        {
            AddToFavorites = LoadSpriteFromResources("BeatSaverDownloader.Assets.AddToFavorites.png");
            RemoveFromFavorites = LoadSpriteFromResources("BeatSaverDownloader.Assets.RemoveFromFavorites.png");
            StarFull = LoadSpriteFromResources("BeatSaverDownloader.Assets.StarFull.png");
            StarEmpty = LoadSpriteFromResources("BeatSaverDownloader.Assets.StarEmpty.png");
            BeastSaberLogo = LoadSpriteFromResources("BeatSaverDownloader.Assets.BeastSaberLogo.png");
            ReviewIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.ReviewIcon.png");
            ThumbUp = LoadSpriteFromResources("BeatSaverDownloader.Assets.ThumbUp.png");
            ThumbDown = LoadSpriteFromResources("BeatSaverDownloader.Assets.ThumbDown.png");
            PlaylistIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.PlaylistIcon.png");
            SongIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.SongIcon.png");
            DownloadIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.DownloadIcon.png");
            PlayIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.PlayIcon.png");
            DoubleArrow = LoadSpriteFromResources("BeatSaverDownloader.Assets.DoubleArrow.png");
            RandomIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.RandomIcon.png");
            DeleteIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.DeleteIcon.png");
            BeatSaverIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.BeatSaver.png");
            ScoreSaberIcon = LoadSpriteFromResources("BeatSaverDownloader.Assets.ScoreSaber.png");
        }

        public static string SpriteToBase64(Sprite input)
        {
            return Convert.ToBase64String(input.texture.EncodeToPNG());
        }

        public static Sprite Base64ToSprite(string input)
        {
            string base64 = input;
            if (input.Contains(","))
            {
                base64 = input.Substring(input.IndexOf(','));
            }
            Texture2D tex = Base64ToTexture2D(base64);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), (Vector2.one / 2f));
        }

        private static Texture2D Base64ToTexture2D(string encodedData)
        {
            var imageData = Convert.FromBase64String(encodedData);
            var tex2D = new Texture2D(2, 2);
            return tex2D.LoadImage(imageData) ? tex2D : null;
        }

        // Image helpers

        private static Texture2D LoadTextureRaw(byte[] file)
        {
            if (!file.Any()) return null;

            var tex2D = new Texture2D(2, 2);
            return tex2D.LoadImage(file) ? tex2D : null;
        }

        private static Texture2D LoadTextureFromFile(string filePath)
        {
            return File.Exists(filePath) ?
                LoadTextureRaw(File.ReadAllBytes(filePath)) :
                null;
        }

        public static Texture2D LoadTextureFromResources(string resourcePath)
        {
            return LoadTextureRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath));
        }

        public static Sprite LoadSpriteRaw(byte[] image, float pixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureRaw(image), pixelsPerUnit);
        }

        public static Sprite LoadSpriteFromTexture(Texture2D spriteTexture, float pixelsPerUnit = 100.0f)
        {
            return spriteTexture ?
                Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit) :
                null;
        }

        public static Sprite LoadSpriteFromFile(string filePath, float pixelsPerUnit = 100.0f)
        {
            return LoadSpriteFromTexture(LoadTextureFromFile(filePath), pixelsPerUnit);
        }

        private static Sprite LoadSpriteFromResources(string resourcePath, float pixelsPerUnit = 100.0f)
        {
            return LoadSpriteRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath), pixelsPerUnit);
        }

        private static byte[] GetResource(Assembly asm, string resourceName)
        {
            var stream = asm.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, (int) stream.Length);
                return data;
            }

            return null;
        }
    }
}