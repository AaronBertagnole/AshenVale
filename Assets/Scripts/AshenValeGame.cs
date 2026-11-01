using System.Collections.Generic;
using UnityEngine;
using AshenVale;

// Entry point: the scene's only component (keep this file's .meta so the scene still finds it).
//
// How the code is laid out:
//   Core/Tuning.cs      every gameplay number
//   Data/               content tables: heroes, monsters, items, maps
//   Sim/World.cs        the game state and all rules; driven only by PlayerInput and commands
//   View/               draws the World (scenery, heroes, monsters, effects, camera)
//   UI/                 menus, HUD, inventory, shop (IMGUI)
//   Client/             keyboard and music
//
// This class runs the menus, feeds keyboard input to the World at a fixed 60 Hz tick and lets the view and HUD
// draw it. Online play slots in here: the host ticks the World with every player's input, clients send their
// input and draw what the host reports.
public class AshenValeGame : MonoBehaviour
{
    World world;
    WorldView view;
    GameHud hud;
    FrontMenu menu;
    MusicPlayer music;
    readonly KeyboardInput keyboard = new KeyboardInput();
    readonly Dictionary<int, PlayerInput> inputs = new Dictionary<int, PlayerInput>();
    int localId = -1;
    bool inMenu = true;
    float accumulator;

    void Awake()
    {
        Application.targetFrameRate = 60;
        music = new MusicPlayer(gameObject);
        music.Play();
        world = new World(System.Environment.TickCount);
        view = new WorldView(world);
        hud = new GameHud(world);
        menu = new FrontMenu(music, StartHero);
    }

    void StartHero(int hero)
    {
        music.Stop();
        world.Clear();
        view.Reset();
        hud.Close();
        keyboard.Clear();
        inputs.Clear();
        accumulator = 0;
        localId = world.AddPlayer(hero).id;
        inMenu = false;
    }

    // N on the death screen.
    void BackToHeroSelect()
    {
        var me = world.GetPlayer(localId);
        menu.pendingHero = me != null ? me.ch.hero : 0;
        menu.screen = FrontMenu.Screen.Select;
        world.Clear();
        view.Reset();
        hud.Close();
        localId = -1;
        inMenu = true;
        music.Play();
    }

    void Update()
    {
        const float step = 1f / Tuning.TickRate;
        if (inMenu)
        {
            menu.HandleKeys();
            view.Sync(localId, Time.deltaTime, 1f);
            return;
        }

        var me = world.GetPlayer(localId);
        if (me != null && me.dead && me.deathClock >= Tuning.RespawnDelay && Input.GetKeyDown(KeyCode.N)) { BackToHeroSelect(); return; }
        hud.HandleKeys(me);
        keyboard.Sample(!hud.WindowOpen);

        accumulator += Time.deltaTime;
        int ticks = 0;
        while (accumulator >= step && ticks < Tuning.MaxTicksPerFrame)
        {
            inputs[localId] = keyboard.Take();
            world.Tick(step, inputs);
            accumulator -= step;
            ticks++;
        }
        if (ticks == Tuning.MaxTicksPerFrame) accumulator = 0;   // fell far behind (a hitch): skip ahead
        view.Sync(localId, Time.deltaTime, accumulator / step);
    }

    void OnGUI()
    {
        if (inMenu) menu.Draw();
        else hud.Draw(world.GetPlayer(localId));
    }
}
