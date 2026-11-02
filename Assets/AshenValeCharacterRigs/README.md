# Ashen Vale — Ranger & Warrior Cutout Rig Prototype

First articulated prototypes based on the approved painted Elven Ranger and Dragon Knight designs. This pack keeps the same face/hair identity, palettes, leaf/dragon motifs, clothing and weapon designs as closely as practical while reconstructing hidden joint areas. It is an editable first pass, not a pixel-identical cutout or a complete replacement character controller.

## Walk revision 2 — upgrading an existing import

Both ranger and warrior now use independently pivoted feet, forward-bending knees, a planted stance and lifted recovery step, pelvis sway, and staggered shoulder/cape motion. The ranger has a lighter stride; the warrior a longer planted phase. Existing art and the original idle pose are preserved. Foot/calf sprites are non-destructive slices of the original atlas, not new paintings.

1. Overwrite **Assets/AshenValeCharacterRigs/Scripts** and **Editor** with this pack, preserving the supplied .meta files. Keep the supplied Art folder in place.
2. Run **Tools > Ashen Vale > Characters > Create Rig Preview** again. This creates new definitions, ankle joints, prefabs and baked clips in a new timestamped Generated folder.
3. Test both characters in that new preview scene. For gameplay, replace the old visual prefab with the newly generated matching prefab, retaining your visual scale/offset and movement driver.

**Replacing the script alone does not update an existing baked Walk clip or add ankle joints to an old prefab.** Keep older generated folders until the replacements work. `walkPlaybackSpeed` adjusts cadence (default 1); tune it against your movement speed to limit sliding. This is an in-place loop and does not match foot placement to terrain or measured player velocity.

## Included

- Approved high-resolution transparent master images, for comparison.
- Two transparent 4-column x 3-row parts atlases: twelve separate body/weapon pieces each.
- An additional long crimson warrior cape layer.
- Art importer and alpha-bound slicing/registration builder.
- A Transform-based articulated SpriteRenderer hierarchy for each character.
- Builder-generated looping **Idle** and **Walk** animation clips and Animator controllers, with a Walking bool and short transitions.
- Builder-generated prefabs, definitions with Sprite subassets, and a side-by-side Unity preview scene.
- AssemblyPreview.png and IdleWalkPreview.gif rendered from the atlas/skeleton equations in an independent renderer. They are art/pose previews, **not Unity screenshots or proof of Unity execution**.

The Unity assets (prefabs, clips, controllers, scene and definitions) are created by the builder INSIDE your editor; they are not already serialized in the ZIP.

## Unity setup

1. Copy **Assets/AshenValeCharacterRigs** from this ZIP into your Unity project's **Assets** directory. Keep the folder name/path unchanged.
2. Let Unity compile and import. No additional Unity packages are required.
3. Choose **Tools > Ashen Vale > Characters > Create Rig Preview**.
4. Save your current scene when prompted. A new preview scene opens. Outputs are placed in a new timestamped folder under **Assets/AshenValeCharacterRigs/Generated**; earlier generated folders are preserved.
5. Press Play. Both rigs breathe in Idle. Toggle **Walk cycle** for the gait, **Face the other direction** to mirror them, and **Show approved design references** for the original art.
6. Inspect neck, shoulders, elbows, wrists, belt, knees, boots, bow grip and spear alignment. Adjust transforms and definition heights as needed before using these in gameplay.

Target: Unity 2022.3 LTS / Unity 6, including your Unity 6000.3 project. There is no Unity editor or C# compiler available in the creation environment, so import, compilation, Animator playback and actual renderer compatibility still require testing in Unity.

## How the rig works

The parts atlas has one piece per cell, top-left to bottom-right:

| Row | Column 1 | Column 2 | Column 3 | Column 4 |
| --- | --- | --- | --- | --- |
| 1 | Head/hair | Torso/cloak/quiver | Pelvis/belt/skirt | Near upper arm |
| 2 | Near forearm/hand | Far upper arm | Far forearm/hand | Near thigh |
| 3 | Near shin/boot | Far thigh | Far shin/boot | Weapon |

The builder detects nontransparent bounds inside each cell and registers Sprite pivots at the intended joints. Ranger head registration accounts for the long ponytail; the bow grip is offset from the bow's geometric center. The warrior has a separate long cape behind his body. Overlapping joint ends limit gaps during rotation. Some small pose/detail differences from the master remain, including neutral hand shapes and the reconstructed joint regions.

This is a **cutout rig**, not a weighted skeletal skin or frame-by-frame sprite sheet. Individual limbs rotate around joints. Hands are attached to forearm pieces; walking boots have independent ankle joints. Idle uses the original whole shin/boot pieces. The ranger's ponytail stays with the head and quiver stays with the torso. The cape has independent sway. Idle has a 2.4-second loop; Walk has a 0.8-second loop. Walking uses a two-bone leg solver and sampled foot silhouettes to follow a flat stance/recovery path. There is no terrain collision, root-motion locomotion or gameplay controller.

Keeping animation on this same set of artwork avoids a new redraw of the character on every frame. Final bow drawing, spear thrusts, jumping, climbing, hit reactions and death poses still need dedicated animation/art passes.

## Driving it later

`CutoutCharacterRig.SetMovement(signedSpeed)` sets Walking and facing. You can also set `walking` and `facingRight` directly. The Animator handles the generated clips when present; without one, the component can apply its procedural pose curves. Root visual scale is left to your game.

Your current game draws characters through WorldView and sprite frame arrays. These rigs are **not automatically wired into that renderer** and this pack does not alter your current GitHub branch, player physics, combat, saves or multiplayer state. After visual approval, the integration step is to instantiate the matching rig as the visual for each PlayerState, drive it from the existing motion/attack states and preserve the simulation/colliders. Start with a root visual scale around 0.45–0.6, then compare feet/head positions against your current character height.

Avoid scaling physics colliders along with these visual-only rigs. The preview scene has no player colliders or navigation. Imported atlases remain readable so the builder can calculate alpha bounds; optimize texture memory after the rig is finalized.

## Checks and remaining work

Checked here: all atlas cells contain distinct nontransparent art; PNG alpha/dimensions; source delimiter structure; joint registration and assembled poses in an independent canvas renderer; idle/walk pose loop endpoints; ZIP integrity. The initial neck and weapon registration was adjusted after assembly inspection, and the long cape was added to preserve the warrior's concept silhouette.

Still required in Unity: clean compile; correct imported Sprite references; generated clips/controllers/prefabs; Idle/Walk and facing transitions; feet and joint clearance over a complete cycle; material support in your render pipeline; comparison at actual gameplay size. The assembly/GIF preview is a first-pass visual check and cannot replace those engine tests.
