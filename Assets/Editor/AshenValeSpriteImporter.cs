using System.IO;
using UnityEditor;
using UnityEngine;

// Import settings for the game's art, so new files work just by dropping them into the right folder:
//   Assets/Resources/Heroes/...      pixel-art heroes: 128 px per unit, point filtering (unchanged)
//   Assets/Resources/Art/...         painted art (see ART.md): 100 px per unit, smooth filtering,
//                                    pivots chosen so each piece lines up with the map without any offsets
public class AshenValeSpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith("Assets/Resources/Heroes/"))
        {
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            return;
        }
        if (assetPath.StartsWith("Assets/Resources/Art/")) ImportArt((TextureImporter)assetImporter, assetPath);
    }

    static void ImportArt(TextureImporter importer, string path)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 8192;     // painted scenes are wider than Unity's default 2048 limit

        // Pivot: monsters, props, scenes and backdrops stand on their bottom edge; platforms and ground hang from their top edge
        // (the walking surface); far / mid backdrops sit on their bottom edge; anything else is centred.
        string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        var alignment = SpriteAlignment.Center;
        if (path.StartsWith("Assets/Resources/Art/Monsters/") || path.StartsWith("Assets/Resources/Art/Props/") ||
            path.StartsWith("Assets/Resources/Art/Scenes/") || path.StartsWith("Assets/Resources/Art/Backdrops/")) alignment = SpriteAlignment.BottomCenter;
        else if (file.StartsWith("platform") || file == "ground") alignment = SpriteAlignment.TopCenter;
        else if (file == "far" || file == "mid" || file == "ladder" || file == "ground_back") alignment = SpriteAlignment.BottomCenter;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)alignment;
        settings.spriteMeshType = SpriteMeshType.FullRect;   // needed for tiled drawing (ground, platforms, backdrops)
        importer.SetTextureSettings(settings);
    }
}
