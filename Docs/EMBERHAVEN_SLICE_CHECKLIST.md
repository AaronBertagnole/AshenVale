# AshenVale: the Emberhaven slice — checklist

Written 2026-11-24 at the owner's request. This is the plan of record. Work goes down this list in order.

## The goal

One region finished to release quality: **Emberhaven, levels 1 to about 12, from the town to Thornmother's death.** Eleven maps, eighteen monsters, one boss, three heroes. When this slice is done it sets the standard for the other four regions.

## The drift rule (owner's instruction)

The owner asked to be told **no** when he drifts from this list, because he has been building whatever comes to mind first.

- If a request is in the Parking lot below, or is not on this list at all, Claude says **no** first, names the item that is currently open, and offers to add the idea to the Parking lot so it is not lost.
- Claude does not start off-list work on a polite "sure". The owner must say **"off-list, do it anyway"** to override. It is his game; the phrase just makes the detour a decision instead of a drift.
- Not drift: fixing a bug that blocks testing the slice, and anything the open item needs in order to be finished.
- One item is open at a time. An item is closed when the owner has seen it working in Unity, not when the code is pushed.
- Claude updates this file as items close.

## Where the slice stands today

- Art: every Emberhaven map has a painting and every monster a sprite, except the **Ash Wisp** (placeholder). Hollow Den still runs on the test-painting code.
- Sound: two music tracks (Emberhaven town, Thornmother's glade) and Thornmother's effects. Nothing else makes a sound.
- Untested in Unity by Claude: everything. Much of November's work has only been seen by the owner in passing.

## The list

### 1. Play it through (owner) — OPEN
- [ ] New Dragon Knight, no debug panel, Emberhaven to Thornmother's death. Note everything that is wrong, ugly, confusing or boring.
- [ ] Same with the Ranger.
- [ ] Same with the Rimecaller.
- [ ] Send the notes (a list or clips). Claude sorts them into: bug, feel, balance, art, missing.

### 2. Fix what the play-through found
- [ ] Every bug.
- [ ] Every "feel" note that is a code change.
- [ ] Known loose ends inside the region:
  - [ ] Den Shade: the new art is a four-legged beast that still flies. Decide: walks, or flies with different art.
  - [ ] Hollow Den off the test-painting code and onto the normal backdrop path.
  - [ ] Shale Cliffs ledges in the earthen-ledge style (marsh and den already are).
  - [ ] Floor-strip double image check on marsh, cliffs, den and glade (the Highspire fix).
  - [ ] Tall maps (Shale Cliffs): painting runs out at the top.

### 3. Sound
- [ ] Hero: basic attack, skill and movement ability for each of the three heroes; jump, land, hurt, death, level-up.
- [ ] Monsters: hit, death; the Marsh Toad's spit and puddle.
- [ ] World: item drop, pickup (gear, part, gold, potion), potion use, portal, waystone, ladder.
- [ ] Interface: window open and close, button, buy, sell, upgrade, rank up, error.
- [ ] Music for the field maps (one track can cover several maps).
- [ ] Mix pass: nothing drowns anything else at default volume.

### 4. Remaining art (owner paints, Claude integrates)
- [ ] Ash Wisp sprite sheet.
- [ ] Merchant and Blacksmith for the two towns (if the painted scenes do not already cover them).
- [ ] Thornmother: walk row, summon row, rooted rows for the new dryad art (they currently reuse other rows).
- [ ] Thornmother's effects repainted at final size, 8 to 12 frames, transparent background.

### 5. Balance, levels 1 to 12
- [ ] Time to each level feels right; no map is skipped or overstayed.
- [ ] Hits to kill and hits to die on each map, per hero.
- [ ] Class multipliers (Knight 1.2x dealt / 0.8x taken, ranged 0.9x) confirmed or changed.
- [ ] Ability rank numbers and unlock levels for the ranks reachable by level 12.
- [ ] Gold, potion and merchant prices: never starved, never swimming.
- [ ] Thornmother is beatable but not trivial by each hero at level 9 to 11.

### 6. Loot worth chasing (Emberhaven only)
- [ ] Gear stats that change how a hero plays, not only bigger numbers.
- [ ] One named item or small set from the region's ordinary monsters.
- [ ] A reason to revisit a map: a rare spawn or an elite version.
- [ ] Thornbound set reviewed against the above.

### 7. Hero identity
- [ ] Decide whether the choice-of-two upgrade at rank 5 is in the slice. If yes: design and build it for the abilities reachable by level 12.

### 8. Thornmother, final
- [ ] Her fight end to end with the final art, effects and sounds.
- [ ] Reset, enrage, sprout healing and ledge behaviour all confirmed in play.

### 9. The first ten minutes
- [ ] A new player learns to move, attack, pick up, open the ability window, use a waystone and find the first portal without being told outside the game.

### 10. Two players
- [ ] Host and one joiner play the slice start to finish. Fix what breaks (known: cooldowns do not show for the joiner).

### 11. Done
- [ ] A fresh hero, no debug panel, start to Thornmother's death, with nothing on the owner's "wrong" list.

## Parking lot (not now)

Left exactly as they are until the slice is done:

- The other four regions: maps, art, monsters. (Highspire's art is in; it stays as is.)
- The other bosses: their own fights, the static/dynamic split, giant sizing. The Rusted Colossus size test and Captain Marrow's walk and effects stay as they are.
- Mastery (level 30) skills and their animations.
- Equipped gear shown on hero bodies.
- A fourth class.
- Bestiary app changes.
- Interface colours, the floating key prompt, the title menu restyle.
- Moving to Unity's newer input system.
- Emberfields hills test branch.

New ideas go here with a date, so they are kept and not acted on.
