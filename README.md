# AshenVale — Where the Maps End

A 2D side-scrolling adventure RPG: MapleStory meets Diablo 3. Grindy, exploration-heavy, built for
online co-op but fully playable solo.

## Open it

1. Install Unity **2022.3.62f1** (or Unity 6.3 LTS; the project upgrades itself) through Unity Hub.
2. Unity Hub > **Add** > **Add project from disk** > pick this folder.
3. Open `Assets/Scenes/AshenVale.unity` and press **Play**.

## Code layout

```
Assets/Scripts/
  AshenValeGame.cs     entry point: menus, fixed 60 Hz tick loop, wires everything together
  Core/Tuning.cs       every balance number
  Data/                heroes, monsters, items, maps (content tables)
  Sim/World.cs         every game rule; driven only by PlayerInput and commands, so the host can run it online
  View/                draws the world (scenery, heroes, monsters, effects, camera)
  UI/                  menus, HUD, inventory, shop
  Client/              keyboard and music
```
