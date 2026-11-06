using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Import settings for Assets/Resources/Heroes/StyleRefresh and the menu switch between refreshed and original Knight/Ranger art.
public sealed class HeroPolishSpriteImporter:AssetPostprocessor {
 public override int GetPostprocessOrder(){return 10000;}
 void OnPreprocessTexture(){
  if(!assetPath.StartsWith("Assets/Resources/Heroes/StyleRefresh/"))return;
  var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;
  var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(settings);
  t.spritePixelsPerUnit=128;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.maxTextureSize=2048;
  t.textureCompression=TextureImporterCompression.Uncompressed;t.filterMode=FilterMode.Bilinear;
 }
}
public static class HeroPolishStyleChoice {
 const string Marker="Assets/Resources/HeroPolish/ClassicStyle.txt";
 static void RequireStopped(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode before changing art sets.");}
 [MenuItem("AshenVale/Hero polish/Use original Knight and Ranger art")]
 public static void Original(){RequireStopped();Directory.CreateDirectory(Path.GetDirectoryName(Marker));File.WriteAllText(Marker,"Use original artwork for Knight and Ranger. Rimecaller fixes remain active.");AssetDatabase.Refresh();EditorUtility.RequestScriptReload();}
 [MenuItem("AshenVale/Hero polish/Use refreshed Knight and Ranger art")]
 public static void Refreshed(){RequireStopped();if(File.Exists(Marker))AssetDatabase.DeleteAsset(Marker);AssetDatabase.Refresh();EditorUtility.RequestScriptReload();}
}
