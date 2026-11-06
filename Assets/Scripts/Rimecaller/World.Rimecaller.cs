using System.Collections.Generic;
using UnityEngine;
namespace AshenVale {
 public sealed partial class World {
  sealed class RimeCast { public int owner,map,facing,damage,remaining; public float wait,x,y; public bool storm; }
  readonly List<RimeCast> rimeCasts=new List<RimeCast>();
  void BeginRimeAttack(PlayerState p, MapRuntime map, bool skill) {
   if(p.attackCooldown>0 || p.climbing || p.knockTime>0 || p.hurtVisual>0 || p.dead || (skill && p.mp<p.Hero.skillManaCost)) return;
   if(skill) p.mp-=p.Hero.skillManaCost;
   p.lastWasSkill=skill;p.comboStep++;p.attackSerial++;p.attackPending=false;
   p.attackCooldown=skill?p.Hero.skillCooldown:p.Hero.attackCooldown;
   p.attackDuration=p.attackVisual=skill?.65f:.4f;
   rimeCasts.Add(new RimeCast {owner=p.id,map=p.map,facing=p.facing,storm=skill,remaining=skill?3:1,
    wait=skill?p.Hero.skillWindup:p.Hero.basicWindup,
    damage=p.Hero.baseDamage+p.ch.level*Tuning.DamagePerLevel+p.ch.BonusAttack+(skill?p.Hero.skillBonus:0),
    x=Mathf.Clamp(p.x+p.facing*2.6f,map.def.minX,map.def.maxX),y=p.y});
  }
  void TickRimecaller(float dt) {
   for(int i=rimeCasts.Count-1;i>=0;i--) {
    var c=rimeCasts[i];var p=GetPlayer(c.owner);
    if(p==null || p.dead || p.map!=c.map || p.climbing) {rimeCasts.RemoveAt(i);continue;}   // getting hit does not cancel a cast
    c.wait-=dt;var map=maps[c.map];
    while(c.wait<=0 && c.remaining>0) {
     if(c.storm) {
      events.Add(new SimEvent {kind=SimEventKind.RimeStorm,map=c.map,playerId=c.owner,x=c.x,y=c.y});
      var targets=new List<MonsterState>();
      foreach(var m in map.monsters) if(Mathf.Abs(m.x-c.x)<=.85f+m.Def.width*.5f && m.y+m.Def.hover < c.y+2f && m.y+m.Def.hover+m.Def.height>c.y-.25f) targets.Add(m);
      foreach(var m in targets) HitMonster(map,m,c.damage,p);
     } else {
      float x=p.x+c.facing*.6f,y=p.y+1.0f;
      map.shots.Add(new ProjectileState {id=NextId(),map=p.map,owner=p.id,damage=c.damage,pierce=1,x=x,px=x,y=y,py=y,direction=c.facing,life=1.4f});
     }
     c.remaining--;c.wait+=.22f;
    }
    if(c.remaining<=0) rimeCasts.RemoveAt(i);
   }
  }
  bool IsRimeShot(ProjectileState s) { var p=GetPlayer(s.owner);return p!=null && RimecallerRules.Is(p.Hero); }
  void UpdateRimeShot(MapRuntime map,ProjectileState s,float dt) {
   float end=s.x+s.direction*9f*dt,best=float.MaxValue;MonsterState hit=null;
   foreach(var m in map.monsters) {
    float bottom=m.y+m.Def.hover;
    if(s.y<bottom-.18f || s.y>bottom+m.Def.height+.18f) continue;
    float half=m.Def.width*.5f+.18f;
    if(Mathf.Max(s.x,end)<m.x-half || Mathf.Min(s.x,end)>m.x+half) continue;
    float distance=Mathf.Max(0,(m.x-s.x)*s.direction-half);
    if(distance<best){best=distance;hit=m;}
   }
   s.life-=dt;
   if(hit!=null) {
    s.x+=s.direction*best;
    HitMonster(map,hit,s.damage,GetPlayer(s.owner));s.pierce=0;
    events.Add(new SimEvent {kind=SimEventKind.RimeBurst,map=s.map,playerId=s.owner,x=s.x,y=s.y});
   } else s.x=end;
   if(s.x<map.def.minX || s.x>map.def.maxX) s.life=0;
  }
  void Froststep(PlayerState p,MapDef def) {
   if(p.mp<RimecallerRules.BlinkCost || p.attackVisual>0) return;
   p.mp-=RimecallerRules.BlinkCost;p.dashCooldown=RimecallerRules.BlinkCooldown;
   events.Add(new SimEvent {kind=SimEventKind.RimeTeleport,map=p.map,playerId=p.id,x=p.x,y=p.y});
   p.x=Mathf.Clamp(p.x+p.facing*RimecallerRules.BlinkDistance,def.minX,def.maxX);p.px=p.x;p.py=p.y;
   p.dashTime=0;p.crouching=false;
   events.Add(new SimEvent {kind=SimEventKind.RimeTeleport,map=p.map,playerId=p.id,x=p.x,y=p.y});
  }
 }
}
