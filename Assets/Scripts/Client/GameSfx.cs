using System.Collections.Generic;
using UnityEngine;

namespace AshenVale
{
    // One-shot sound effects, by name: Resources/Audio/Sfx/<name>.wav (or .ogg). Played at the player's
    // effects volume (SoundSettings). A missing file is simply silent, so effects can be added one at a time.
    public static class GameSfx
    {
        static AudioClip[] Takes(string name)
        {
            AudioClip[] found;
            if (clips.TryGetValue(name, out found)) return found;
            var list = new List<AudioClip>();
            var one = Resources.Load<AudioClip>("Audio/Sfx/" + name);
            if (one != null) list.Add(one);
            for (int i = 1; i <= 6; i++)
            {
                var take = Resources.Load<AudioClip>("Audio/Sfx/" + name + "_" + i);
                if (take != null) list.Add(take);
            }
            return clips[name] = list.ToArray();
        }

        static AudioSource source;
        static readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        // volume 0..1; the same sound is not started again within `gap` seconds (several marks at once, say).
        // Several takes of one sound (<name>_1, <name>_2 ... up to _6) are picked from at random. If the sound has no
        // file at all, `fallback` is played in its place (every kind of pickup falls back to "pickup", say).
        public static void Play(string name, float volume = 1f, float gap = .06f, string fallback = null)
        {
            var takes = Takes(name);
            if (takes.Length == 0 && fallback != null) { name = fallback; takes = Takes(name); }
            if (takes.Length == 0) return;
            float last;
            if (lastPlayed.TryGetValue(name, out last) && Time.unscaledTime - last < gap) return;
            var clip = takes[Random.Range(0, takes.Length)];
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
