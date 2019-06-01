using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vis.TextureAutoCropper
{
    internal class TexturesPostprocessors : AssetPostprocessor
    {
        internal const string AssetsFolderName = "Assets";

        internal static List<string> CropedPaths = new List<string>();

        private void OnPreprocessTexture()
        {
            var settings = Settings.FindInstance();
            if (settings == null)
                return;
            if (!settings.CropAutomatically)
                return;

            if (CropedPaths.Contains(assetPath))
            {
                //_cropedPaths.Remove(assetPath);
                return;
            }

            var applicationPath = Application.dataPath;
            var absolutePath = Path.Combine(applicationPath.Substring(0, applicationPath.Length - AssetsFolderName.Length), assetPath);
            if (!Path.HasExtension(absolutePath) || Path.GetExtension(absolutePath) != ".png")
                return;

            //Debug.Log($"absolutePath = {absolutePath}");
            var bytes = File.ReadAllBytes(absolutePath);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
            texture.LoadImage(bytes);

            Crop(texture, absolutePath, settings);
            Object.DestroyImmediate(texture);
        }

        internal static void Crop(Texture2D texture, string saveToAbsolutePath, Settings settings)
        {
            var top = 0;
            var bottom = 0;
            var left = 0;
            var right = 0;

            for (int y = 0; y < texture.height; y++)
            {
                top = y;
                for (int x = 0; x < texture.width; x++)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0f)
                        goto checkLeft;
                }
            }

        checkLeft:

            for (int x = 0; x < texture.width; x++)
            {
                left = x;
                for (int y = top; y < texture.height; y++)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0f)
                        goto checkBottom;
                }
            }

        checkBottom:

            for (int y = texture.height - 1; y > top; y--)
            {
                bottom = y;
                for (int x = left; x < texture.width; x++)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0f)
                        goto checkRight;
                }
            }

        checkRight:

            for (int x = texture.width - 1; x > left; x--)
            {
                right = x;
                for (int y = top; y < bottom; y++)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a > 0f)
                        goto crop;
                }
            }

        crop:

            var width = right - left;
            var heigth = bottom - top;
            var pixels = texture.GetPixels(left, top, width, heigth);

            var croppedTexture = new Texture2D(width, heigth, TextureFormat.ARGB32, false, false);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            var bytes = croppedTexture.EncodeToPNG();
            if (!settings.RewriteOriginal)
            {
                var fileName = Path.GetFileNameWithoutExtension(saveToAbsolutePath);
                var extension = Path.GetExtension(saveToAbsolutePath);

                var similarNamesCounter = 0;
                var originalAaveToAbsolutePath = saveToAbsolutePath;
                while (File.Exists(saveToAbsolutePath))
                {
                    var newAbsolutePath = Path.Combine(originalAaveToAbsolutePath.Substring(0, originalAaveToAbsolutePath.Length - fileName.Length - extension.Length), $"{fileName}{settings.CroppedFileNamingSchema}{(similarNamesCounter > 0 ? $" {similarNamesCounter}" : string.Empty)}{extension}");
                    saveToAbsolutePath = newAbsolutePath;
                    similarNamesCounter++;
                }
            }
            File.WriteAllBytes(saveToAbsolutePath, bytes);
            Object.DestroyImmediate(croppedTexture);

            var applicationPath = Application.dataPath;
            var relativePath = saveToAbsolutePath.Substring(applicationPath.Length - AssetsFolderName.Length);

            if (!CropedPaths.Contains(relativePath))
                CropedPaths.Add(relativePath);
            AssetDatabase.ImportAsset(relativePath);
        }
    }
}
