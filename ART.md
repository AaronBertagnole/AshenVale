# AshenVale art guide

The game draws placeholder shapes for everything until real art exists. Drop a PNG into the right folder and it
replaces that one placeholder the next time you press Play. Nothing else needs changing: the importer
(`Assets/Editor/AshenValeSpriteImporter.cs`) sets the size, filtering and pivot automatically.

All art must be original or properly licensed. MapleStory is the style target (soft, bright, painted, cute), but
its actual sprites and backgrounds belong to Nexon, so nothing traced or ripped from it.

## Scale

| Thing | Size |
|---|---|
| 1 world unit (one map cell) | **100 px** |
| Camera view | 10.8 units tall (exactly 1080 px on a 1080p screen, so art shows 1:1) |
| Hero | about 1.65 units tall (165 px) |
| Gap between platform levels | usually 2 units (200 px) |

## Style

- Soft painted look: 2 to 3 tones per colour plus a soft highlight, no harsh noise.
- Coloured outlines (dark version of the fill, 2 to 3 px), not pure black.
- Round, chunky, friendly shapes; monsters should be readable as silhouettes.
- Backgrounds get lighter, bluer and lower in contrast the further back they are, so the play area stands out.
- Keep each theme to a small palette so platforms, monsters and heroes read clearly against it.

## Folders

Everything lives under `Assets/Resources/Art/`.

### Painted scenes: `Art/Scenes/<map>.png`

One finished painting for a whole map (usually a town), drawn behind everything in place of the theme layers
below. It is stretched to the map's width, and `MapDef.sceneFloor` says how far up the picture the street is
(the share of the picture's height below the walking line), so feet land on it. The map's layout is then lined up with
the painting: shops, waystone and portals go where the picture shows them (`Assets/Scripts/Data/MapDefs.cs`).

| File | Map | Picture | Notes |
|---|---|---|---|
| `emberhaven.png` | Emberhaven | 2172 x 724, street 116 px above the bottom | merchant at the left house, waystone at the arch, smithy at the forge |
| `cinderford.png` | Cinderford | 4140 x 724, street 144 px above the bottom | two pictures joined: west gate (Kingsroad), potion shop and outfitter (both merchants), smithy, waystone, east gate (Ashen Marsh) |

Both scenes are see-through fronts with a distant `_back.png` behind them (see "Two layers" below).

Emberhaven is 38 cells wide, so its picture shows at about 57 px per unit; Cinderford keeps the same scale at
72 cells. A version twice the size (same framing) would look sharp on a 1080p screen: drop it in with the same name.

A bigger town can be made from several pictures of the same street: `Tools/stitch_pictures.py` overlaps them,
cuts a seam where they look most alike and blends across it. Cinderford was made with

    python Tools/stitch_pictures.py west.png east.png 200 Assets/Resources/Art/Scenes/cinderford.png 1

(200 px overlap; the last number crops one row off the top so both sides are multiples of 4, which Unity needs to
compress the picture). Put `$` on every shop that should open the merchant window. See-through fronts join the
same way (the current Cinderford front: the pack's arrival and plaza fronts, 198 px overlap, 1 row cropped).

### Two layers: a see-through front and the distance behind it

Any painted scene or backdrop can come as two pictures: the front (the usual file name, transparent where the
distance shows through) and `<same name>_back.png` (the sky, hills and far-off town, opaque, with whatever the
front hides painted in). The distance slides more slowly than the front as you walk, which gives depth. Both
use the same size and framing (2172 x 724 for most; Cinderford's front is two fronts joined, 4140 x 724, with one
distance behind the whole street). How far the distance slides is `backTravel` in `Assets/Scripts/Data/MapArt.cs`.

Every painted map has both layers now (from the owner's "All Maps" pack). Its pictures were redrawn as layers,
so they differ a little from the first versions; ground and platform tiles are still the ones cut before, which
match closely and cover the painted path anyway.

### Living extras: `Assets/Scripts/Data/MapArt.cs`

Per map, in picture pixels (x from the left, y from the top) so they line up with the painting:
- **Effects**, animated lights drawn with `Assets/Resources/Shaders/MapEffect.shader`: portal ripples (waystones,
  the Mossgate arch), flowing water (waterfalls, the Mosswood cascade, the Kingsroad river), drifting mist and fire
  flicker (forges, the Emberhaven waystone flame). An effect marked distant rides on the `_back` picture.
  Glows and fire add light (they show over darker paint); water and mist are painted over the picture in bands
  darker and paler than their tint, so they read over bright paint too. Each has a `strength` (1 = normal).
- **Specks**: golden motes everywhere, falling leaves in Emberhaven / Emberfields / Amberleaf Trail, pollen on
  Kingsroad, ash in Ashen Marsh, chimney smoke and forge or waystone sparks at their spots.
To move one, find its spot on the picture in an image editor and change its numbers.

### Painted backdrops: `Art/Backdrops/<map>.png`

For a map wider than the picture (most hunting maps). The picture is shown `MapDef.backdropWidth` units wide behind
everything and slides more slowly than the map, always filling the screen: its left edge shows at the map's left
end and its right edge at the right end. At the lowest camera its painted path (`backdropFloor`) sits on the ground.
It replaces the theme's sky / far / mid; ground and platforms are still drawn in front.

| File | Map | Picture | Shown |
|---|---|---|---|
| `emberfields.png` | Emberfields (and Amberleaf Trail, mirrored, until it has its own) | 2172 x 724, path 116 px above the bottom | 42 units wide |
| `mossgate_hollow.png` | Mossgate Hollow | 2172 x 724, path 116 px above the bottom | 42 units wide |
| `mosswood.png` | Mosswood | 2172 x 724, path 130 px above the bottom (set in MapDefs) | 42 units wide |
| `kingsroad.png` | Kingsroad | 2172 x 724, path 140 px above the bottom (set in MapDefs) | 42 units wide |

Each also has its distant `_back.png` (see "Two layers" above).

**All 43 maps now have a picture (2026-11-04, the owner's "43 Backgrounds" set).** The 37 new ones are stored
as 2172 x 1000: the pack's 2172 x 724 painting with 276 px of soft sky added above it, and they are shown **28 units
wide** (not 42) because their props were painted large: at 42 the hero was waist-high to a barrel. The extra sky is
what lets a narrower picture still fill the view. Each is a single layer (no `_back`). The walking line is the TOP
of the painted path lane (the hero stands on the lane, feet a third of the way down it) and is set per map in its
`WithBackdrop` call. Emberhaven and Cinderford keep their earlier two-layer scenes, Emberfields, Mossgate Hollow,
Mosswood and Kingsroad their earlier two-layer backdrops (42 units wide, as before).

| File | Map | Walking line (lane top) |
|---|---|---|
| `amberleaf_trail.png` | AmberleafTrail | row 825 of 1000 (175 px above the bottom) |
| `archmage_sanctum.png` | ArchmageSanctum | row 825 of 1000 (175 px above the bottom) |
| `ashen_marsh.png` | AshenMarsh | row 839 of 1000 (161 px above the bottom) |
| `black_galleon.png` | BlackGalleon | row 878 of 1000 (122 px above the bottom) |
| `black_ziggurat.png` | BlackZiggurat | row 859 of 1000 (141 px above the bottom) |
| `brackwater_shore.png` | BrackwaterShore | row 846 of 1000 (154 px above the bottom) |
| `captains_hold.png` | CaptainsHold | row 853 of 1000 (147 px above the bottom) |
| `colossus_vault.png` | ColossusVault | row 834 of 1000 (166 px above the bottom) |
| `crystal_hollow.png` | CrystalHollow | row 870 of 1000 (130 px above the bottom) |
| `deep_forge.png` | DeepForge | row 824 of 1000 (176 px above the bottom) |
| `eldermere.png` | Eldermere | row 821 of 1000 (179 px above the bottom) |
| `forgotten_stair.png` | ForgottenStair | row 843 of 1000 (157 px above the bottom) |
| `frostpine_pass.png` | FrostpinePass | row 847 of 1000 (153 px above the bottom) |
| `gallows_point.png` | GallowsPoint | row 847 of 1000 (153 px above the bottom) |
| `glacier_steps.png` | GlacierSteps | row 846 of 1000 (154 px above the bottom) |
| `glimmerwood.png` | Glimmerwood | row 841 of 1000 (159 px above the bottom) |
| `hall_of_idols.png` | HallOfIdols | row 888 of 1000 (112 px above the bottom) |
| `highspire.png` | Highspire | row 895 of 1000 (105 px above the bottom) |
| `hollow_den.png` | HollowDen | row 871 of 1000 (129 px above the bottom) |
| `hollow_library.png` | HollowLibrary | row 887 of 1000 (113 px above the bottom) |
| `ironvein_mines.png` | IronveinMines | row 827 of 1000 (173 px above the bottom) |
| `lastlight.png` | Lastlight | row 835 of 1000 (165 px above the bottom) |
| `moonwell.png` | Moonwell | row 826 of 1000 (174 px above the bottom) |
| `oubliette.png` | Oubliette | row 876 of 1000 (124 px above the bottom) |
| `owlkeep.png` | Owlkeep | row 829 of 1000 (171 px above the bottom) |
| `saltmarrow.png` | Saltmarrow | row 854 of 1000 (146 px above the bottom) |
| `shale_cliffs.png` | ShaleCliffs | row 814 of 1000 (186 px above the bottom) |
| `sirens_grotto.png` | SirensGrotto | row 818 of 1000 (182 px above the bottom) |
| `sleepers_throne.png` | SleepersThrone | row 891 of 1000 (109 px above the bottom) |
| `smugglers_cove.png` | SmugglersCove | row 869 of 1000 (131 px above the bottom) |
| `spellbroken_glade.png` | SpellbrokenGlade | row 825 of 1000 (175 px above the bottom) |
| `sunken_avenue.png` | SunkenAvenue | row 825 of 1000 (175 px above the bottom) |
| `thistledown_trail.png` | ThistledownTrail | row 823 of 1000 (177 px above the bottom) |
| `thorn_glade.png` | ThornGlade | row 825 of 1000 (175 px above the bottom) |
| `weeping_catacombs.png` | WeepingCatacombs | row 826 of 1000 (174 px above the bottom) |
| `windward_ridge.png` | WindwardRidge | row 827 of 1000 (173 px above the bottom) |
| `wreckers_reef.png` | WreckersReef | row 823 of 1000 (177 px above the bottom) |

### Map themes: `Art/Themes/<theme>/`

Themes: `town` (Emberhaven), `autumn` (Emberfields, Amberleaf Trail), `mossgate` (Mossgate Hollow), `forest` (Mosswood),
`cave` (Hollow Den), `road` (Kingsroad), `rivertown` (Cinderford), `marsh` (Ashen Marsh); `meadow` is spare.

Each map with a painted picture has its ground and platforms cut from that picture (row = walking line):

    python Tools/make_ground_tiles.py Assets/Resources/Art/Backdrops/emberfields.png 608 42 autumn
    python Tools/make_ground_tiles.py Assets/Resources/Art/Backdrops/mossgate_hollow.png 608 42 mossgate
    python Tools/make_ground_tiles.py Assets/Resources/Art/Backdrops/mosswood.png 594 42 forest
    python Tools/make_ground_tiles.py Assets/Resources/Art/Backdrops/kingsroad.png 584 42 road

Every other theme (cave, marsh, cliffs, snow, hold, mine, forge, crystal, beach, port, reef, cove, galleon, gallows,
deepwood, wizardtown, library, moonwell, ruins, ruintown, catacomb, ziggurat) was cut the same way from one of its
maps' pictures at 28 units (`make_ground_tiles.py <padded picture> <lane top row> 28 <theme>`; the row is that
map's walking line in the table above). Where the line sits low in the picture the tool extends `ground.png`
downward with mirrored copies so the wall is at least 2 units tall. These themes have no `ground_back.png`: with the
walking line at the lane top the strip the tool cuts above it is just the base of the scenery, which showed as a
blurred band in front of the backdrop, so it was deleted. Painted maps (scene or backdrop) draw no placeholder shop,
waystone or smithy shapes; the labels over them still show. A theme with a
`ground.png` is drawn .16 units high (platforms .10) automatically, since the tiles carry a strip of path above the
ledge line; a theme can still set its own `groundSurface` / `platformSurface`.

`Tools/recolor_theme.py` can make a theme from another by recolouring its leaves.
If a picture's path is not 116 px above the bottom, give the map its own `MapLayout.WithBackdrop(...)` line in MapDefs.

| File | What it is | Suggested size | Lines up by |
|---|---|---|---|
| `sky.png` | Fills the screen behind everything, fixed to the camera | 2560 x 1440 | centre (scaled to cover) |
| `far.png` | Distant hills, mountains, trees. Tiles sideways; transparent above the silhouettes | 2048 x 1000 | bottom edge, about at ground level |
| `mid.png` | Nearer trees, houses, rocks. Tiles sideways; transparent above | 2048 x 700 | bottom edge, about at ground level |
| `ground.png` | The ground strip. Tiles sideways; its **top edge is the walking surface** (a theme can set `groundSurface` in MapScenery when the tile shows a strip of path above the edge, so feet land on the edge) | 256 x 160 | top edge |
| `ground_back.png` | Optional strip just behind your feet, fading out at its top, so the ground blends into a backdrop | 256 x 70 | bottom edge, on the walking surface |
| `platform_mid.png` | Middle of a platform, tiles sideways; **top edge is the walking surface** (`platformSurface`, as for the ground) | 100 x 70 | top edge |
| `platform_left.png`, `platform_right.png` | Optional rounded ends for platforms | 100 x 70 each | top edge |
| `ladder.png` | One section of ladder (or rope), tiles upward | 60 x 100 | bottom edge |

`far` moves slowly behind the camera and `mid` a bit faster (parallax), so they should be seamless left to right.

### Monsters: `Art/Monsters/<key>/<animation>/00.png, 01.png, ...`

Frames are numbered from `00`, play at 8 per second and are drawn **facing right** (the game mirrors them).
Put the monster's feet at the bottom centre of the image (the bottom edge of the picture sits on the floor, whatever
pivot Unity imports it with). `idle` is required to switch a monster to art;
the others are optional (`idle` is used in their place).

| Animation | Used when |
|---|---|
| `idle` | standing |
| `move` | walking or chasing |
| `hurt` | just got hit |
| `die` | plays once as it fades out |

Body sizes (width x height, in px at 100 px per unit) from `Assets/Scripts/Data/MonsterDefs.cs`:

| Key | Monster | Lv | Body |
|---|---|---|---|
| `hedge_shrew` | Hedge Shrew | 1 | 96 x 58 (painted: idle 8 frames, move 8) |
| `ash_sprout` | Ash Sprout | 2 | 80 x 92 (painted: idle 8 frames, move 8) |
| `thornback_hedgehog` | Thornback Hedgehog | 4 | 120 x 97 (painted: idle 8 frames, move 8) |
| `old_chest` | Old Chest | 4 | 100 x 80 |
| `moss_crawler` | Moss Crawler | 6 | 110 x 83 (painted: idle 8 frames, move 8) |
| `bramble_boar` | Bramble Boar | 8 | 150 x 130 (painted: idle 8 frames, move 8) |
| `mossy_chest` | Mossy Chest | 8 | 100 x 80 |
| `den_shade` | Den Shade | 9 | 100 x 140 (floats a little) |
| `violet_wraith` | Violet Wraith | 10 | 120 x 150 (floats a little) |
| `marsh_toad` | Marsh Toad | 11 | 100 x 80 |
| `bog_lurker` | Bog Lurker | 13 | 140 x 110 |
| `ash_wisp` | Ash Wisp | 14 | 80 x 90 (floats) |
| `sunken_chest` | Sunken Chest | 15 | 110 x 90 |
| `leaf_beetle` | Leaf Beetle | 3 | 116 x 81 (painted: idle 8 frames, move 8) |
| `mossling` | Mossling | 4 | 80 x 89 (painted: idle 8 frames, move 8) |
| `ember_fox` | Ember Fox | 5 | 120 x 87 (painted: idle 8 frames, move 8) |

The body size is what touches and gets hit; art can be a bit bigger (ears, tails, glow).

### Props: `Art/Props/`

Bottom centre of the image = where it stands.

| File | What it is | Suggested size |
|---|---|---|
| `portal.png` | Map portal: the owner's painted oval (blue) | 108 x 220 |
| `return_portal.png` | The same, recoloured gold, for a town's return portal | 108 x 220 |
| `town_portal.png` | The same, recoloured violet, for your own T portal | 108 x 220 |
| `ladder.png` | One section of the owner's painted ladder (two rungs), tiles upward; a theme's own `ladder.png` wins | 72 x 84 |
| `waystone.png` | Town waystone | 90 x 190 |
| `merchant.png` | Merchant and stall | 300 x 230 |
| `smithy.png` | Smith, anvil and forge | 320 x 230 |

The portal and ladder files are cut from the owner's painted pictures by `Tools/cut_props.py` (it scales the portal
to 2.2 units, recolours the glow for the other two, and cuts a seamless two-rung ladder section). Portals also get
the rippling shimmer from the effect shader.

On a painted scene these are optional: the painting already shows the shops, so nothing is drawn there
unless one of these files exists (a merchant or smith character standing in front of the painting, for example).

### Monster parts: `Art/Parts/<part>.png`

A painted icon per part, 64 x 64 with a transparent background, named after the part in lower case with underscores
(`shrew_tuft.png`, `sprout_leaf.png`, `moss_tuft.png`, `ember_core.png`, `hedgehog_quill.png`, `boar_tusk.png` so far). A part with an icon shows it when it drops (half a unit
tall) and in the bag's materials tab and the merchant's sell list; parts without one stay coloured blobs.

### Potions: `Art/Items/hp_potion.png`, `mp_potion.png`

Painted potion icons (256 x 256, transparent). Shown on the ground when a potion drops and on the potion cards in the
bag's useables tab.

### World map: `Art/UI/world_map.png`

The painted atlas shown behind the world map window (M): the owner's `AshenVale_World_Map` terrain (1536 x 1024, the
unlabelled version; the game writes the names, roads and markers itself so unexplored places can stay hidden). The
window takes the picture's own shape (up to 760 tall) and never stretches it. Places are drawn at the spots listed in
`Assets/Scripts/Data/WorldAtlas.cs` (shares of the picture's width and height from the top left), copied from the
pack's `WorldMapLocations.json`; a new picture with different landmark positions means moving those spots. Names are
written in dark ink on small parchment patches, and the footer text sits in the dark frame below the picture.
Markers are the owner's gold atlas seals in `Art/UI/MapMarkers/` (town castle, hunting leaf, boss skull, unexplored
compass, a compass ring for the place you are on, a small gem tinted by difficulty); the place you are on flashes.

### Heroes: `Assets/Resources/Heroes/<Hero>/<animation>/`

The current heroes are pixel art (128 x 128 frames, sharp filtering). Painted heroes can replace them with the same
folder and animation names (`idle`, `run`, `jump`, `fall`, `hurt`, `death`, plus each hero's attacks); the hero
import settings would then be switched to smooth filtering at the new size.
