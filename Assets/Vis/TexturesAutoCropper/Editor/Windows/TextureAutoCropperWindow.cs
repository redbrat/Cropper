using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Vis.TextureAutoCropper
{
    public class TextureAutoCropperWindow : EditorWindow
    {
        [MenuItem("Vis/Texture Auto Cropper")]
        private static void ShowWindow()
        {
            var instance = GetWindow<TextureAutoCropperWindow>();
            instance.titleContent = new GUIContent("Texture Auto Cropper");
        }

        private Texture2D _manualCroppedTexture;
        private DefaultAsset _manualCroppedFolder;
        private bool _showWrongFileFormatError;
        private bool _cropFolderRecursively;

        private void OnGUI()
        {
            var settings = Settings.FindInstance();
            if (settings == null)
            {
                EditorGUILayout.HelpBox($"TextureAutoCropper installation is corrupted. Please reimport asset from asset store!", MessageType.Error);
                return;
            }

            //var originalSkin = GUI.skin;
            //GUI.skin = settings.Skin;
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", settings.Skin.GetStyle("Caption"));
            EditorGUILayout.Space();

            EditorGUI.indentLevel++;

            var newAutoCrop = EditorGUILayout.Toggle(new GUIContent("Crop automatically", "Crop all imported textures automatically?"), settings.CropAutomatically);
            if (newAutoCrop != settings.CropAutomatically)
            {
                Undo.RecordObject(settings, "TextureAutoCropper - Crop automatically option changed");
                settings.CropAutomatically = newAutoCrop;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            //if (newAutoCrop)
            //{
            //    var newFormatFilter = (FileFormat)EditorGUILayout.EnumPopup(new GUIContent("Applied file format", "Which file formats must be cropped automatically?"), settings.FormatFilter);
            //    if (newFormatFilter != settings.FormatFilter)
            //    {
            //        Undo.RecordObject(settings, "TextureAutoCropper - Applied formats changed");
            //        settings.FormatFilter = newFormatFilter;
            //        EditorUtility.SetDirty(settings);
            //        AssetDatabase.SaveAssets();
            //    }
            //}

            var newRewriteOriginal = EditorGUILayout.Toggle("Rewrite original file", settings.RewriteOriginal);
            if (newRewriteOriginal != settings.RewriteOriginal)
            {
                Undo.RecordObject(settings, "TextureAutoCropper - Rewrite original option changed");
                settings.RewriteOriginal = newRewriteOriginal;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            if (newRewriteOriginal)
                EditorGUILayout.HelpBox($"Cropped file will replace original imported file", MessageType.Warning);
            else
            {
                var newCroppedFileNamingSchema = EditorGUILayout.TextField("Cropped file naming schema:", settings.CroppedFileNamingSchema);
                if (newCroppedFileNamingSchema != settings.CroppedFileNamingSchema)
                {
                    Undo.RecordObject(settings, "TextureAutoCropper - Cropped file naming schema changed");
                    settings.CroppedFileNamingSchema = newCroppedFileNamingSchema;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
                //if (newCroppedFileNamingSchema.Length == 0)
                //    EditorGUILayout.HelpBox($"Warning! Naming schema is empty - that means that cropped file's name will be the same as original file's name and original file would be rewritten by cropped file.", MessageType.Error);
                EditorGUILayout.HelpBox("Naming schema for cropped files can contain symbols a-zA-Z0-9-_. that will be added to original file's name.", MessageType.Info);
            }

            var newAlphaThreshold = EditorGUILayout.Slider(new GUIContent("Alpha Threshold", "Alpha value less or equal than this will be considered transparent and ready for crop."), settings.AlphaThreshold, 0f, 1f);
            if (newAlphaThreshold != settings.AlphaThreshold)
            {
                Undo.RecordObject(settings, "TextureAutoCropper - Alpha Threshold changed");
                settings.AlphaThreshold = newAlphaThreshold;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            var topLeftPadding = new Vector2Int(settings.Padding.x, settings.Padding.y);
            var newTopLeftPadding = EditorGUILayout.Vector2IntField(new GUIContent("Top Left padding: "), topLeftPadding);
            if (newTopLeftPadding != topLeftPadding)
            {
                var newPaddingRect = settings.Padding;
                newPaddingRect.x = newTopLeftPadding.x;
                newPaddingRect.y = newTopLeftPadding.y;

                Undo.RecordObject(settings, "TextureAutoCropper - Cropping padding changed");
                settings.Padding = newPaddingRect;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            var bottomRightPadding = new Vector2Int(settings.Padding.width, settings.Padding.height);
            var newBottomRightPadding = EditorGUILayout.Vector2IntField(new GUIContent("Bottom Right padding: "), bottomRightPadding);
            if (newBottomRightPadding != bottomRightPadding)
            {
                var newPaddingRect = settings.Padding;
                newPaddingRect.width = newBottomRightPadding.x;
                newPaddingRect.height = newBottomRightPadding.y;

                Undo.RecordObject(settings, "TextureAutoCropper - Cropping padding changed");
                settings.Padding = newPaddingRect;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            //var newPadding = EditorGUILayout.RectIntField(new GUIContent("Padding:", "How much space should be left uncropped from each side?"), settings.Padding);
            //if (!newPadding.Equals(settings.Padding))
            //{
            //    Undo.RecordObject(settings, "TextureAutoCropper - Cropping padding changed");
            //    settings.Padding = newPadding;
            //    EditorUtility.SetDirty(settings);
            //    AssetDatabase.SaveAssets();
            //}

            var encodeTo = (FileFormat)EditorGUILayout.EnumPopup(new GUIContent("Encode cropped to", "You may choose for cropped images in wich format to encode. \"All\" means to keep original format."), settings.EncodeTo);
            if (encodeTo != settings.EncodeTo)
            {
                Undo.RecordObject(settings, "TextureAutoCropper - Encode format changed");
                settings.EncodeTo = encodeTo;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUI.indentLevel--;


            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manual Cropping", settings.Skin.GetStyle("Caption"));
            EditorGUILayout.Space();


            _manualCroppedTexture = (Texture2D)EditorGUILayout.ObjectField("Texture: ", _manualCroppedTexture, typeof(Texture2D), false);
            if (_manualCroppedTexture != null)
            {
                if (!AssetDatabase.IsForeignAsset(_manualCroppedTexture))
                    _manualCroppedTexture = null;
                else if (!TexturesPostprocessors.ExtensionFits(AssetDatabase.GetAssetPath(_manualCroppedTexture), FileFormat.Png))
                {
                    _showWrongFileFormatError = true;
                    _manualCroppedTexture = null;
                }
                else
                    _showWrongFileFormatError = false;
            }
            if (_showWrongFileFormatError)
                EditorGUILayout.HelpBox($"Currently only .PNG file format supported for cropping.", MessageType.Error);
            if (_manualCroppedTexture != null && GUILayout.Button("Crop"))
            {
                var relativePath = AssetDatabase.GetAssetPath(_manualCroppedTexture);
                var absolutePath = TexturesPostprocessors.GetAbsolutePathByRelative(relativePath);

                if (!TexturesPostprocessors.CropedPaths.Contains(relativePath))
                    TexturesPostprocessors.CropedPaths.Add(relativePath);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                var bytes = File.ReadAllBytes(absolutePath);
                texture.LoadImage(bytes);
                //Debug.Log($"manual texture resolution = {texture.width}x{texture.height}");
                TexturesPostprocessors.Crop(texture, absolutePath, settings);
                DestroyImmediate(texture);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            _manualCroppedFolder = (DefaultAsset)EditorGUILayout.ObjectField(new GUIContent("Folder with textures: ", "Crop all textures inside that folder"), _manualCroppedFolder, typeof(DefaultAsset), false);
            if (_manualCroppedTexture != null)
            {
                if (!AssetDatabase.IsForeignAsset(_manualCroppedTexture))
                    _manualCroppedTexture = null;
            }
            if (_manualCroppedFolder != null)
            {
                _cropFolderRecursively = EditorGUILayout.Toggle(new GUIContent("Crop recursively", "Crop textures from all subfolders of that folder?"), _cropFolderRecursively);

                if (GUILayout.Button("Crop"))
                {
                    var relativePath = AssetDatabase.GetAssetPath(_manualCroppedFolder);
                    var absolutePath = TexturesPostprocessors.GetAbsolutePathByRelative(relativePath);
                    cropDirectory(absolutePath, _cropFolderRecursively, settings);
                }
            }



            EditorGUI.indentLevel--;

            //GUI.skin = originalSkin;
        }

        private void cropDirectory(string directory, bool recursively, Settings settings)
        {
            var allImages = Directory.GetFiles(directory).Where(p => Path.HasExtension(p) && TexturesPostprocessors.ExtensionFits(p, FileFormat.Png)).ToArray();

            for (int i = 0; i < allImages.Length; i++)
            {
                var absolutePath = allImages[i];
                var relativePath = TexturesPostprocessors.GetRelativePathByAbsolute(absolutePath);
                if (!TexturesPostprocessors.CropedPaths.Contains(relativePath))
                    TexturesPostprocessors.CropedPaths.Add(relativePath);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                var bytes = File.ReadAllBytes(absolutePath);
                texture.LoadImage(bytes);
                //Debug.Log($"manual texture resolution = {texture.width}x{texture.height}");
                TexturesPostprocessors.Crop(texture, absolutePath, settings);
                DestroyImmediate(texture);
            }

            if (recursively)
            {
                var directories = Directory.GetDirectories(directory);
                for (int i = 0; i < directories.Length; i++)
                    cropDirectory(directories[i], recursively, settings);
            }
        }

        private bool setReadable(string relativePath, bool value = true)
        {
            var tImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (tImporter != null)
            {
                var result = tImporter.isReadable;
                tImporter.isReadable = value;

                AssetDatabase.ImportAsset(relativePath);
                AssetDatabase.Refresh();
                return result;
            }
            else
            {
                Debug.LogError($"TextureAutoCropper - can't locate texture imported for asset {relativePath}!");
                return false;
            }
        }
    }
}
