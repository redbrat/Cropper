using System;

namespace Vis.TextureAutoCropper
{
    [Flags, Serializable]
    public enum FileFormat : int
    {
        Png = 0,
        Tga = 1,
        //Exr = 2,
        All = 2
    }
}
