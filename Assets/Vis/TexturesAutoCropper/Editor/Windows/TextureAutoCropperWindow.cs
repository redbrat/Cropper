using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vis.TextureAutoCropper
{
    internal class TextureAutoCropperWindow : EditorWindow
    {
        [MenuItem("Vis/Texture Auto Cropper")]
        private static void ShowWindow()
        {
            var instance = GetWindow<TextureAutoCropperWindow>();
            instance.titleContent = new GUIContent("Texture Auto Cropper");
        }

        private Texture2D _manualCroppedTexture;

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
                AssetDatabase.SaveAssets();
            }

            var newRewriteOriginal = EditorGUILayout.Toggle("Rewrite original file", settings.RewriteOriginal);
            if (newRewriteOriginal != settings.RewriteOriginal)
            {
                Undo.RecordObject(settings, "TextureAutoCropper - Rewrite original option changed");
                settings.RewriteOriginal = newRewriteOriginal;
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
                var applicationPath = Application.dataPath;
                var absolutePath = Path.Combine(applicationPath.Substring(0, applicationPath.Length - TexturesPostprocessors.AssetsFolderName.Length), relativePath);

                if (!TexturesPostprocessors.CropedPaths.Contains(relativePath))
                    TexturesPostprocessors.CropedPaths.Add(relativePath);
                var originalReadable = setReadable(relativePath);
                TexturesPostprocessors.Crop(_manualCroppedTexture, absolutePath, settings);
                setReadable(relativePath, originalReadable);
                _manualCroppedTexture = null;
            }


            EditorGUI.indentLevel--;

            //GUI.skin = originalSkin;
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
