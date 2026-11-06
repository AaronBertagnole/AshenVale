#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace AshenVale {
 public static class HeroPolishChecks {
  static void Check(bool pass,string message){if(!pass)throw new Exception("Hero polish: "+message);}
  static object Call(World w,string method,params object[] args){return typeof(World).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,args);}
  static MonsterState Mob(int id,int def,int map,float x,float y){return new MonsterState {id=id,def=def,map=map,x=x,y=y,hp=10000,maxHp=10000};}
  static ProjectileState Orb(PlayerState p,int id,float x,float y){return new ProjectileState {id=id,owner=p.id,map=p.map,x=x,px=x,y=y,py=y,direction=1,life=1.4f,damage=10,pierce=1};}
  [MenuItem("AshenVale/Hero polish/Run checks")]
  public static void Run(){
   var w=new World(29);var p=w.AddPlayer(new Character {hero=2,name="Polish test"});var map=w.maps[p.map];
   int small=0;for(int i=1;i<Monsters.All.Length;i++)if(Monsters.All[i].height<Monsters.All[small].height)small=i;
   float x=(map.def.minX+map.def.maxX)*.5f,y=p.y;p.x=x;
   var target=Mob(9101,small,p.map,x+2,y);map.monsters.Add(target);
   var orb=Orb(p,9201,x+.6f,y+1);map.shots.Add(orb);Call(w,"AimNewRimeOrb",map,orb);
   for(int i=0;i<45 && orb.pierce>0;i++)Call(w,"UpdateRimeShot",map,orb,1f/60f);
   Check(target.hp==9990 && orb.pierce==0,"orb reaches smallest enemy and hits once");
   map.monsters.Clear();map.shots.Clear();
   var behind=Mob(9102,small,p.map,x-2,y);var upper=Mob(9103,small,p.map,x+1,y+4);
   map.monsters.Add(behind);map.monsters.Add(upper);orb=Orb(p,9202,x+.6f,y+1);map.shots.Add(orb);
   Call(w,"AimNewRimeOrb",map,orb);float startY=orb.y;
   Call(w,"UpdateRimeShot",map,orb,.2f);Check(orb.y==startY && behind.hp==10000 && upper.hp==10000,"no behind/upper-platform targeting");
   map.monsters.Clear();map.shots.Clear();
   var near=Mob(9104,small,p.map,x+1,y);var far=Mob(9105,small,p.map,x+3,y);map.monsters.Add(far);map.monsters.Add(near);
   orb=Orb(p,9203,x,y+near.Def.hover+near.Def.height*.5f);map.shots.Add(orb);Call(w,"UpdateRimeShot",map,orb,.5f);
   Check(near.hp==9990 && far.hp==10000,"2D sweep stops at nearest body on a long tick");
   map.monsters.Clear();map.shots.Clear();target=Mob(9106,small,p.map,x+3,y);map.monsters.Add(target);
   orb=Orb(p,9204,x,y+1);map.shots.Add(orb);Call(w,"AimNewRimeOrb",map,orb);map.monsters.Clear();float before=orb.y;
   Call(w,"UpdateRimeShot",map,orb,.1f);Check(orb.y==before && orb.x>x,"missing target continues forward safely");
   Check(Heroes.All[0].folder=="DragonKnight" && Heroes.All[1].folder=="ElvenRanger" && Heroes.All[2].folder=="Rimecaller","class IDs unchanged");
   foreach(var folder in new[]{"DragonKnight","ElvenRanger","Rimecaller"}){
    foreach(var clip in new[]{"idle","run","jump","fall","climb","hurt","death","prone"})Check(Sprites.Hero(folder+"/"+clip).Length>0,folder+" missing "+clip);
    if(HeroPolishArt.Uses(folder))Check(Sprites.Hero(folder+"/climb").Length==4,folder+" climb needs four frames");
   }
   int pose=HeroPolishArt.ClimbIndex(2.3f,.24f,.38f,4);Check(pose==HeroPolishArt.ClimbIndex(2.3f,.24f,.38f,4),"stationary climb stable");
   Check(HeroPolishArt.ClimbIndex(.24f,.24f,.38f,4)!=HeroPolishArt.ClimbIndex(.63f,.24f,.38f,4),"height advances climb pose");
   Debug.Log("Hero polish checks passed: tiny target, no rear/high-floor aim, nearest-only sweep, missing target, class IDs, sprite coverage and height-driven climb. Still test visual feel and networking in Play Mode.");
  }
 }
}
#endif
