using System.Collections.Generic;
using UnityEngine;
namespace AshenVale {
 public sealed partial class WorldView {
  readonly List<GameObject> rimeFx=new List<GameObject>();
  void ClearRimeFx(){foreach(var go in rimeFx) if(go!=null) Object.Destroy(go);rimeFx.Clear();}
  void SpawnRimeFx(SimEvent e){
   rimeFx.RemoveAll(go=>go==null);
   string name=e.kind==SimEventKind.RimeStorm?"storm":e.kind==SimEventKind.RimeTeleport?"teleport":"impact";
   var sprite=Sprites.Art("Art/Rimecaller/"+name);if(sprite==null)return;
   float h=name=="storm"?3.8f:name=="teleport"?1.8f:1.2f;
   float y=e.y+(name=="impact"?0:h*.5f);
   var go=Sprites.Picture("Rime "+name,sprite,ArrowOrder+2,null,e.x,y);
   go.transform.localScale=Vector3.one*(h/sprite.bounds.size.y);
   go.AddComponent<RimeTransientFx>().duration=name=="storm"?.2f:.35f;rimeFx.Add(go);
  }
 }
}
