using System.Collections.Generic;
using UnityEngine;
namespace AshenVale {
 public sealed partial class World {
  readonly Dictionary<int,int> rimeOrbTargets=new Dictionary<int,int>();
  MonsterState RimeTarget(MapRuntime map,float x,float y,float facing) {
   MonsterState best=null;float nearest=7f;
   foreach(var m in map.monsters) {
    float forward=(m.x-x)*facing,body=m.y+m.Def.hover+m.Def.height*.5f;
    if(m.hp<=0 || forward<-.15f || forward>nearest || Mathf.Abs(body-y)>1.35f)continue;
    best=m;nearest=forward;
   }
   return best;
  }
  void AimNewRimeOrb(MapRuntime map,ProjectileState s) {
   var target=RimeTarget(map,s.x,s.y,s.direction);if(target==null)return;
   rimeOrbTargets[s.id]=target.id;   // the path is steered during flight; the release point is not moved
  }
  void PruneRimeOrbTargets() {
   var live=new HashSet<int>();foreach(var map in maps)foreach(var s in map.shots)live.Add(s.id);
   var gone=new List<int>();foreach(var key in rimeOrbTargets.Keys)if(!live.Contains(key))gone.Add(key);
   foreach(var key in gone)rimeOrbTargets.Remove(key);
  }
  void UpdateAimedRimeOrb(MapRuntime map,ProjectileState s,float dt) {
   MonsterState target=null;int id;
   if(rimeOrbTargets.TryGetValue(s.id,out id))foreach(var m in map.monsters)if(m.id==id && m.hp>0){target=m;break;}
   if(target!=null && ((target.x-s.x)*s.direction<-.15f || Mathf.Abs(target.y+target.Def.hover+target.Def.height*.5f-s.y)>1.35f))target=null;
   if(target==null){target=RimeTarget(map,s.x,s.y,s.direction);if(target!=null)rimeOrbTargets[s.id]=target.id;else rimeOrbTargets.Remove(s.id);}
   Vector2 a=new Vector2(s.x,s.y),b=new Vector2(s.x+s.direction*9f*dt,s.y);
   if(target!=null) {
    // Aim for the body centre so the orb arrives there as it reaches the target: a straight line, not a drop-then-flat kink.
    float ty=target.y+target.Def.hover+target.Def.height*.5f;
    float run=Mathf.Max(0f,(target.x-s.x)*s.direction-(target.Def.width*.5f+.18f));
    float vy=Mathf.Clamp((ty-s.y)/Mathf.Max(.1f,run/9f),-8f,8f);
    b.y=Mathf.MoveTowards(s.y,ty,Mathf.Abs(vy)*dt);
   }
   float best=2f;MonsterState hit=null;
   foreach(var m in map.monsters) {
    if(m.hp<=0)continue;float t;
    if(RimeSegmentHit(a,b,m.x-m.Def.width*.5f-.18f,m.x+m.Def.width*.5f+.18f,m.y+m.Def.hover-.18f,m.y+m.Def.hover+m.Def.height+.18f,out t) && t<best){best=t;hit=m;}
   }
   s.life-=dt;
   Vector2 end=hit==null?b:Vector2.Lerp(a,b,best);s.x=end.x;s.y=end.y;
   if(hit!=null){HitMonster(map,hit,s.damage,GetPlayer(s.owner),Tuning.PushOrb);s.pierce=0;events.Add(new SimEvent {kind=SimEventKind.RimeBurst,map=s.map,playerId=s.owner,x=s.x,y=s.y});}
   if(s.x<map.def.minX || s.x>map.def.maxX)s.life=0;
   if(s.life<=0 || s.pierce<=0)rimeOrbTargets.Remove(s.id);
  }
  static bool RimeSegmentHit(Vector2 a,Vector2 b,float left,float right,float bottom,float top,out float t) {
   float lo=0,hi=1;Vector2 d=b-a;
   bool hit=RimeSlab(a.x,d.x,left,right,ref lo,ref hi) && RimeSlab(a.y,d.y,bottom,top,ref lo,ref hi);t=lo;return hit;
  }
  static bool RimeSlab(float start,float delta,float low,float high,ref float lo,ref float hi) {
   if(Mathf.Abs(delta)<.00001f)return start>=low && start<=high;
   float a=(low-start)/delta,b=(high-start)/delta;if(a>b){float temp=a;a=b;b=temp;}
   lo=Mathf.Max(lo,a);hi=Mathf.Min(hi,b);return lo<=hi;
  }
 }
}
