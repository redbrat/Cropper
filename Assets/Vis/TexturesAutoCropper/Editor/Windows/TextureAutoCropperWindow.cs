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

            var newPadding = EditorGUILayout.RectIntField(new GUIContent("Padding:", "How much space should be left uncropped from each side?"), settings.Padding);
            if (!newPadding.Equals(settings.Padding))
            {
                Undo.RecordObject(settings, "TextureAutoCropper - Cropping padding changed");
                settings.Padding = newPadding;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUI.indentLevel--;


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manual Cropping", settings.Skin.GetStyle("Caption"));
            EditorGUILayout.Space();


            _manualCroppedTexture = (Texture2D)EditorGUILayout.ObjectField("Texture: ", _manualCroppedTexture, typeof(Texture2D), false);
            if (_manualCroppedTexture != null)
            {
                if (!AssetDatabase.IsForeignAsset(_manualCroppedTexture))
                    _manualCroppedTexture = null;
            }
            if (_manualCroppedTexture != null && GUILayout.Button("Crop"))
            {
                var relativePath = AssetDatabase.GetAssetPath(_manualCroppedTexture);
                var absolutePath = TexturesPostprocessors.GetAbsolutePathByRelative(relativePath);

                if (!TexturesPostprocessors.CropedPaths.Contains(relativePath))
                    TexturesPostprocessors.CropedPaths.Add(relativePath);
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                var bytes = File.ReadAllBytes(absolutePath);
                texture.LoadImage(bytes);
                Debug.Log($"manual texture resolution = {texture.width}x{texture.height}");
                TexturesPostprocessors.Crop(texture, absolutePath, settings);
                DestroyImmediate(texture);
            }

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
            var allImages = Directory.GetFiles(directory).Where(p => Path.HasExtension(p) && Path.GetExtension(p) == ".png").ToArray();

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
