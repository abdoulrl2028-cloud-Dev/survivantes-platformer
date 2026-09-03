using UnityEngine;
using BlackHorizon.Systems;

namespace BlackHorizon.Audio
{
    /// <summary>
    /// Central audio mixer. Owns the AudioListener volume and provides public
    /// API to play footstep, shot, impact and ambient sounds. Clip references
    /// are assigned in the Inspector; without assets the system stays silent
    /// rather than failing (the architecture is asset-ready).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Clips")]
        public AudioClip footstepWalk;
        public AudioClip footstepRun;
        public AudioClip shot;
        public AudioClip reload;
        public AudioClip impact;
        public AudioClip ambient;
        public AudioClip stingClick;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;

        private AudioSource _sfxSource;
        private AudioSource _ambientSource;

        public float MasterVolume { get => masterVolume; set { masterVolume = Mathf.Clamp01(value); ApplyVolumes(); } }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioListener();

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.spatialBlend = 1f;
            _sfxSource.playOnAwake = false;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.spatialBlend = 0f;
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
        }

        private void Start()
        {
            if (ambient != null)
            {
                _ambientSource.clip = ambient;
                _ambientSource.volume = ambientVolume;
                _ambientSource.Play();
            }
            EventBus.OnFootstep += OnFootstep;
            EventBus.OnShotFired += OnShotFired;
        }

        private void OnDestroy()
        {
            EventBus.OnFootstep -= OnFootstep;
            EventBus.OnShotFired -= OnShotFired;
        }

        private void EnsureAudioListener()
        {
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private void OnFootstep(Vector3 worldPos)
        {
            PlayClip(footstepWalk, worldPos, 0.4f);
        }

        private void OnShotFired(Vector3 worldPos)
        {
            PlayClip(shot, worldPos, 1f);
        }

        private void PlayClip(AudioClip clip, Vector3 worldPos, float volume)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.transform.position = worldPos;
            _sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        }

        public void PlayUISfx(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
            _sfxSource.spatialBlend = 1f;
        }

        private void ApplyVolumes()
        {
            if (_ambientSource != null) _ambientSource.volume = ambientVolume * masterVolume;
        }

        public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); }
        public void SetAmbientVolume(float v) { ambientVolume = Mathf.Clamp01(v); ApplyVolumes(); }
    }
}
