using UnityEngine;
namespace AshenVale {
 public sealed partial class WorldView {
  int FitPolishedClimb(PlayerState p,HeroView v,Sprite[] frames,float y) {
   float origin=Tuning.Ground+.24f,spacing=.38f,center=p.x;
   // Keep natural body scale. Old hand coordinates belong to the old sprite shapes.
   SpriteRenderer[] ladderArt;
   if(!climbLadders.TryGetValue(p.map,out ladderArt)){ladderArt=visuals[p.map].root.GetComponentsInChildren<SpriteRenderer>(true);climbLadders[p.map]=ladderArt;}
   foreach(var sr in ladderArt)if(sr!=null && sr.gameObject.name=="Ladder" && sr.sprite!=null && Mathf.Abs(sr.bounds.center.x-p.x)<.8f && y>=sr.bounds.min.y-.6f && y<=sr.bounds.max.y+.6f){
    center=sr.bounds.center.x;float unit=sr.transform.lossyScale.y/sr.sprite.pixelsPerUnit*sr.sprite.rect.height/84f;
    spacing=42f*unit;origin=sr.bounds.min.y+22f*unit;break;
   }
   v.climbXOffset=center-p.x;v.climbOffset=0;v.fittedClimb=true;v.climbScale=new Vector3(HeroScale,HeroScale,1);
   // Pose follows height, so stopping on a ladder freezes the hands and feet.
   return HeroPolishArt.ClimbIndex(y,origin,spacing,frames.Length);
  }
 }
}
