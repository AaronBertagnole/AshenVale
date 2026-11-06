using UnityEditor;
using UnityEngine;

namespace AshenVale
{
    // Import settings for the Rimecaller hero frames and effects (128 PPU, uncompressed, smooth filtering).
    public sealed class RimecallerSpriteImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Heroes/Rimecaller/") && !assetPath.StartsWith("Assets/Resources/Art/Rimecaller/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = 128; t.spritePivot = new Vector2(.5f, .5f); t.mipmapEnabled = false; t.alphaIsTransparency = true;
            t.maxTextureSize = 2048; t.textureCompression = TextureImporterCompression.Uncompressed; t.filterMode = FilterMode.Bilinear;
        }
    }
}
