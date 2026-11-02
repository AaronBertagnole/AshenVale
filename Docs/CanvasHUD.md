# Canvas HUD — first integration

The in-game HUD builds automatically from `AshenValeGame.Awake()` using the project's existing uGUI dependency. Open the existing AshenVale scene and press Play; start or load a hero. There is no manual prefab/scene setup and no added package dependency.

The approved visual direction is deep teal, copper, ivory and parchment. The implementation uses nine-slice surfaces and vector symbols generated once in code, live text and the selected hero's existing idle sprite. It is an implementation of the design direction, not a pixel-exact reproduction of the concept image or a new painted portrait/skill-icon set.

## Data and interaction

- HP, MP, XP, level, hero name, gold and potion counts come from PlayerState/Character.
- Six slots correspond to the game's actual Attack, Skill, Dash, Town Portal, HP Potion and MP Potion actions. Labels follow Keys bindings. Cooldowns reflect the shared attack timer, dash timer and portal cast timer; unavailable actions dim.
- Clicking a slot queues a one-shot PlayerInput through KeyboardInput. Solo, host and client keep the same simulation and networking routes. The world still validates costs, cooldowns and state.
- Bag, World and Menu open the existing GameHud windows. Existing shop, smithy, controls and waystone actions remain on IGameCommands. Their IMGUI surfaces use the matching theme; those windows have not been rewritten as Canvas layouts.
- Minimap renders real terrain, portals, NPC facilities, monsters, chests, party and camera bounds; undiscovered secret portals stay hidden. Tab and world-map bindings still work.
- The adventure log shows real arrivals, level changes and existing pickup notifications. Journey guidance uses the current map and discovered towns. There are no simulated quests or chat messages: those systems are not in the project yet.
- Nearby merchant/waystone/smithy prompts follow the existing interaction range. World labels, monster/boss bars, damage numbers and the death screen remain with GameHud.

Decorative graphics do not intercept clicks. UI buttons use hover/pressed states, clear EventSystem selection after clicks, and disable navigation so arrows/Space stay gameplay controls. Mouse-bound actions are filtered when the pointer is over a HUD button. Existing keyboard bindings continue to work.

## Layout and lifecycle

A 1280x720 reference Canvas uses Expand scaling and safe-area anchors. The center remains open for gameplay. The Canvas is hidden on front menus, temporarily hides while an IMGUI window is open (so Overlay Canvas graphics cannot cover legacy dialogs), and blocks action slots on death. It creates an EventSystem only when none exists, and cleans up the textures/sprites/skin it owns. Scene changes and hero selection clear queued clicks.

## Validation

This environment has no Unity editor or C# compiler. Runtime compilation, visual parity and input/network smoke testing have **not** been executed here. Review this as a draft until checked in the project editor.

A dependency-free smoke-check menu is included: while in Play Mode choose **Tools > Ashen Vale > Run Canvas HUD smoke checks (Play Mode)**. It checks one-shot input consumption, real Canvas construction, minimap binding, slot cooldown/death gating and inventory-window pointer handoff. Temporary objects are destroyed afterwards. This menu does not replace manual visual and online tests.

Before merging:

1. Unity 6000.3.27f2 imports and compiles without errors; no Console errors after leaving/re-entering gameplay.
2. Check 1280x720, 1920x1080, 2560x1080 and 1024x768. Inspect labels, keycaps, minimap and modal windows for clipping/overlap.
3. Create/load both heroes; confirm selected sprite/name, HP/MP/XP, level, gold, pickup log and minimap.
4. Attack, special, dash, town portal and both potions work from keyboard and buttons. Confirm no click repeats across ticks; depleted mana/potions and death disable buttons.
5. Rebind actions, including a mouse button and long names. Clicking Bag must not also attack/use a potion; arrows and Space must still move/jump after UI clicks.
6. Open inventory, shop, smithy, waystone, world map, controls and Esc menu. Verify equip/buy/sell/upgrade/travel, key capture and close/return behavior. Front menus retain their own skin.
7. Traverse maps, toggle minimap, find a secret and encounter a boss. Check that hidden routes stay hidden and the boss/death overlays remain readable.
8. Host and join a second player; verify HUD button inputs reach the host, values update from snapshots and Copy Join Code copies the active code.
9. Save, return to heroes, load another hero and stop Play Mode. Confirm no duplicated Canvas/EventSystem or leftover activity/queued input.

No balance, save format, map definitions or network protocol changes are included.
