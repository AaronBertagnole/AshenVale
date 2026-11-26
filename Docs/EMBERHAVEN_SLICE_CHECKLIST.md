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
- [x] New Dragon Knight, no debug panel, Emberhaven to Thornmother's death. Note everything that is wrong, ugly, confusing or boring. (Done 2026-11-25; notes below.)
- [x] Same with the Ranger. (Done 2026-11-25.)
- [ ] Same with the Rimecaller.
- [ ] Send the notes (a list or clips). Claude sorts them into: bug, feel, balance, art, missing.

### 2. Fix what the play-through found

Knight run, 2026-11-25. Pushed in 342e025 and after; confirmed by the owner 2026-11-25:
- [x] Return portal shows only while one is open, labelled with the map it leads back to.
- [x] Waystone: no extra "[E] Open waystone travel" prompt.
- [x] Damage number on the killing blow.
- [x] EXP bar: taller, numbers and percent on it; "XP" renamed "EXP".
- [x] Monsters pace under a hero on a ladder or ledge instead of standing still.
- [x] Fewer Rare and Epic drops early (5% / 1% at level 1, rising to 20% / 5% by about level 30).
- [x] No moving while prone.
- [x] Ladder climb smooth (no resizing or stepping). Fixed without a clip; needs the owner's eye.
- [x] Class select: no flame icon on the highlighted choice.
- [x] Thornmother tougher: health 2600 to 3900, damage 26 to 33.
- [x] Potion prices rise with level (HP 30 at level 1, 130 at level 11; MP 37 and 157), and the merchant sells x1 / x5 / x10. (Pushed after 342e025.)
- [x] No second prompt over the hero at the merchant or blacksmith; the label over each stand is enough.

Ranger run, 2026-11-25, confirmed by the owner 2026-11-25:
- [x] Ground monsters chase across the whole floor (they used to stop 10 units from where they spawned), then walk home.
- [x] Knight less stiff: planted only until the blow lands plus 0.1s (was the whole swing: 0.46s basic, 0.78s Dragon Strike); moving, jumping or leaping then cuts the swing short. Attack rate unchanged.
- [x] Ranger arrows leave at the height of the nocked arrow (0.9 units above her feet, was 1.1).
- [x] Dragon Strike steps forward 1.2 units (was 0.55).
- [x] Holding the attack or skill key keeps attacking (all heroes). The movement ability still needs a press each time.
- [x] One damage number per hit (the killing-blow number was also appearing on hits the monster survived).

Rimecaller run, 2026-11-25, pushed and waiting for the owner to confirm:
- [ ] Stormcall lands on the nearest monster in front of her (up to 5 units, including one right beside her) and follows it between strikes. It used to strike a fixed spot 2.6 units ahead and miss anything nearer, farther, or walking.

Knight run, still open:
- [ ] Emberhaven painting: two flaws in the front layer (a garbled hanging sign under the balcony, a mint cloth scrap at the left edge). Needs a repaint of those spots; an automatic patch looked worse.
- [ ] Merchants and the blacksmith as characters, not only labels. Emberhaven's two are in (2026-11-25; enlarged to 2.0 and 1.7 units and the blacksmith turned to face left after the first look; waiting for the owner to confirm). Cinderford's still need art (see 4).
- [ ] Hero strength against Thornmother: wait for the Ranger and Rimecaller runs, then tune in 5.
- [ ] Class-select screen redesign beyond the flame icon: parked (title menu restyle).

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
- [ ] Merchant and Blacksmith for the two towns. Emberhaven: done, to confirm. Cinderford: not yet (file names `merchant_cinderford`, `smithy_cinderford`).
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
