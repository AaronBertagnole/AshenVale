using System.Collections.Generic;
using UnityEngine;
namespace AshenVale {
 public sealed partial class World {
  sealed class RimeCast { public int owner,map,facing,damage,remaining; public float wait,x,y; public bool storm; }
  readonly List<RimeCast> rimeCasts=new List<RimeCast>();
  void BeginRimeAttack(PlayerState p, MapRuntime map, bool skill) {
   if(p.attackCooldown>0 || p.climbing || p.knockTime>0 || p.hurtVisual>0 || p.dead || (skill && (p.mp<p.Hero.skillManaCost || p.skillRecharge>0))) return;
   if(skill) {p.mp-=p.Hero.skillManaCost;p.skillRecharge=p.Hero.skillRecharge;}
   p.lastWasSkill=skill;p.comboStep++;p.attackSerial++;p.attackPending=false;
   p.attackCooldown=skill?p.Hero.skillCooldown:p.Hero.attackCooldown;
   p.attackDuration=p.attackVisual=skill?.65f:.4f;
   float strikeX=Mathf.Clamp(p.x+p.facing*2.6f,map.def.minX,map.def.maxX);
   rimeCasts.Add(new RimeCast {owner=p.id,map=p.map,facing=p.facing,storm=skill,remaining=skill?3:1,
    wait=skill?p.Hero.skillWindup:p.Hero.basicWindup,
    damage=p.Hero.baseDamage+p.ch.level*Tuning.DamagePerLevel+p.ch.BonusAttack+(skill?p.Hero.skillBonus:0),
    x=strikeX,y=skill?FloorAt(map.def,strikeX,p.y):p.y});
  }
  void TickRimecaller(float dt) {
   PruneRimeOrbTargets();
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
      var orb=new ProjectileState {id=NextId(),map=p.map,owner=p.id,damage=c.damage,pierce=1,x=x,px=x,y=y,py=y,direction=c.facing,life=1.4f};
      AimNewRimeOrb(map,orb);map.shots.Add(orb);
     }
     c.remaining--;c.wait+=.22f;
    }
    if(c.remaining<=0) rimeCasts.RemoveAt(i);
   }
  }
  bool IsRimeShot(ProjectileState s) { var p=GetPlayer(s.owner);return p!=null && RimecallerRules.Is(p.Hero); }
  void UpdateRimeShot(MapRuntime map,ProjectileState s,float dt) { UpdateAimedRimeOrb(map,s,dt); }
  // Highest floor at x that is not above yRef (platform tops, else the ground).
  static float FloorAt(MapDef def,float x,float yRef) {
   float best=def.GroundAt(x);
   foreach(var plat in def.platforms) if(x>=plat.left && x<=plat.right && plat.top<=yRef+.3f && plat.top>best) best=plat.top;
   return best;
  }
  // Up / Down + Froststep: the nearest platform above (or below; Down also reaches the ground) that lies over the hero's x.
  static bool BlinkVertical(PlayerState p,MapDef def,bool up,out float destX,out float destY) {
   destX=p.x;destY=p.y;bool found=false;float bestTop=up?float.MaxValue:float.MinValue;
   foreach(var plat in def.platforms) {
    if(p.x<plat.left-.5f || p.x>plat.right+.5f) continue;
    float dy=plat.top-p.y;
    if(up) { if(dy<.35f || dy>RimecallerRules.BlinkUpRange || plat.top>=bestTop) continue; }
    else { if(dy>-.35f || dy<-RimecallerRules.BlinkDownRange || plat.top<=bestTop) continue; }
    bestTop=plat.top;found=true;destY=plat.top;
    float lo=plat.left+.2f,hi=plat.right-.2f;destX=hi>lo?Mathf.Clamp(p.x,lo,hi):(plat.left+plat.right)*.5f;
   }
   float ground = def.GroundAt(p.x);
   if(!found && !up && p.y-ground>.35f && p.y-ground<=RimecallerRules.BlinkDownRange) { found=true;destY=ground; }
   destX=Mathf.Clamp(destX,def.minX,def.maxX);
   return found;
  }
  void Froststep(PlayerState p,MapDef def,float vertical) {
   if(p.mp<RimecallerRules.BlinkCost || p.attackVisual>0) return;
   float destX=p.x,destY=p.y;bool vert=false;
   if(vertical>.5f) vert=BlinkVertical(p,def,true,out destX,out destY);
   else if(vertical<-.5f) vert=BlinkVertical(p,def,false,out destX,out destY);
   if(!vert) { destX=Mathf.Clamp(p.x+p.facing*RimecallerRules.BlinkDistance,def.minX,def.maxX);destY=p.y; }
   p.mp-=RimecallerRules.BlinkCost;p.dashCooldown=RimecallerRules.BlinkCooldown;
   events.Add(new SimEvent {kind=SimEventKind.RimeTeleport,map=p.map,playerId=p.id,x=p.x,y=p.y});
   p.x=destX;p.px=p.x;
   if(vert) { p.y=destY;p.vy=0;p.grounded=true;p.currentFloor=destY;p.dropTimer=0;p.climbing=false; }
   if (!vert && def.rollingGround)
   {
    float ground = def.GroundAt(p.x);
    if (p.grounded && def.IsGroundFloor(p.currentFloor)) p.y = p.currentFloor = ground;
    else if (!p.grounded) p.y = Mathf.Max(p.y, ground);
   }
   p.py=p.y;
   p.dashTime=0;p.crouching=false;
   events.Add(new SimEvent {kind=SimEventKind.RimeTeleport,map=p.map,playerId=p.id,x=p.x,y=p.y});
  }
 }
}
