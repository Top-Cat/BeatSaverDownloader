using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BeatSaverDownloader.Bookmarks
{
    public static class Crypto
    {
        private static readonly byte[] Iv = Convert.FromBase64String("%SECRET_IV%");

        internal static string ExtractSecrets(Texture2D tex)
        {
            var pixels = tex.GetPixels(7, 210, 130, 10).Select(x => (byte) (x.b * 255)).ToArray();
            var length = pixels[0] | (pixels[1] << 8);

            var key = new byte[32];
            Buffer.BlockCopy(pixels, 2, key, 0, 32);

            var enc = new byte[length - 32];
            Buffer.BlockCopy(pixels, 34, enc, 0, length - 32);

            return Decrypt(enc, key);
        }

        private static string Decrypt(byte[] enc, byte[] key)
        {
            using (var dsp = new RijndaelManaged())
            {
                using (var decryptor = dsp.CreateDecryptor(key, Iv))
                {
                    using (var memoryStream = new MemoryStream(enc))
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            var plainTextBytes = new byte[enc.Length];
                            var _ = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                            return Encoding.UTF8.GetString(plainTextBytes).Trim('\0');
                        }
                    }
                }
            }
        }
    }
}