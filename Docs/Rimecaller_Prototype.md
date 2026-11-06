# Rimecaller prototype (installed by hand, not through the pack installer)

Applied to the repo directly: the 28 source edits, the new scripts in `Assets/Scripts/Rimecaller/`, the sprite importer and smoke checks in `Assets/Editor/Rimecaller/` (run **AshenVale > Rimecaller > Run smoke checks**), and the art. Differences from the pack: the HUD edit went into `UI/Hud/CanvasHud.cs`; a hit no longer cancels a pending cast (matches Knight/Ranger); the main importer skips Rimecaller art. Original pack notes follow.


Built against the current main-branch class, simulation, menu, HUD and networking scripts. This is an installable prototype, not a Unity-tested release. Existing Knight/Ranger IDs remain 0/1; Rimecaller is appended at 2. No maps, backgrounds, saves, monster definitions or existing class artwork are replaced.

## Install
1. Exit Unity Play Mode. Keep a clean git commit of your working project.
2. Extract this ZIP outside the project, then copy its **Assets** folder into your project root (alongside your existing Assets folder). Merge folders.
3. Wait for Unity to finish importing. Choose **AshenVale > Rimecaller > Install prototype**.
4. Wait for compilation. Choose **AshenVale > Rimecaller > Run smoke checks**. Check the Console.
5. Create a new character, choose **Rimecaller** (third card / key 3), and enter a field map.

The installer checks every required code fragment in memory before writing any scripts. If your local files differ, it stops and names the file. Send that file for adaptation. Do not overwrite it with an older copy. Backups go into **RimecallerBackups/<timestamp>/** outside Assets. Re-running an installed version makes no changes.

## Controls and starting values
Uses your existing Attack, Skill and Dash bindings, including their rebound keys.

| Input | Ability | Prototype behavior |
| --- | --- | --- |
| Attack | Frozen Orb | 0 MP, 0.6s cooldown; 0.18s windup; projectile stops at first target and shatters. No splash damage. |
| Skill | Stormcall | 24 MP, 1.8s cooldown; three hits at the same location 2.6 units ahead, 0.22s apart. Small area of effect. |
| Dash | Froststep | 8 MP, 1.2s cooldown; instant horizontal teleport 3.2 units in facing direction, bounded by map edges. No dash motion or added invulnerability. |

85 base HP, 135 base MP, 22 base attack; existing level and equipment attack bonuses apply. Each storm strike uses attack + 5. Balance values live in installed **Assets/Scripts/Rimecaller/RimecallerRules.cs**. Pending spells cancel on death, map change, climbing or a hit interruption; mana is spent when casting starts. Froststep is blocked during casting.

## Included art
Transparent 128x128 individual sprites for idle, four-pose walk, jump, fall, orb cast, storm cast, hurt, death, climb and prone; four independent effects. Fixed canvas and uniform scale prevent per-frame resizing. The slow four-pose walk and single-pose climb/prone are first-pass animation assets and need in-game review. Orb and lightning impact artwork is animated through lifetime fading; this is not a fully hand-animated VFX sequence.

Character design preserved: silver-blue hair, ivory collar, navy coat, silver embroidery and cyan crystal. The uncut pose sheet is included for reference. Artwork generated with the built-in image generator, then mechanically cropped/padded. Prompt direction: matching Rimecaller identity, right-facing side-scroller poses, transparent 4x4 atlas, alternating relaxed walk, casting/jump/fall/hurt/death/climb/prone; separate cyan orb, ice burst, icy mist and white-violet lightning atlas.

## Checks and limits
Outside Unity: all changed/new C# files passed syntax parsing; all 28 replacements matched uniquely against fetched source; sprite alpha and dimensions checked. **No Unity editor or C# compiler was available here**, so runtime compilation, actual smoke checks, animation feel and online play still need your Unity test. The supplied editor checks cover class IDs, windup, cooldown, nearest-only swept orb collision, mana, three fixed-location strikes, death cancellation, teleport boundaries and asset loading. They use an isolated World and do not change saves.

For multiplayer, update every peer to this same build. Event IDs are appended and existing packet fields are reused; an old build cannot render the new hero/effects correctly.

Test: walk both directions; jump/fall/climb/prone; orb through two nearby monsters; storm against a group; blink at a ledge and map boundary; save/load the third class; confirm Knight/Ranger still select and fight; then test a host/client session. New animation polish, sound effects, vertical teleport aiming and frost slow are not included.

## Undo
Close Unity. Restore the original files from the timestamped backup to their matching project paths. Remove **Assets/Scripts/Rimecaller**, the two new Rimecaller Resources folders, **Assets/Editor/RimecallerInstaller.cs**, **Assets/Editor/RimecallerSource**, and **ProjectSettings/RimecallerInstalled.txt**. Remove corresponding .meta files for removed assets. Reopen Unity. Do not delete saves containing class 2; they require the class to remain installed for correct loading.
