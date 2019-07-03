using UnityEditor;
using UnityEngine;

namespace Vis.AutoImageCropper
{
    public class Settings : ScriptableObject
    {
        private const string _pointerToDbName = "AutoImageCropperDBFolderPointer";
        private const string _settingsFileName = "AutoImageCropperSettings.asset";
        private const string _skinFileName = "AutoImageCropperSkin.guiskin";

        private static Settings _settingsCache;
        internal static Settings FindInstance()
        {
            if (_settingsCache != null)
                return _settingsCache;

            var pointerToDbFolderGuids = AssetDatabase.FindAssets(_pointerToDbName);
            if (pointerToDbFolderGuids.Length == 0)
            {
                Debug.LogError("AutoImageCropper installation is corrupted. Please reimport asset from asset store!");
                return null;
            }
            var pointerToDbFolderPath = AssetDatabase.GUIDToAssetPath(pointerToDbFolderGuids[0]);

            var settingsPath = pointerToDbFolderPath.Substring(0, pointerToDbFolderPath.Length - _pointerToDbName.Length - ".bytes".Length) + _settingsFileName;
            var skinPath = pointerToDbFolderPath.Substring(0, pointerToDbFolderPath.Length - _pointerToDbName.Length - ".bytes".Length) + _skinFileName;
            _settingsCache = AssetDatabase.LoadAssetAtPath<Settings>(settingsPath);
            var skin = AssetDatabase.LoadAssetAtPath<GUISkin>(skinPath);
            if (skin == null)
            {
                Debug.LogError("AutoImageCropper installation is corrupted. Please reimport asset from asset store!");
                return null;
            }

            if (_settingsCache == null)
            {
                _settingsCache = CreateInstance<Settings>();
                _settingsCache.Skin = skin;
                AssetDatabase.CreateAsset(_settingsCache, settingsPath);
                AssetDatabase.SaveAssets();
            }

            return _settingsCache;
        }

        public bool CropAutomatically;
        public bool RewriteOriginal;
        public string CroppedFileNamingSchema = "-cropped";

        public float AlphaThreshold = 0f;
        //public FileFormat FormatFilter = FileFormat.All;
        public FileFormat EncodeTo = FileFormat.All;

        public RectInt Padding;

        public GUISkin Skin;
    }
}
