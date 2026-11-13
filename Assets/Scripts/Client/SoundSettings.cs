using UnityEngine;

namespace AshenVale
{
    // The player's sound settings, saved in PlayerPrefs: an overall volume, a music volume, a sound-effects
    // volume and a mute for everything. Overall volume and mute are applied to the AudioListener, so they
    // cover every sound in the game. Music reads Music; anything that plays a sound effect should multiply
    // its volume by Effects (see PlayEffect).
    public static class SoundSettings
    {
        const string MasterKey = "AshenVale.MasterVolume", MusicKey = "AshenVale.MusicVolume",
                     EffectsKey = "AshenVale.EffectsVolume", MutedKey = "AshenVale.MusicMuted";
        static bool loaded;
        static float master = 1f, music = .35f, effects = .8f;
        static bool muted;

        public static float Master { get { Load(); return master; } }
        public static float Music { get { Load(); return music; } }
        public static float Effects { get { Load(); return effects; } }
        public static bool Muted { get { Load(); return muted; } }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
            music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, .35f));
            effects = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, .8f));
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            Apply();
        }

        public static void Apply() { Load(); AudioListener.volume = muted ? 0f : master; }

        public static void SetMaster(float value) { Load(); if (Mathf.Abs(value - master) < .001f) return; master = Mathf.Clamp01(value); Save(); }
        public static void SetMusic(float value) { Load(); if (Mathf.Abs(value - music) < .001f) return; music = Mathf.Clamp01(value); Save(); }
        public static void SetEffects(float value) { Load(); if (Mathf.Abs(value - effects) < .001f) return; effects = Mathf.Clamp01(value); Save(); }
        public static void ToggleMute() { Load(); muted = !muted; Save(); }

        // For sound effects as they are added: plays a clip at the player's effects volume.
        public static void PlayEffect(AudioSource source, AudioClip clip, float volume = 1f)
        {
            if (source != null && clip != null) source.PlayOneShot(clip, volume * Effects);
        }

        static void Save()
        {
            Apply();
            PlayerPrefs.SetFloat(MasterKey, master);
            PlayerPrefs.SetFloat(MusicKey, music);
            PlayerPrefs.SetFloat(EffectsKey, effects);
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
