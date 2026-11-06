using UnityEngine;
namespace AshenVale {
 public static class HeroPolishArt {
  static readonly bool classic=Resources.Load<TextAsset>("HeroPolish/ClassicStyle")!=null;
  public static bool Uses(string folder) {
   return (folder=="Rimecaller" || !classic) && Sprites.Frames("Heroes/StyleRefresh/"+folder+"/idle").Length>0;
  }
  public static Sprite[] Resolve(string path) {
   int slash=path.IndexOf('/');string folder=slash<0?path:path.Substring(0,slash);
   if(Uses(folder)){var art=Sprites.Frames("Heroes/StyleRefresh/"+path);if(art.Length>0)return art;}
   return Sprites.Frames("Heroes/"+path);
  }
  public static int ClimbIndex(float y,float origin,float spacing,int count) {
   if(count<=1)return 0;
   int step=Mathf.FloorToInt((y-origin)/Mathf.Max(.1f,spacing));return (step%count+count)%count;
  }
 }
}
