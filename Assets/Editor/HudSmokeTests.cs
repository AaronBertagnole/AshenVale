using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshenVale.Editor
{
    // Exercises real input latching and Canvas objects without a new testing package dependency.
    public static class HudSmokeTests
    {
        [MenuItem("Tools/Ashen Vale/Run Canvas HUD smoke checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode first, then run the Canvas HUD smoke checks."); return; }
            CheckInput();
            var root = new GameObject("Temporary HUD smoke check");
            try
            {
                var world = new World(1234);
                var me = world.AddPlayer(new Character { id = "hud-smoke", name = "HUD Test", hero = 0 }, Maps.Emberhaven, -1, -1);
                var legacy = new GameHud(world);
                var clicks = new List<GameAction>();
                var hud = root.AddComponent<CanvasHud>();
                hud.Bind(world, legacy, action => clicks.Add(action)); hud.Refresh(me); Canvas.ForceUpdateCanvases();
                Require(root.GetComponentsInChildren<Canvas>(true).Length == 1, "Exactly one HUD canvas");
                Require(legacy.canvasActive, "Legacy overlay hidden while Canvas is active");
                var minimap = root.GetComponentInChildren<HudMinimap>(true);
                Require(minimap != null && minimap.player == me, "Minimap bound to the actual player");
                Require(minimap.GetComponent<CanvasRenderer>() != null, "Minimap owns a CanvasRenderer");
                Require(minimap.canvasRenderer.GetMesh() != null && minimap.canvasRenderer.GetMesh().vertexCount > 0, "Minimap actually generates visible geometry");
                foreach (var glyph in root.GetComponentsInChildren<HudGlyph>(true))
                {
                    Require(glyph.GetComponent<CanvasRenderer>() != null, "Every action/menu icon owns a CanvasRenderer");
                    Require(glyph.canvasRenderer.GetMesh() != null && glyph.canvasRenderer.GetMesh().vertexCount > 0, "Icons generate visible geometry");
                }
                CheckInventoryPotions(legacy, me, clicks); clicks.Clear();
                Button attack = null, bag = null;
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    Require(button.navigation.mode == Navigation.Mode.None, "Gameplay keys must not navigate UI");
                    if (button.name == GameAction.Attack.ToString()) attack = button;
                    if (button.name == "Bag") bag = button;
                }
                Require(attack != null && attack.interactable, "Attack slot starts ready");
                attack.onClick.Invoke(); Require(clicks.Count == 1 && clicks[0] == GameAction.Attack, "Click reaches the input queue");
                me.attackCooldown = .2f; hud.Refresh(me); Require(!attack.interactable, "Shared attack cooldown blocks slot");
                me.attackCooldown = 0;
                Require(bag != null, "Bag shortcut exists"); bag.onClick.Invoke(); hud.Refresh(me);
                Require(legacy.inventoryOpen, "Bag uses existing inventory state");
                Require(!root.GetComponentInChildren<CanvasGroup>(true).blocksRaycasts, "IMGUI windows receive pointer events");
                legacy.inventoryOpen = false; me.dead = true; hud.Refresh(me); Require(!attack.interactable, "Death blocks actions");
                hud.SetVisible(false); Require(!root.GetComponentInChildren<Canvas>(true).gameObject.activeSelf, "HUD hides outside gameplay");
                Debug.Log("Canvas HUD smoke checks passed. Also run the visual/resolution and solo/online checklist in Docs/CanvasHUD.md.");
            }
            finally { UnityEngine.Object.Destroy(root); }
        }
        static void CheckInventoryPotions(GameHud legacy, PlayerState me, List<GameAction> clicks)
        {
            int hp = me.hp, mp = me.mp, healthCount = me.ch.hpPotions, manaCount = me.ch.mpPotions;
            legacy.onUseItem = action => clicks.Add(action);
            legacy.inventoryOpen = true;
            try
            {
                me.hp = me.MaxHp;
                Require(!legacy.UseInventoryPotion(me, true), "Full health cannot waste an inventory potion");
                me.hp = me.MaxHp - 1; me.ch.hpPotions = 1;
                int before = clicks.Count;
                Require(legacy.UseInventoryPotion(me, true) && clicks.Count == before + 1 && clicks[before] == GameAction.HpPotion, "Useables queues the health-potion input");
                Require(me.ch.hpPotions == 1 && me.hp == me.MaxHp - 1, "UI leaves potion consumption and healing to the world/host");
                me.ch.hpPotions = 0;
                Require(!legacy.UseInventoryPotion(me, true), "Empty stacks cannot queue use");
                me.mp = me.MaxMp; me.ch.mpPotions = 1;
                Require(!legacy.UseInventoryPotion(me, false), "Full mana cannot waste an inventory potion");
                me.mp = me.MaxMp - 1;
                before = clicks.Count;
                Require(legacy.UseInventoryPotion(me, false) && clicks[before] == GameAction.MpPotion, "Useables queues mana-potion input");
                me.dead = true;
                Require(!legacy.UseInventoryPotion(me, false), "Dead characters cannot use inventory potions");
            }
            finally
            {
                me.hp = hp; me.mp = mp; me.ch.hpPotions = healthCount; me.ch.mpPotions = manaCount;
                me.dead = false; legacy.inventoryOpen = false;
            }
        }

        static void CheckInput()
        {
            var input = new KeyboardInput();
            input.QueueAction(GameAction.Attack); input.QueueAction(GameAction.HpPotion);
            var first = input.Take(); var second = input.Take();
            Require(first.attack && first.hpPotion, "Clicks survive until a simulation tick");
            Require(!second.attack && !second.hpPotion, "Clicks are consumed once");
            input.QueueAction(GameAction.Skill); input.Clear(); Require(!input.Take().skill, "Run changes clear queued actions");
            var held = new PlayerInput { moveX = -1, moveY = 1, attack = true, townPortal = true }.HeldOnly();
            Require(held.moveX == -1 && held.moveY == 1 && !held.attack && !held.townPortal, "Movement persists without replaying clicks");
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("HUD check failed: " + message); }
    }
}

