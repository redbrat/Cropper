using UnityEditor;
using UnityEngine;

namespace Vis.TextureAutoCropper
{
    public class Settings : ScriptableObject
    {
        private const string _pointerToDbName = "TexturesAutoCropperDBFolderPointer";
        private const string _settingsFileName = "TexturesAutoCropperSettings.asset";

        private static Settings _settingsCache;
        internal static Settings FindInstance()
        {
            if (_settingsCache != null)
                return _settingsCache;

            var pointerToDbFolderGuids = AssetDatabase.FindAssets(_pointerToDbName);
            if (pointerToDbFolderGuids.Length == 0)
            {
                Debug.LogError($"TextureAutoCropper installation is corrupted. Please reimport asset from asset store!");
                return null;
            }
            var pointerToDbFolderPath = AssetDatabase.GUIDToAssetPath(pointerToDbFolderGuids[0]);

            var settingsPath = pointerToDbFolderPath.Substring(0, pointerToDbFolderPath.Length - _pointerToDbName.Length - ".bytes".Length) + _settingsFileName;
            _settingsCache = AssetDatabase.LoadAssetAtPath<Settings>(settingsPath);
            if (_settingsCache == null)
            {
                _settingsCache = CreateInstance<Settings>();
                AssetDatabase.CreateAsset(_settingsCache, settingsPath);
                AssetDatabase.SaveAssets();
            }

            return _settingsCache;
        }

        public bool CropAutomatically = true;
        public bool RewriteOriginal;
        public string CroppedFileNamingSchema = "-cropped";

        //public FileFormat FormatFilter = FileFormat.All;
        public FileFormat EncodeTo = FileFormat.All;

        public RectInt Padding;

        public GUISkin Skin;
    }
}
