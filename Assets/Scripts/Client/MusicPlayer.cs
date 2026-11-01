using UnityEngine;

namespace AshenVale
{
    // Title music and its volume / mute settings (saved in PlayerPrefs).
    public sealed class MusicPlayer
    {
        const string ClipPath = "Audio/WhereTheMapsEnd_Loop_v3";
        const string VolumeKey = "AshenVale.MusicVolume", MutedKey = "AshenVale.MusicMuted";

        readonly AudioSource source;
        public float Volume { get; private set; }
        public bool Muted { get; private set; }

        public MusicPlayer(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .30f));
            Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            Apply();
            source.clip = Resources.Load<AudioClip>(ClipPath);
            if (source.clip == null) Debug.LogWarning("Ashen Vale: music missing at Assets/Resources/" + ClipPath + ".ogg");
        }

        public void Play() { if (source.clip != null && !source.isPlaying) source.Play(); }
        public void Stop() { source.Stop(); }

        public void SetVolume(float volume)
        {
            if (Mathf.Abs(volume - Volume) < .001f) return;
            Volume = Mathf.Clamp01(volume);
            Save();
        }

        public void ToggleMute() { Muted = !Muted; Save(); }

        void Apply() { source.volume = Volume; source.mute = Muted; }

        void Save()
        {
            Apply();
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.SetInt(MutedKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
