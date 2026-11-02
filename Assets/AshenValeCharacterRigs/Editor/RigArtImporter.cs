using UnityEditor;
using UnityEngine;

namespace AshenVale.CharacterRigs.Editor
{
    public sealed class RigArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/AshenValeCharacterRigs/Art/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.alphaIsTransparency = true; t.mipmapEnabled = false;
            t.isReadable = (assetPath.EndsWith("_Parts.png") || assetPath.EndsWith("_Cape.png"));
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.maxTextureSize = 4096; t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
