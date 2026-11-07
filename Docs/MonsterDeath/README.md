# AshenVale monster death animations

Eight four-frame defeat sequences based on the sprites currently in AshenVale. Each plays once through the existing monster death renderer, which fades the sprite out over 0.45 seconds.

| Monster | Defeat motion |
| --- | --- |
| Hedge Shrew | Recoils, loses its footing, and curls up |
| Thornback Hedgehog | Tucks its snout and paws into its spines |
| Ash Sprout | Bends at the stem and wilts |
| Leaf Beetle | Tips over onto its shell |
| Mossling | Sags into a low mound of leaves |
| Ember Fox | Folds to the ground as its tail flame dims |
| Moss Crawler | Tucks its legs and curls into its mossy shell |
| Bramble Boar | Staggers, drops to its knees, and rolls onto its side |

## Install

1. Exit Play Mode.
2. Extract this archive outside your Unity project. Merge the included **Assets** folder into your project root.
3. Let Unity import the sprites, then defeat one of the eight monsters in Play Mode.

The existing importer sets Sprite (Single), 100 pixels per unit, and a bottom-center pivot. The game loads the numbered PNGs from `Assets/Resources/Art/Monsters/<monster_key>/die/` in filename order. The package contains new art only and requires no menu installer or Animator setup.

The preview shows the exported sequences at the game's approximate timing. The artwork was generated from each monster’s current idle sprite as a design reference. Source atlases were mechanically sliced, scaled once per monster, and aligned to the idle sprite’s floor contact. Pose drawings can differ slightly from the live artwork. Review the idle-to-death transition and floor contact at normal gameplay zoom. Unity playback still needs verification.

## Preview and art direction

**Frame_Comparison.png** shows the current idle pose followed by the four new frames, at their exported pixel scale. **Playback_Preview.gif** shows the death fade beside each idle reference.

The two atlases were generated with the built-in image generator using the current monsters as visual references. The prompts called for four right-facing, grounded poses per monster, with wide transparent gutters and a non-graphic collapse, curl, wilt, or roll suited to each design. Export checks verified four distinct transparent frames per enemy, equal canvas sizes within each sequence, and floor contact within one pixel of the idle reference.
