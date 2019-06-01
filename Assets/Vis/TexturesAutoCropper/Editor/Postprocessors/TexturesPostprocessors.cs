using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturesPostprocessors : AssetPostprocessor
{
    private const string _assetsFolderName = "Assets";

    private static List<string> _cropedPaths = new List<string>();

    private void OnPreprocessTexture()
    {
        if (_cropedPaths.Contains(assetPath))
        {
            _cropedPaths.Remove(assetPath);
            return;
        }
        _cropedPaths.Add(assetPath);

        var applicationPath = Application.dataPath;
        var absolutePath = Path.Combine(applicationPath.Substring(0, applicationPath.Length - _assetsFolderName.Length), assetPath);
        if (!Path.HasExtension(absolutePath) || Path.GetExtension(absolutePath) != ".png")
            return;

        Debug.Log($"absolutePath = {absolutePath}");
        var bytes = File.ReadAllBytes(absolutePath);
        var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
        texture.LoadImage(bytes);

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
        Object.DestroyImmediate(texture);

        var croppedTexture = new Texture2D(width, heigth, TextureFormat.ARGB32, false, false);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();
        bytes = croppedTexture.EncodeToPNG();
        File.WriteAllBytes(absolutePath, bytes);
        //File.WriteAllBytes(absolutePath.Substring(0, absolutePath.Length - ".png".Length) + "-cropped.png", bytes);
        Object.DestroyImmediate(croppedTexture);

        AssetDatabase.ImportAsset(assetPath);
    }
}
