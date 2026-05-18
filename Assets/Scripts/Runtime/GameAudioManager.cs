using UnityEngine;

namespace ChessVR.Runtime
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class GameAudioManager : MonoBehaviour
    {
        private const string MusicVolumeKey = "ChessVR.Audio.MusicVolume";
        private const string SfxVolumeKey = "ChessVR.Audio.SfxVolume";
        private const string MutedKey = "ChessVR.Audio.Muted";

        [Header("Clips")]
        [SerializeField] private AudioClip musicLoop;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip dropClip;
        [SerializeField] private AudioClip captureClip;
        [SerializeField] private AudioClip invalidClip;
        [SerializeField] private AudioClip gameOverClip;

        [Header("Defaults")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultMusicVolume = 0.25f;
        [Range(0f, 1f)]
        [SerializeField] private float defaultSfxVolume = 0.8f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        public static GameAudioManager Instance { get; private set; }

        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }
        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureSources();
            LoadSettings();
            ApplyVolumes();
            StartMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            defaultMusicVolume = Mathf.Clamp01(defaultMusicVolume);
            defaultSfxVolume = Mathf.Clamp01(defaultSfxVolume);

            if (Application.isPlaying)
            {
                ApplyVolumes();
            }
        }

        public void PlayPickup()
        {
            PlaySfx(pickupClip);
        }

        public void PlayDrop()
        {
            PlaySfx(dropClip);
        }

        public void PlayCapture()
        {
            PlaySfx(captureClip);
        }

        public void PlayInvalid()
        {
            PlaySfx(invalidClip);
        }

        public void PlayGameOver()
        {
            PlaySfx(gameOverClip);
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            PlayerPrefs.SetInt(MutedKey, IsMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        private void EnsureSources()
        {
            _musicSource = EnsureChildSource("MusicSource");
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;

            _sfxSource = EnsureChildSource("SFXSource");
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
        }

        private AudioSource EnsureChildSource(string sourceName)
        {
            var child = transform.Find(sourceName);
            if (child == null)
            {
                var go = new GameObject(sourceName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            var source = child.GetComponent<AudioSource>();
            return source != null ? source : child.gameObject.AddComponent<AudioSource>();
        }

        private void LoadSettings()
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume));
            IsMuted = PlayerPrefs.GetInt(MutedKey, 0) != 0;
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = IsMuted ? 0f : MusicVolume;
            }

            if (_sfxSource != null)
            {
                _sfxSource.volume = IsMuted ? 0f : SfxVolume;
            }
        }

        private void StartMusic()
        {
            if (_musicSource == null || musicLoop == null)
            {
                return;
            }

            _musicSource.clip = musicLoop;
            if (!_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }
    }
}
