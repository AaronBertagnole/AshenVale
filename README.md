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
Emberhaven (town) - Emberfields - Amberleaf Trail - Mossgate Hollow - Mosswood - Kingsroad - Cinderford (town) - Ashen Marsh
                    Lv 1-4        Lv 2-5            Lv 4-6     \      Lv 6-8   \ Lv 8-10                         Lv 11-14
                                                        top ledge: Shale Cliffs   '- secret ledge, top left: Hollow Den (Lv 9)
                                                        (up the mountain, Lv 9-10)
```

```
Shale Cliffs (summit) - Frostpine Pass - Highspire (town) - Glacier Steps - Windward Ridge - Ironvein Mines - The Deep Forge - The Colossus' Vault
                        Lv 15-17                           Lv 16-19        Lv 15-19         Lv 21-23     \   Lv 20-22        (boss, Lv 24)
                                                                                     hidden ledge at the top: Crystal Hollow (Lv 18-22)
```

```
Ashen Marsh - Brackwater Shore - Saltmarrow (town) - Wreckers' Reef - Smugglers' Cove - The Black Galleon - Gallows Point - Captain's Hold
              Lv 25-27                               Lv 25-29   \     Lv 29-31          Lv 30-33            Lv 26-34        (boss, Lv 34)
                                                     hole at the reef's end: Siren's Grotto (Lv 28-31)
```

```
Saltmarrow (north gate) - Thistledown Trail - Eldermere (town) - Glimmerwood - The Hollow Library - Owlkeep - Spellbroken Glade - The Archmage's Sanctum
                          Lv 35-36                                Lv 36-39  \   Lv 39-41             Lv 41-43   Lv 43-44           (boss, Lv 44)
                                                                  highest bough: Moonwell (Lv 37-40)
```

```
Spellbroken Glade (top shelf) - The Forgotten Stair - Lastlight (town) - The Sunken Avenue - Hall of Idols - The Weeping Catacombs - The Black Ziggurat - The Sleeper's Throne
                                Lv 45-47                                 Lv 46-48           Lv 47-51   \    Lv 51-53                Lv 53-55            (boss, Lv 55)
                                                                                            walled-up cell: The Oubliette (Lv 47-50)
```

New heroes start in Emberhaven; Emberfields is through the portal on the right. A portal on Mossgate Hollow's top
ledge climbs to Shale Cliffs, and from its summit the Frostpine Pass leads up into the snow: Highspire, the dwarves'
hall, then the glacier, the ridge, and down into the mines and the old forge under the mountain, where the Rusted
Colossus (the war golem the dwarves built and could not stop) waits in its vault. East past the Ashen Marsh the
land runs down to the Saltmarrow coast, where pirates were common once: a port that is still a port, then the reefs,
the smugglers' caves, the beached Black Galleon and Gallows Point, with Captain Marrow holding the top of the cliffs.
North of Saltmarrow the deep forest closes over the road: Eldermere, where the magicians lived, their hollow library,
the owls' keep, the glade where the spells broke loose, and the Hollow Archmage keeping what is left of his sanctum.
At the top of that glade a stair goes down into the ruins: a city that was only a story until someone found it.
Lastlight is the camp of the ones who stayed; below it the avenues, the idols, the catacombs and the black ziggurat
lead to the throne of the Sleeper Beneath, which the city was built to keep asleep. Nothing down there is cute. Region layouts live in
`Assets/Scripts/Data/Maps/<Region>.cs`, their monsters in `Data/Monsters/<Region>.cs`.
Every monster kind has a fixed level, like classic MapleStory, and monsters far below your level give less XP.
Some monsters leave you alone until you hit them; after that they hunt you like the aggressive ones do.
Walk into a town to find it; its waystone then lets you travel between found towns. **T** opens a portal to
your last town, and that town's return portal takes you back.

Cinderford, the second town, is a long walled street with a gate at each end, two shops, a smithy and a waystone.

**M** opens the world map: every place you have been, "???" for the places next door, the roads between them,
where you are and where the rest of your party is (hover a place for its monsters). Secret places stay hidden
until you find them. The minimap in the top right corner (**Tab** hides it) shows the map you are on: floors,
ladders, portals, monsters (red), chests (gold), other heroes (green) and you (yellow).

Chests turn up at random: each hunting map has a few chest spots (`k` in the layouts), and when someone arrives on
a map there is a 50% chance a chest is waiting at one of them (never the same spot twice in a row, and not within a
minute of the last one). Staying on a map never brings another: leave and come back, or have a friend arrive. An
unopened chest moves on after 3 minutes. One chest per map at a time; the secret Hollow Den allows two.

Keys can be changed from the Esc menu (**CONTROLS**): two keys per action, saved on your PC. Every label in the game
shows your bound keys. The merchant also buys monster parts, and what you pick up is listed at the bottom left.

**Thornmother**, the first boss, waits in her glade off the east end of Mosswood (Lv 9, a party job or a well-geared
solo). She rears up before slamming the roots (step back or jump), calls thorn sprouts, and rages below a third of
her health. Everyone alive in the glade gets her XP; she always drops Wraith Silk and spills parts like a chest, and
returns three minutes after she falls. Hits show damage numbers; hers show in red over you.

Monsters also drop parts (chests spill several). The smithies in every town spend parts and gold to upgrade gear
from +1 to +5, about 12% of the item's base stats each time. Which parts depends on the item's level band and slot.

Maps are text pictures in `Assets/Scripts/Data/MapDefs.cs` and `Data/Maps/*.cs` (the legend is in the comment above `MapLayout`).
After editing one, run `python Tools/check_maps.py`: it checks that every platform can really be reached,
every ladder and marker stands on something, and every portal leads somewhere and back.

## Art

The towns (Emberhaven, Cinderford) and the road from Emberfields to Kingsroad are painted, each as a see-through
front with a distant picture sliding behind it for depth, plus animated touches: waystone and forge glows,
waterfalls and river glints, mist, falling leaves, smoke and drifting motes (`Assets/Scripts/Data/MapArt.cs`).
Hollow Den, Ashen Marsh, monsters and town folk are still placeholder shapes. See [ART.md](ART.md) for sizes,
file names and folders: drop a PNG in the right place and it replaces that placeholder.

## Playing together

The host's game runs the world (monsters, damage, loot, XP). Everyone else sends their key presses and
sees what the host sends back. Up to 4 players. Each player keeps their own hero file.

Hunting together pays: a kill gives full XP to everyone nearby (about a screen) and to anyone who hit the monster
in the last ten seconds, and a boss gives it to the whole map. Monsters appear with 30% more health per extra
player on the map. Drops belong to whoever did the most damage for eight seconds (faint for everyone else), then
anyone can take them. Potions heal 40% of your maximum (never less than 60 HP / 45 MP), so they keep up as you level.

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

Then: host picks a hero > **HOST ONLINE GAME**. A join code appears at the top right after a few seconds, with a
**COPY CODE** button. Friends type (or Ctrl+V) it under **JOIN WITH CODE**. Friends need a build of the game
(**File > Build Profiles** > Windows > **Build**, then send them the folder), not the Unity project.

**Testing alone:** host in the Unity editor and join from a build of the game on the same PC (the build signs in as
a different player than the editor does). That goes through Relay for real, the same as a friend joining.

If hosting fails, the game says why at the top of the screen and carries on solo; the Unity Console has the full
error. The usual causes: the project is not linked (Project Settings > Services shows no project), or Relay / Lobby
have not been set up for it in the Dashboard. Relay has a free tier that easily covers testing.

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
