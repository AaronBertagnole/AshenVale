# AshenVale — Hero Polish Update 2

Install this on the project that already has the Rimecaller prototype. This is a targeted update against the current main-branch scripts fetched November 6, 2026. It preserves the newer rule that taking a hit does not cancel a cast.

## Install
1. Exit Play Mode. Keep your current working git commit.
2. Extract the ZIP outside the project. Merge its **Assets** folder into your project root.
3. Wait for import, then choose **AshenVale > Hero polish > Install update**.
4. Wait for Unity compilation, then choose **AshenVale > Hero polish > Run checks**.
5. Enter Play Mode and try existing characters. No new save is required.

If the installer reports a source mismatch, it stops before changing any scripts and names the file. Send that file for adaptation. Do not replace your file with an old copy. Original scripts are backed up in **HeroPolishBackups/<timestamp>/**, outside Assets. Re-running an installed update does nothing.

## What changed

| Issue | Update |
| --- | --- |
| Character select too fast | Preview idle slows from 5 to 1 frame/second. Gameplay idle slows from 5 to 1.5. |
| Rimecaller looks too small | Larger artwork inside the same 128px canvas, about 12% larger; closely matched head-to-boot height across all three heroes. No character collider or world scale change. New art also uses its measured boot offset on procedural maps. |
| Orb misses short enemies | Acquires a nearby body ahead, lowers the release point for tiny enemies, and gently adjusts height during flight. |
| Climbing looks wrong | Four distinct back-view poses per hero, selected by ladder height. Stopping holds the pose. Constant body scale replaces fitting old hand coordinates to new art. |
| Other classes look dated | Refreshed red/black/gold Dragon Knight and green/gold Elven Ranger in the Rimecaller rendering style. |

The art set has 73 transparent 128x128 individual sprites, including the refreshed mage, Knight and Ranger movement, attacks, climb, prone, hurt and death. The sprites retain center pivots, 128 PPU and fixed canvases. Idle contact rows are aligned at pixel114. **Hero_Lineup.png** shows the actual imported idle canvases side by side. Artwork is the next animation pass, with compact pose counts (four walk/climb poses, two-frame Knight attacks); it is not a finished high-frame-count animation set. Knight Storm/Dragon Strike reuses the new thrust pair with existing effects.

All original character images remain available. Outside Play Mode, **AshenVale > Hero polish > Use original Knight and Ranger art** switches those two heroes back for comparison. **Use refreshed Knight and Ranger art** switches them to the new set. These choices persist into builds. Mage fixes remain active in either mode. A script/domain reload applies the art choice.

## Orb behavior
- Looks for a living enemy within 7 units ahead and a 1.35-unit vertical band from the orb, keeping the closest forward body as its aim target.
- Initial release can move down/up by at most .65 unit; in flight, vertical steering is limited to 4.5 units/second while horizontal speed remains 9.
- Does not turn around for an enemy behind it or seek a far upper platform.
- Uses a two-dimensional swept collision against expanded monster bodies, catching the first body touched even during a long tick.
- If its target dies or leaves the forward aiming band, it can choose another nearby enemy ahead; otherwise it continues forward. It still expires normally.
- Still one impact, one damaged target, no splash. Spell damage, costs, cooldowns, Stormcall and Froststep tuning are unchanged.

Like the existing ranged system, this does not add projectile-versus-platform collision. Aiming is a vertical-band heuristic, not line-of-sight pathfinding through arbitrary geometry. Host simulation owns aim and damage; existing projectile position snapshots carry the result to clients, without a packet-format change. Update all peers together.

## Validation
Local checks passed: nine C# syntax trees, ten exact patch operations, Windows CRLF compatibility, late-conflict preflight without input mutation, preservation of current cast interruption behavior, sprite alpha/dimensions, four distinct climb frames per hero, and shared idle contact rows.

**Unity and a C# compiler are not available in this environment. Compilation, actual editor smoke checks, animation feel and host/client behavior have not been run here.** The supplied Unity checks use a separate World and test aiming at the smallest enemy, exclusions behind/above, first-body collision during a long tick, missing-target behavior, class IDs, asset coverage and climb phase selection. They do not change your saves.

Suggested in-game checks: preview each hero; compare idle height at the same location; fire at Leaf Beetles, Hedge Shrews and other small mobs; fire past two lined-up monsters; climb up/down and stop midway; jump, prone and attack with Knight/Ranger; then join a second client. If something differs, send the Console error and a clip of that action.

## Sources and art
Only four existing scripts receive narrow edits: **FrontMenu.cs**, **Sprites.cs**, **WorldView.cs**, **World.Rimecaller.cs**. New helpers install under **Assets/Scripts/HeroPolish**. Art lives in **Assets/Resources/Heroes/StyleRefresh** and resolves through the existing sprite loader, falling back to original clips if a refreshed clip is absent. Hero identities, save class indices, simulation movement, maps and background framing are unchanged.

Artwork was generated with the built-in image generator, using the approved Rimecaller concept as the style reference, then mechanically sliced, uniformly scaled and translated onto 128px canvases. No independent per-frame stretching. Source atlases and the prompt direction are included under **ArtSources**.

## Undo the code update
Close Unity. Restore the four scripts from the timestamped backup to matching paths in your project. Remove **Assets/Scripts/HeroPolish**, **Assets/Resources/Heroes/StyleRefresh**, **Assets/Editor/HeroPolishInstaller.cs**, **Assets/Editor/HeroPolishSource**, **Assets/Resources/HeroPolish/ClassicStyle.txt** if present, and **ProjectSettings/HeroPolishInstalled.txt**. Remove the corresponding .meta files for removed assets. Reopen Unity. The original Rimecaller prototype remains installed.
