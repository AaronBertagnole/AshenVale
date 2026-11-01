# AshenVale — Where the Maps End

A 2D side-scrolling adventure RPG: MapleStory meets Diablo 3. Grindy, exploration-heavy, built for
online co-op but fully playable solo.

## Open it

1. Install Unity **6.3 LTS** (6000.3) through Unity Hub.
2. Unity Hub > **Add** > **Add project from disk** > pick this folder.
3. The AshenVale scene opens by itself. Press **Play**.

Heroes save automatically (one file each, in Unity's persistent data folder under `Characters/`).

## The world so far

```
Emberhaven (town) - Emberfields - Mosswood - Kingsroad - Cinderford (town) - Ashen Marsh
                    Lv 1-4        Lv 6-8   \ Lv 8-10                         Lv 11-14
                                            '- secret ledge, top left: Hollow Den (Lv 9)
```

Every monster kind has a fixed level, like classic MapleStory, and monsters far below your level give less XP.
Walk into a town to find it; its waystone then lets you travel between found towns. **T** opens a portal to
your last town, and that town's return portal takes you back.

Monsters also drop parts (chests spill several). Emberhaven's smithy spends parts and gold to upgrade gear
from +1 to +5, about 12% of the item's base stats each time. Which parts depends on the item's level band and slot.

Maps are text pictures in `Assets/Scripts/Data/MapDefs.cs` (the legend is in the comment above `MapLayout`).
After editing one, run `python Tools/check_maps.py`: it checks that every platform can really be reached,
every ladder and marker stands on something, and every portal leads somewhere and back.

## Art

Everything you see is placeholder shapes until real art is added. See [ART.md](ART.md) for sizes, file names
and folders: drop a PNG in the right place and it replaces that placeholder.

## Playing together

The host's game runs the world (monsters, damage, loot, XP). Everyone else sends their key presses and
sees what the host sends back. Up to 4 players. Each player keeps their own hero file.

In the game: **Start Adventure > Your Heroes > PLAY** on a hero, then choose how to play.

### Test on one PC (no setup needed)

1. **Window > Multiplayer > Multiplayer Play Mode**, tick **Player 2**, and wait for its window to load.
2. Press **Play**. Both windows start the game.
3. Main window: pick a hero > **HOST ON THIS PC**.
4. Player 2 window: pick (or create) a *different* hero > **JOIN BY IP ADDRESS** with `127.0.0.1`.

The same buttons work across a local network: the joiner types the host PC's local IP address
(for example `192.168.1.20`). Allow the game through the Windows firewall if asked. Port 7777.

### Online with friends (join code)

One-time setup, on the host's Unity project:

1. Sign in to the Unity Dashboard (cloud.unity.com) and create a project (or pick one).
2. In that project, under **Multiplayer**, open **Relay** and set it up (launch it). Do the same for **Lobby**.
3. In Unity: **Edit > Project Settings > Services**, choose **Use an existing Unity project ID**, and link
   it to that project.

Then: host picks a hero > **HOST ONLINE GAME**. A join code appears at the top right after a few seconds.
Friends type it under **JOIN WITH CODE**. Friends need a build of the game (**File > Build Profiles** >
Windows > **Build**, then send them the folder), not the Unity project.

Relay has a free tier that easily covers testing.

## Code layout

```
Assets/Scripts/
  AshenValeGame.cs     entry point: menus, fixed 60 Hz tick loop, solo / host / client modes
  Core/Tuning.cs       every balance number
  Data/                heroes, monsters, items, maps (content tables; maps are text layouts)
  Sim/World.cs         every game rule; driven only by PlayerInput and commands, so the host can run it online
  Persistence/         hero save files
  Net/                 online co-op: NetSession (messages, snapshots), OnlineSession (Relay join codes)
  View/                draws the world (themed scenery with parallax, heroes, monsters, effects, camera)
  UI/                  menus, HUD, inventory, shop, waystone, Esc menu
  Client/              keyboard and music
```
