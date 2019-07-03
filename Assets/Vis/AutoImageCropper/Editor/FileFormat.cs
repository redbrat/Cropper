using System;

namespace Vis.AutoImageCropper
{
    [Flags, Serializable]
    public enum FileFormat : int
    {
        Png = 0,
#if !UNITY_2017 && ! UNITY_5
        Tga = 1,
#endif
        //Exr = 2,
        All = 2
    }
}
