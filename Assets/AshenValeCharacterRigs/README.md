# Ashen Vale — Ranger & Warrior Cutout Rig Prototype

First articulated prototypes based on the approved painted Elven Ranger and Dragon Knight designs. This pack keeps the same face/hair identity, palettes, leaf/dragon motifs, clothing and weapon designs as closely as practical while reconstructing hidden joint areas. It is an editable first pass, not a pixel-identical cutout or a complete replacement character controller.

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

This is a **cutout rig**, not a weighted skeletal skin or frame-by-frame sprite sheet. Individual limbs rotate around joints. Hands and boots are attached to forearm/shin pieces. The ranger's ponytail stays with the head and quiver stays with the torso. The cape has independent sway. Idle has a 2.4-second loop; Walk has a 0.8-second loop. Foot-silhouette sampling adjusts body height during the gait, but there is no collision, IK system, root-motion locomotion or gameplay controller.

Keeping animation on this same set of artwork avoids a new redraw of the character on every frame. Final bow drawing, spear thrusts, jumping, climbing, hit reactions and death poses still need dedicated animation/art passes.

## Driving it later

`CutoutCharacterRig.SetMovement(signedSpeed)` sets Walking and facing. You can also set `walking` and `facingRight` directly. The Animator handles the generated clips when present; without one, the component can apply its procedural pose curves. Root visual scale is left to your game.

Your current game draws characters through WorldView and sprite frame arrays. These rigs are **not automatically wired into that renderer** and this pack does not alter your current GitHub branch, player physics, combat, saves or multiplayer state. After visual approval, the integration step is to instantiate the matching rig as the visual for each PlayerState, drive it from the existing motion/attack states and preserve the simulation/colliders. Start with a root visual scale around 0.45–0.6, then compare feet/head positions against your current character height.

Avoid scaling physics colliders along with these visual-only rigs. The preview scene has no player colliders or navigation. Imported atlases remain readable so the builder can calculate alpha bounds; optimize texture memory after the rig is finalized.

## Checks and remaining work

Checked here: all atlas cells contain distinct nontransparent art; PNG alpha/dimensions; source delimiter structure; joint registration and assembled poses in an independent canvas renderer; idle/walk pose loop endpoints; ZIP integrity. The initial neck and weapon registration was adjusted after assembly inspection, and the long cape was added to preserve the warrior's concept silhouette.

Still required in Unity: clean compile; correct imported Sprite references; generated clips/controllers/prefabs; Idle/Walk and facing transitions; feet and joint clearance over a complete cycle; material support in your render pipeline; comparison at actual gameplay size. The assembly/GIF preview is a first-pass visual check and cannot replace those engine tests.
