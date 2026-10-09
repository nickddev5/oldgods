using OldGods.Runtime;
using UnityEditor;

namespace OldGods.Editor
{
    /// <summary>Binds the pixel-art detail texture in the Editor too, so scene views show it outside Play mode.</summary>
    static class PixelTextureBinding
    {
        [InitializeOnLoadMethod]
        static void Bind() => PixelTexture.Bind();
    }
}
