using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vis.AutoImageCropper
{
    public class TexturesPostprocessors : AssetPostprocessor
    {
        private const string _assetsFolderName = "Assets";

        private static List<string> IgnoredIds = new List<string>();

        internal static void IgnoreNextTime(string relativePath)
        {
            var guid = getId(relativePath);
            //Debug.LogWarning($"Now ignoring: {relativePath} {guid}");
            if (!IgnoredIds.Contains(guid))
                IgnoredIds.Add(guid);
        }

        internal static bool IsIgnored(string relativePath)
        {
            var guid = getId(relativePath);
            var answer = IgnoredIds.Contains(guid) || IgnoredIds.Contains(relativePath);
            //Debug.LogWarning($"Asking if ignored: {relativePath} {guid}. Answering: {answer}");
            return answer;
        }

        internal static void UnignoreNextTime(string relativePath)
        {
            var guid = getId(relativePath);
            //Debug.LogWarning($"Unignored: {relativePath} {guid}");
            if (IgnoredIds.Contains(guid))
                IgnoredIds.Remove(guid);
        }

        private static string getId(string relativePath)
        {
            var result = AssetDatabase.AssetPathToGUID(relativePath);
            if (string.IsNullOrEmpty(result))
                result = relativePath;
            return result;
        }

        private void OnPreprocessTexture()
        {
            var settings = Settings.FindInstance();
            if (settings == null)
                return;
            if (!settings.CropAutomatically)
                return;

            if (IsIgnored(assetPath))
            {
                //_cropedPaths.Remove(assetPath);
                return;
            }
            IgnoreNextTime(assetPath);

            var absolutePath = GetAbsolutePathByRelative(assetPath);
            if (!Path.HasExtension(absolutePath) || !ExtensionFits(absolutePath, FileFormat.Png))
                return;

            var bytes = File.ReadAllBytes(absolutePath);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
            texture.LoadImage(bytes);

            Crop(texture, absolutePath, settings);
            Object.DestroyImmediate(texture);
        }

        internal static void Crop(Texture2D texture, string saveToAbsolutePath, Settings settings)
        {
            var relativePath = GetRelativePathByAbsolute(saveToAbsolutePath);
            IgnoreNextTime(relativePath);

            var top = 0;
            var bottom = 0;
            var left = 0;
            var right = 0;

            var alphaThresholdCache = settings.AlphaThreshold;

            for (int y = 0; y < texture.height; y++)
            {
                top = y;
                for (int x = 0; x < texture.width; x++)
                {
                    var pixel = texture.GetPixel(x, y);
                    if (pixel.a > alphaThresholdCache)
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
                    if (pixel.a > alphaThresholdCache)
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
                    if (pixel.a > alphaThresholdCache)
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
                    if (pixel.a > alphaThresholdCache)
                        goto crop;
                }
            }

        crop:

            right = Mathf.Clamp(right + 1 + settings.Padding.width, 0, texture.width);

            var width = right - left;
            var heigth = bottom - top;
            var pixels = texture.GetPixels(left, top, width, heigth);

            var croppedTexture = new Texture2D(width, heigth, TextureFormat.ARGB32, false, false);
            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply();
            var bytes = default(byte[]);
            var extension = default(string);
            var encodeToSpecific = settings.EncodeTo;
            if (encodeToSpecific == FileFormat.All)
            {
                var ext = Path.GetExtension(saveToAbsolutePath).ToLower();
                if (ext == ".png")
                    encodeToSpecific = FileFormat.Png;
#if !UNITY_2017 && !UNITY_5
                else if (ext == ".tga")
                    encodeToSpecific = FileFormat.Tga;
#endif
                //else if (ext == ".exr")
                //    encodeToSpecific = FileFormat.Exr;
                else
                    Debug.LogError(string.Format("Unknown image extension: {0}", ext));
            }
            switch (encodeToSpecific)
            {
                case FileFormat.Png:
                    bytes = croppedTexture.EncodeToPNG();
                    extension = ".png";
                    break;
#if !UNITY_2017 && !UNITY_5
                case FileFormat.Tga:
                    bytes = croppedTexture.EncodeToTGA();
                    extension = ".tga";
                    break;
#endif
                //case FileFormat.Exr:
                //    bytes = croppedTexture.EncodeToEXR();
                //    extension = ".exr";
                //    break;
                case FileFormat.All:
                    break;
                default:
                    Debug.LogError(string.Format("Unknown image encoding option: {0}", settings.EncodeTo));
                    break;
            }
            var fileName = Path.GetFileNameWithoutExtension(saveToAbsolutePath);
            var originalExtension = Path.GetExtension(saveToAbsolutePath);
            if (!settings.RewriteOriginal)
            {
                var similarNamesCounter = 0;
                var originalSaveToAbsolutePath = saveToAbsolutePath;
                while (File.Exists(saveToAbsolutePath))
                {
                    var newAbsolutePath = Path.Combine(originalSaveToAbsolutePath.Substring(0, originalSaveToAbsolutePath.Length - fileName.Length - originalExtension.Length), string.Format("{0}{1}{2}{3}", fileName, settings.CroppedFileNamingSchema, (similarNamesCounter > 0 ? string.Format(" {0}", similarNamesCounter) : string.Empty), extension));
                    //$"{fileName}{settings.CroppedFileNamingSchema}{(similarNamesCounter > 0 ? $" {similarNamesCounter}" : string.Empty)}{extension}");
                    saveToAbsolutePath = newAbsolutePath;
                    similarNamesCounter++;
                }
            }
            else
            {
                //If extension is different we stil need to change name and therefore save original file. If extension the same, names will coincide and we'll rewrite.
                saveToAbsolutePath = Path.Combine(saveToAbsolutePath.Substring(0, saveToAbsolutePath.Length - fileName.Length - originalExtension.Length), string.Format("{0}{1}", fileName, extension));
            }
            File.WriteAllBytes(saveToAbsolutePath, bytes);
            Object.DestroyImmediate(croppedTexture);

            relativePath = GetRelativePathByAbsolute(saveToAbsolutePath);
            IgnoreNextTime(relativePath);
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
#if !UNITY_2017 && !UNITY_5
                case FileFormat.Tga:
                    return ext == ".tga";
#endif
                //case FileFormat.Exr:
                //    return ext == ".exr";
                default:
                    return false;
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            for (int i = 0; i < deletedAssets.Length; i++)
                if (ExtensionFits(deletedAssets[i], FileFormat.All))
                    UnignoreNextTime(deletedAssets[i]);
        }
    }
}
