#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
namespace AshenVale {
 public static class RimecallerSmokeChecks {
  static void Check(bool ok,string message){if(!ok)throw new Exception("Rimecaller check: "+message);}
  static object Call(World w,string name,params object[] args){return typeof(World).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,args);}
  [MenuItem("AshenVale/Rimecaller/Run smoke checks")]
  public static void Run(){
   Check(Heroes.All.Length>=3 && Heroes.All[0].folder=="DragonKnight" && Heroes.All[1].folder=="ElvenRanger" && RimecallerRules.Is(Heroes.All[2]),"class IDs preserved");
   var w=new World(17);var p=w.AddPlayer(new Character {hero=2,name="Rime test"});var map=w.maps[p.map];
   Call(w,"BeginRimeAttack",p,map,false);Call(w,"TickRimecaller",.19f);
   Check(map.shots.Count==1,"orb releases after windup");
   Call(w,"BeginRimeAttack",p,map,false);Call(w,"TickRimecaller",1f);Check(map.shots.Count==1,"cooldown blocks repeated cast");
   var near=new MonsterState {id=9001,def=0,map=p.map,x=p.x+1.5f,y=p.y,hp=10000,maxHp=10000};
   var far=new MonsterState {id=9002,def=0,map=p.map,x=p.x+3f,y=p.y,hp=10000,maxHp=10000};
   map.monsters.Clear();map.monsters.Add(far);map.monsters.Add(near);
   var orb=map.shots[0];orb.x=p.x;orb.y=near.y+near.Def.hover+near.Def.height*.5f;orb.direction=1;
   Call(w,"UpdateRimeShot",map,orb,.5f);
   Check(near.hp<10000 && far.hp==10000 && orb.pierce==0,"swept orb hits nearest only, regardless of list order");
   w.events.Clear();map.monsters.Clear();map.shots.Clear();
   p.attackCooldown=0;p.attackVisual=0;int mana=p.mp;
   Call(w,"BeginRimeAttack",p,map,true);Check(p.mp==mana-p.Hero.skillManaCost,"Stormcall mana cost");
   Call(w,"TickRimecaller",.7f);int strikes=0;float x=0;
   foreach(var e in w.events)if(e.kind==SimEventKind.RimeStorm){if(strikes>0)Check(e.x==x,"same storm target");x=e.x;strikes++;}
   Check(strikes==3,"exactly three strikes including long tick");
   w.events.Clear();p.attackCooldown=0;p.attackVisual=0;p.mp=0;
   Call(w,"BeginRimeAttack",p,map,true);Call(w,"TickRimecaller",1f);Check(w.events.Count==0,"insufficient mana blocks storm");
   p.mp=100;p.attackCooldown=0;Call(w,"BeginRimeAttack",p,map,true);p.dead=true;Call(w,"TickRimecaller",1f);Check(w.events.Count==0,"death cancels pending cast");p.dead=false;
   p.attackVisual=0;p.mp=100;p.dashCooldown=0;p.x=map.def.maxX-.1f;p.facing=1;Call(w,"Froststep",p,map.def,0f);   // mana reset: the death-cancel check above already spent 24 MP on Stormcall
   Check(p.x<=map.def.maxX && p.x==p.px && p.dashTime==0,"blink clamps and does not dash/interpolate");
   Check(p.mp==100-RimecallerRules.BlinkCost && p.dashCooldown==RimecallerRules.BlinkCooldown,"blink cost/cooldown");
   // Up / Down + Froststep: onto the nearest platform above, then back down (skipped when this map has no suitable platform).
   foreach(var plat in map.def.platforms){
    float rise=plat.top-Tuning.Ground;if(rise<.5f||rise>RimecallerRules.BlinkUpRange)continue;
    p.dead=false;p.attackVisual=0;p.mp=100;p.dashCooldown=0;p.grounded=true;p.x=(plat.left+plat.right)*.5f;p.y=p.currentFloor=Tuning.Ground;
    Call(w,"Froststep",p,map.def,1f);
    Check(p.y>Tuning.Ground+.3f&&p.grounded&&p.currentFloor==p.y&&p.dashTime==0,"Up + Froststep lands on a platform above");
    p.mp=100;p.dashCooldown=0;float up=p.y;Call(w,"Froststep",p,map.def,-1f);
    Check(p.y<up-.3f&&p.grounded&&p.currentFloor==p.y,"Down + Froststep lands on a floor below");
    break;
   }
   foreach(string clip in new[]{"idle","run","jump","fall","orb_cast","storm_cast","hurt","death","climb","prone"})Check(Sprites.Hero("Rimecaller/"+clip).Length>0,"missing "+clip);
   foreach(string fx in new[]{"orb","impact","teleport","storm"})Check(Sprites.Art("Art/Rimecaller/"+fx)!=null,"missing "+fx);
   UnityEngine.Debug.Log("Rimecaller smoke checks passed: class IDs, windup, cooldown, mana, three fixed strikes, death cancellation, blink boundaries, all clips/effects. Play Mode combat/animation still needs testing.");
  }
 }
}
#endif
