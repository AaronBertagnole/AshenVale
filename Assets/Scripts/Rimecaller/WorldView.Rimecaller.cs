using System.Collections.Generic;
using UnityEngine;
namespace AshenVale {
 public sealed partial class WorldView {
  const float GroundPad=.165f;
  readonly List<GameObject> rimeFx=new List<GameObject>();
  // The orb art has its head on the left and its tail on the right: flip it when flying right, tilt it along its path.
  void AimRimeShot(GameObject go,ProjectileState s){
   var sr=go.GetComponent<SpriteRenderer>();var frost = go.GetComponent<RimeFrostWake>();
   if (frost == null) frost = go.AddComponent<RimeFrostWake>();
   sr.flipX = frost.HasArt ? s.direction < 0 : s.direction > 0;
   if (frost.ShedSliver()) AddAttackSpark(Sprites.Disc, go.transform.position, new Vector2(-s.direction * .8f, Mathf.Sin(Time.time * 32f) * .55f), new Color(.68f,.93f,1f,.85f), .095f, .025f, .17f);
   float dx=Mathf.Abs(s.x-s.px),dy=s.y-s.py;
   if(dx+Mathf.Abs(dy)>.0005f) go.transform.rotation=Quaternion.Euler(0,0,Mathf.Rad2Deg*s.direction*Mathf.Atan2(dy,dx));
  }
  void ClearRimeFx(){foreach(var go in rimeFx) if(go!=null) Object.Destroy(go);rimeFx.Clear();}
  void SpawnRimeFx(SimEvent e){
   rimeFx.RemoveAll(go=>go==null);
   string name=e.kind==SimEventKind.RimeStorm?"storm":e.kind==SimEventKind.RimeTeleport?"teleport":"impact";
   if (name == "storm" && SpawnPolishedStorm(e)) return;
   if (e.kind == SimEventKind.RimeBurst || e.kind == SimEventKind.RimeTeleport)
   {
       var effect = new GameObject(name == "impact" ? "Frost shatter" : "Froststep shimmer");
       effect.transform.position = new Vector3(e.x, e.y, 0f);
       effect.AddComponent<RimeIceAccent>().Begin(name == "teleport", ArrowOrder + 2);
       rimeFx.Add(effect);
       return;
   }
   var sprite=Sprites.Art("Art/Rimecaller/"+name);if(sprite==null)return;
   float h=name=="storm"?3.8f:name=="teleport"?1.8f:1.2f;
   float y=e.y+(name=="impact"?0:h*(.5f-GroundPad));   // storm/teleport art: ground contact is ~16% above the bottom edge
   var go=Sprites.Picture("Rime "+name,sprite,ArrowOrder+2,null,e.x,y);
   go.transform.localScale=Vector3.one*(h/sprite.bounds.size.y);
   go.AddComponent<RimeTransientFx>().duration=name=="storm"?.2f:.35f;rimeFx.Add(go);
  }
 }
}
