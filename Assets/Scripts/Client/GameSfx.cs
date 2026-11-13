using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // One-shot sound effects, by name: Resources/Audio/Sfx/<name>.wav (or .ogg). Played at the player's
    // effects volume (SoundSettings). A missing file is simply silent, so effects can be added one at a time.
    public static class GameSfx
    {
        static AudioSource source;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        // volume 0..1; the same sound is not started again within `gap` seconds (several marks at once, say).
        public static void Play(string name, float volume = 1f, float gap = .06f)
        {
            float last;
            if (lastPlayed.TryGetValue(name, out last) && Time.unscaledTime - last < gap) return;
            AudioClip clip;
            if (!clips.TryGetValue(name, out clip)) clips[name] = clip = Resources.Load<AudioClip>("Audio/Sfx/" + name);
            if (clip == null) return;
            if (source == null)
            {
                var host = new GameObject("Ashen Vale SFX") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(host);
                source = host.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 0f;
            }
            lastPlayed[name] = Time.unscaledTime;
            SoundSettings.PlayEffect(source, clip, volume);
        }
    }
}
