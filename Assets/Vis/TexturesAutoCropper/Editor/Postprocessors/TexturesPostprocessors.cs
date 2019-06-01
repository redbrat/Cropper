using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vis.TextureAutoCropper
{
    public class TexturesPostprocessors : AssetPostprocessor
    {
        private const string _assetsFolderName = "Assets";

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
            
            var absolutePath = GetAbsolutePathByRelative(assetPath);
            if (!Path.HasExtension(absolutePath) || !ExtensionFits(absolutePath, FileFormat.Png))
                return;

            //Debug.Log($"absolutePath = {absolutePath}");
            var bytes = File.ReadAllBytes(absolutePath);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
            texture.LoadImage(bytes);

            //Debug.Log($"auto texture resolution = {texture.width}x{texture.height}");
            Crop(texture, absolutePath, settings);
            Object.DestroyImmediate(texture);
            //var importer = (TextureImporter)assetImporter;
            //importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings() { maxTextureSize = 2048 });
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

            top = Mathf.Clamp(top - settings.Padding.y, 0, texture.height);

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

            left = Mathf.Clamp(left - settings.Padding.x, 0, texture.width);

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

            bottom = Mathf.Clamp(bottom + 1 + settings.Padding.height, 0, texture.height);

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

            right = Mathf.Clamp(right + 1 + settings.Padding.width, 0, texture.width);

            var width = right - left;
            var heigth = bottom - top;
            //Debug.Log($"settings.Padding = {settings.Padding}");
            //Debug.Log($"width = {width}, height = {heigth}");
            var pixels = texture.GetPixels(left, top, width, heigth);

            var croppedTexture = new Texture2D(width, heigth, TextureFormat.ARGB32, false, false);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            var bytes = default(byte[]);
            var extension = default(string);
            var encodeToSpecific = settings.EncodeTo;
            //Debug.Log($"settings.EncodeTo = {settings.EncodeTo}");
            if (encodeToSpecific == FileFormat.All)
            {
                var ext = Path.GetExtension(saveToAbsolutePath).ToLower();
                if (ext == ".png")
                    encodeToSpecific = FileFormat.Png;
                else if (ext == ".tga")
                    encodeToSpecific = FileFormat.Tga;
                //else if (ext == ".exr")
                //    encodeToSpecific = FileFormat.Exr;
                else
                    Debug.LogError($"Unknown image extension: {ext}");
            }
            switch (encodeToSpecific)
            {
                case FileFormat.Png:
                    bytes = croppedTexture.EncodeToPNG();
                    extension = ".png";
                    break;
                case FileFormat.Tga:
                    bytes = croppedTexture.EncodeToTGA();
                    extension = ".tga";
                    break;
                //case FileFormat.Exr:
                //    bytes = croppedTexture.EncodeToEXR();
                //    extension = ".exr";
                //    break;
                case FileFormat.All:
                    break;
                default:
                    Debug.LogError($"Unknown image encoding option: {settings.EncodeTo}");
                    break;
            }
            //Debug.Log($"extension = {extension}");
            var fileName = Path.GetFileNameWithoutExtension(saveToAbsolutePath);
            var originalExtension = Path.GetExtension(saveToAbsolutePath);
            if (!settings.RewriteOriginal)
            {
                //Debug.Log($"originalExtension = {originalExtension}");

                var similarNamesCounter = 0;
                var originalSaveToAbsolutePath = saveToAbsolutePath;
                //Debug.Log($"saveToAbsolutePath = {saveToAbsolutePath}");
                while (File.Exists(saveToAbsolutePath))
                {
                    var newAbsolutePath = Path.Combine(originalSaveToAbsolutePath.Substring(0, originalSaveToAbsolutePath.Length - fileName.Length - originalExtension.Length), $"{fileName}{settings.CroppedFileNamingSchema}{(similarNamesCounter > 0 ? $" {similarNamesCounter}" : string.Empty)}{extension}");
                    //Debug.Log($"newAbsolutePath = {newAbsolutePath}");
                    saveToAbsolutePath = newAbsolutePath;
                    similarNamesCounter++;
                }
            }
            else
            {
                //If extension is rifferent we stil need to change name and therefore save original file. If extension the same, names will coincide and we'll rewrite.
                saveToAbsolutePath = Path.Combine(saveToAbsolutePath.Substring(0, saveToAbsolutePath.Length - fileName.Length - originalExtension.Length), $"{fileName}{extension}");
            }
            File.WriteAllBytes(saveToAbsolutePath, bytes);
            Object.DestroyImmediate(croppedTexture);

            var relativePath = GetRelativePathByAbsolute(saveToAbsolutePath);
            if (!CropedPaths.Contains(relativePath))
                CropedPaths.Add(relativePath);
            AssetDatabase.ImportAsset(relativePath);
        }

        internal static string GetAbsolutePathByRelative(string relativePath)
        {
            var applicationPath = Application.dataPath;
            return Path.Combine(applicationPath.Substring(0, applicationPath.Length - _assetsFolderName.Length), relativePath);
        }

        internal static string GetRelativePathByAbsolute(string absolutePath)
        {
            var applicationPath = Application.dataPath;
            return absolutePath.Substring(applicationPath.Length - _assetsFolderName.Length);
        }

        internal static bool ExtensionFits(string path, FileFormat ff)
        {
            var ext = Path.GetExtension(path).ToLower();
            switch (ff)
            {
                case FileFormat.All:
                    return ext == ".png" || ext == ".tga"/* || ext == ".exr"*/;
                case FileFormat.Png:
                    return ext == ".png";
                case FileFormat.Tga:
                    return ext == ".tga";
                //case FileFormat.Exr:
                //    return ext == ".exr";
                default:
                    return false;
            }
        }
    }
}
