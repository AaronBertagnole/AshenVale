using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AshenVale.EditorTools
{
    public static class EmberhavenMenuSmokeChecks
    {
        static void Require(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
        static void Call(FrontMenu menu,string name,params object[] args)
        {
            var method=typeof(FrontMenu).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
            if(method==null) throw new InvalidOperationException("Install the new menu first. Missing: "+name);
            method.Invoke(menu,args);
        }
        [MenuItem("AshenVale/Art/Emberhaven Menu Refresh/Run Flow Smoke Checks")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Stop Play mode first."); return; }
            var host=new GameObject("Menu smoke check (temporary)");
            try
            {
                int created=0; string createdName=null;
                var menu=new FrontMenu(new MusicPlayer(host),(hero,name)=>{created++; createdName=name;},
                    data=>{},(data,online)=>{},(data,online,address)=>{},data=>{});
                menu.SetSaved(new List<CharacterData>());
                Call(menu,"TitleAction",0);
                Require(menu.screen==FrontMenu.Screen.Select,"Empty roster should open hero creation.");
                Call(menu,"TitleAction",1);
                Require((bool)typeof(FrontMenu).GetField("settingsOpen",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu),"Settings must open without navigation.");
                Call(menu,"OpenHelp");
                Require(menu.screen==FrontMenu.Screen.Tutorial,"How to play should open the tutorial.");
                Call(menu,"FinishTutorial");
                Require(menu.screen==FrontMenu.Screen.Title && created==0,"Help tutorial must never create a hero.");
                menu.SetSaved(new List<CharacterData>{new CharacterData()});
                Call(menu,"TitleAction",0);
                Require(menu.screen==FrontMenu.Screen.Characters,"Continue should open the saved roster.");
                menu.heroName="  Kestrel  "; menu.pendingHero=0;
                typeof(FrontMenu).GetField("tutorialCreatesHero",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,true);
                Call(menu,"FinishTutorial");
                Require(created==1 && createdName=="Kestrel","New-hero tutorial should call creation once with a trimmed name.");
                Debug.Log("PASS: empty/saved roster navigation, help without character creation, and new-hero callback. Rendered UI and network connections still require Play-mode tests.");
            }
            catch(Exception e) { Debug.LogError("Menu smoke check failed: "+(e.InnerException ?? e).Message); }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}
