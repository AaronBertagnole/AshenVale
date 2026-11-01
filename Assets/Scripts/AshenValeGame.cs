using System.Collections.Generic;
using UnityEngine;
using AshenVale;

// Entry point: the scene's only component (keep this file's .meta so the scene still finds it).
//
// How the code is laid out:
//   Core/Tuning.cs      every gameplay number
//   Data/               content tables: heroes, monsters, items, maps
//   Sim/World.cs        the game state and all rules; driven only by PlayerInput and commands
//   Persistence/        hero save files (one JSON file per hero) and the conversion to and from the World
//   View/               draws the World (scenery, heroes, monsters, effects, camera)
//   UI/                 menus, HUD, inventory, shop, Esc menu (IMGUI)
//   Client/             keyboard and music
//
// This class runs the menus, feeds keyboard input to the World at a fixed 60 Hz tick, lets the view and HUD
// draw it, and saves the hero. Online play slots in here: the host ticks the World with every player's input,
// clients send their input and draw what the host reports.
public class AshenValeGame : MonoBehaviour
{
    const float AutosaveInterval = 30f;

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
    float saveTimer;
    int lastMap = -1;

    void Awake()
    {
        Application.targetFrameRate = 60;
        music = new MusicPlayer(gameObject);
        music.Play();
        world = new World(System.Environment.TickCount);
        view = new WorldView(world);
        hud = new GameHud(world) { onCharacterSelect = BackToHeroes, onQuit = QuitGame };
        menu = new FrontMenu(music, CreateHero, PlaySaved, DeleteSaved);
        menu.SetSaved(SaveStore.LoadAll());
    }

    // ---------------------------------------------------------------- starting and leaving

    void CreateHero(int hero, string name)
    {
        var ch = new Character { id = SaveStore.NewId(), name = name, hero = hero };
        StartRun(ch, Maps.Emberfields, -1, -1);
        SaveNow();
    }

    void PlaySaved(CharacterData data)
    {
        StartRun(SaveCodec.ToCharacter(data), data.map, data.hp, data.mp);
    }

    void DeleteSaved(CharacterData data)
    {
        SaveStore.Delete(data.id);
        menu.SetSaved(SaveStore.LoadAll());
    }

    void StartRun(Character ch, int map, int hp, int mp)
    {
        music.Stop();
        world.Clear();
        view.Reset();
        hud.Close();
        keyboard.Clear();
        inputs.Clear();
        accumulator = 0;
        var me = world.AddPlayer(ch, map, hp, mp);
        localId = me.id;
        lastMap = me.map;
        saveTimer = AutosaveInterval;
        inMenu = false;
    }

    void SaveNow()
    {
        var me = world.GetPlayer(localId);
        if (me != null) SaveStore.Save(SaveCodec.ToData(me));
        saveTimer = AutosaveInterval;
    }

    // N on the death screen, or "Save & your heroes" in the Esc menu.
    void BackToHeroes()
    {
        SaveNow();
        world.Clear();
        view.Reset();
        hud.Close();
        localId = -1;
        menu.SetSaved(SaveStore.LoadAll());
        menu.screen = FrontMenu.Screen.Characters;
        inMenu = true;
        music.Play();
    }

    void QuitGame()
    {
        SaveNow();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Closing the window (or stopping Play in the editor) saves too.
    void OnApplicationQuit()
    {
        if (!inMenu) SaveNow();
    }

    // ---------------------------------------------------------------- frame loop

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
        if (me != null && me.dead && me.deathClock >= Tuning.RespawnDelay && Input.GetKeyDown(KeyCode.N)) { BackToHeroes(); return; }
        hud.HandleKeys(me);
        if (inMenu) return;   // an Esc-menu choice may have just left the game
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

        Autosave();
        view.Sync(localId, Time.deltaTime, accumulator / step);
    }

    // Saves every 30 seconds, whenever the hero changes map, and on every level up.
    void Autosave()
    {
        var me = world.GetPlayer(localId);
        if (me == null) return;
        bool levelled = false;
        foreach (var e in world.events) if (e.kind == SimEventKind.LevelUp && e.playerId == localId) levelled = true;
        saveTimer -= Time.deltaTime;
        if (saveTimer <= 0 || me.map != lastMap || levelled)
        {
            lastMap = me.map;
            SaveNow();
        }
    }

    void OnGUI()
    {
        if (inMenu) menu.Draw();
        else hud.Draw(world.GetPlayer(localId));
    }
}
