using UnityEngine;

namespace AshenVale
{
    // Music: the title theme in the menus and a theme for the maps that have one (towns, so far), with a
    // short fade between them. Its volume and the mute come from SoundSettings.
    public sealed class MusicPlayer
    {
        const string ClipPath = "Audio/BeneathTheWinterBough", FallbackPath = "Audio/WhereTheMapsEnd_Loop_v3";
        const float FadeTime = 1.2f;

        // The theme for a map (a clip under Resources), or null for none.
        static string MapClipPath(int mapId)
        {
            return mapId == Maps.Emberhaven ? "Audio/TheBellsOfEmberhaven"
                 : mapId == Maps.ThornGlade ? "Audio/BeneathTheTangledBough"      // Thornmother's Glade
                 : null;
        }

        readonly AudioSource source;
        readonly AudioClip menuClip;
        AudioClip wanted;           // what should be playing (null: silence)
        AudioClip mapClip;          // the clip loaded for mapClipId
        int mapClipId = -1;
        float fade = 1f;            // 0 silent .. 1 full, multiplied into the volume
        public float Volume { get { return SoundSettings.Music; } }
        public bool Muted { get { return SoundSettings.Muted; } }

        public MusicPlayer(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            Apply();
            menuClip = Resources.Load<AudioClip>(ClipPath);
            if (menuClip == null)
            {
                Debug.LogWarning("Ashen Vale: music missing at Assets/Resources/" + ClipPath + ".ogg - using the old loop.");
                menuClip = Resources.Load<AudioClip>(FallbackPath);
            }
            if (menuClip == null) Debug.LogWarning("Ashen Vale: no music clip could be loaded.");
            else Debug.Log("Ashen Vale: menu music " + menuClip.name + " (" + menuClip.length.ToString("0.0") + " s, state " + menuClip.loadState + ")");
        }

        // The title theme (menus).
        public void Play()
        {
            wanted = menuClip;
            if (source.clip == menuClip && source.isPlaying) return;
            StartClip(menuClip, 1f);
        }

        // Silence, at once (a run is starting or ending).
        public void Stop() { wanted = null; source.Stop(); source.clip = null; fade = 1f; Apply(); }

        // In the game: play the theme of the map the hero is on, if it has one. Call every frame; pass -1 for no map.
        public void SetMap(int mapId)
        {
            if (mapId != mapClipId)
            {
                mapClipId = mapId;
                string path = MapClipPath(mapId);
                mapClip = path != null ? Resources.Load<AudioClip>(path) : null;
                if (path != null && mapClip == null) Debug.LogWarning("Ashen Vale: map music missing at Assets/Resources/" + path);
            }
            wanted = mapClip;
        }

        // Fades the current piece out when another (or none) is wanted, then brings the wanted one in.
        public void Tick(float dt)
        {
            bool playing = source.isPlaying && source.clip != null;
            if (playing && source.clip != wanted)
            {
                fade = Mathf.Max(0f, fade - dt / FadeTime);
                if (fade <= 0f) { source.Stop(); source.clip = null; }
            }
            else if (!playing && wanted != null) StartClip(wanted, 0f);
            else if (playing && fade < 1f) fade = Mathf.Min(1f, fade + dt / FadeTime);
            Apply();
        }

        void StartClip(AudioClip clip, float startFade)
        {
            if (clip == null) { source.Stop(); source.clip = null; return; }
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            source.clip = clip;
            fade = startFade;
            Apply();
            source.Play();
        }

        public void SetVolume(float volume) { SoundSettings.SetMusic(volume); Apply(); }
        public void ToggleMute() { SoundSettings.ToggleMute(); Apply(); }

        // Overall volume and mute live on the AudioListener (SoundSettings.Apply); this is the music's own level.
        void Apply() { SoundSettings.Apply(); source.volume = SoundSettings.Music * fade; }
    }
}
