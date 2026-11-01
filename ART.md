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

Emberhaven is 38 cells wide, so this picture shows at about 57 px per unit. A version twice the size
(4344 x 1448, same framing) would look sharp on a 1080p screen: drop it in with the same name.

### Map themes: `Art/Themes/<theme>/`

Themes: `town` (Emberhaven), `meadow` (Emberfields), `forest` (Mosswood), `cave` (Hollow Den),
`road` (Kingsroad), `rivertown` (Cinderford), `marsh` (Ashen Marsh).

| File | What it is | Suggested size | Lines up by |
|---|---|---|---|
| `sky.png` | Fills the screen behind everything, fixed to the camera | 2560 x 1440 | centre (scaled to cover) |
| `far.png` | Distant hills, mountains, trees. Tiles sideways; transparent above the silhouettes | 2048 x 1000 | bottom edge, about at ground level |
| `mid.png` | Nearer trees, houses, rocks. Tiles sideways; transparent above | 2048 x 700 | bottom edge, about at ground level |
| `ground.png` | The ground strip. Tiles sideways; its **top edge is the walking surface** | 256 x 160 | top edge |
| `platform_mid.png` | Middle of a platform, tiles sideways; **top edge is the walking surface** | 100 x 70 | top edge |
| `platform_left.png`, `platform_right.png` | Optional rounded ends for platforms | 100 x 70 each | top edge |
| `ladder.png` | One section of ladder (or rope), tiles upward | 60 x 100 | bottom edge |

`far` moves slowly behind the camera and `mid` a bit faster (parallax), so they should be seamless left to right.

### Monsters: `Art/Monsters/<key>/<animation>/00.png, 01.png, ...`

Frames are numbered from `00`, play at 8 per second and are drawn **facing right** (the game mirrors them).
Put the monster's feet at the bottom centre of the image. `idle` is required to switch a monster to art;
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
| `emberling` | Emberling | 1 | 85 x 90 |
| `ash_sprout` | Ash Sprout | 2 | 80 x 80 |
| `elite_emberling` | Elite Emberling | 4 | 115 x 135 |
| `old_chest` | Old Chest | 4 | 100 x 80 |
| `moss_crawler` | Moss Crawler | 6 | 100 x 70 |
| `bramble_boar` | Bramble Boar | 8 | 140 x 100 |
| `mossy_chest` | Mossy Chest | 8 | 100 x 80 |
| `den_shade` | Den Shade | 9 | 100 x 140 (floats a little) |
| `violet_wraith` | Violet Wraith | 10 | 120 x 150 (floats a little) |
| `marsh_toad` | Marsh Toad | 11 | 100 x 80 |
| `bog_lurker` | Bog Lurker | 13 | 140 x 110 |
| `ash_wisp` | Ash Wisp | 14 | 80 x 90 (floats) |
| `sunken_chest` | Sunken Chest | 15 | 110 x 90 |

The body size is what touches and gets hit; art can be a bit bigger (ears, tails, glow).

### Props: `Art/Props/`

Bottom centre of the image = where it stands.

| File | What it is | Suggested size |
|---|---|---|
| `portal.png` | Map portal | 110 x 210 |
| `return_portal.png` | Town return portal | 110 x 210 |
| `waystone.png` | Town waystone | 90 x 190 |
| `merchant.png` | Merchant and stall | 300 x 230 |
| `smithy.png` | Smith, anvil and forge | 320 x 230 |

On a painted scene these are optional: the painting already shows the shops, so nothing is drawn there
unless one of these files exists (a merchant or smith character standing in front of the painting, for example).

### Heroes: `Assets/Resources/Heroes/<Hero>/<animation>/`

The current heroes are pixel art (128 x 128 frames, sharp filtering). Painted heroes can replace them with the same
folder and animation names (`idle`, `run`, `jump`, `fall`, `hurt`, `death`, plus each hero's attacks); the hero
import settings would then be switched to smooth filtering at the new size.
