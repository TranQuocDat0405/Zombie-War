using NFramework;
using UnityEngine;
using UnityEngine.Audio;

namespace ZombieWar
{
    /// <summary>
    /// Pooled one-shot 3D positional SFX (gunfire, zombies, pickups, bombs) — ported
    /// from the old AudioManager, minus music (NFramework SoundManager owns that).
    /// NOT merged into SoundManager on purpose: its sources sit on the manager and
    /// have no world position, which would flatten every battlefield sound to 2D.
    /// Routing these sources through the SFX mixer group keeps them on the same
    /// volume slider while preserving direction and distance.
    /// </summary>
    public class WorldSoundManager : SingletonMono<WorldSoundManager>
    {
        [SerializeField] private int sourceCount = 12;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        private AudioSource[] sources;
        private int next;

        protected override void Awake()
        {
            base.Awake();

            sources = new AudioSource[sourceCount];
            for (int i = 0; i < sourceCount; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform, false);
                sources[i] = go.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 1f;
                sources[i].rolloffMode = AudioRolloffMode.Linear;
                sources[i].maxDistance = 40f;
                sources[i].dopplerLevel = 0f;
                sources[i].outputAudioMixerGroup = sfxMixerGroup; // slider control via mixer
            }
        }

        /// <param name="minDistance">3D loudness plateau radius — big values (10+) make the
        /// sound dominate a wide area while still being directional (explosions).</param>
        public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f,
            bool spatial = true, float minDistance = 1.5f)
        {
            if (clip == null || sources == null) return;
            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.transform.position = position;
            src.spatialBlend = spatial ? 1f : 0f;
            src.pitch = pitch;
            src.minDistance = minDistance;
            src.maxDistance = Mathf.Max(40f, minDistance * 6f);
            // No manual volume multiply — the mixer's SFXVolume applies globally.
            src.PlayOneShot(clip, volume);
        }

        public void PlaySfxRandomPitch(AudioClip clip, Vector3 position, float volume = 1f)
        {
            PlaySfx(clip, position, volume, Random.Range(0.92f, 1.08f));
        }
    }
}
