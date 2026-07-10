using UnityEngine;

namespace ZombieWar.Core
{
    /// <summary>
    /// Pooled one-shot 3D/2D SFX playback plus looping music source.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private int sourceCount = 12;
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

        private AudioSource[] sources;
        private AudioSource musicSource;
        private int next;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

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
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
            if (music != null)
            {
                musicSource.clip = music;
                musicSource.Play();
            }
        }

        public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (clip == null || sources == null) return;
            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.transform.position = position;
            src.spatialBlend = spatial ? 1f : 0f;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        public void PlaySfxRandomPitch(AudioClip clip, Vector3 position, float volume = 1f)
        {
            PlaySfx(clip, position, volume, Random.Range(0.92f, 1.08f));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
