using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Central audio manager for the slot machine game.
    /// Manages sound effects, looping mechanical reel sounds, win fanfares, and coin showers.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource reelLoopSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip leverPullClip;
        [SerializeField] private AudioClip reelSpinLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip anticipationClip;
        [SerializeField] private AudioClip winSmallClip;
        [SerializeField] private AudioClip winJackpotClip;
        [SerializeField] private AudioClip coinPayoutClip;
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Volume Settings")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] private bool isMuted = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            InitializeSources();
        }

        private void InitializeSources()
        {
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (reelLoopSource == null) reelLoopSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();

            reelLoopSource.loop = true;
            reelLoopSource.playOnAwake = false;
        }

        public void PlayLeverPull() => PlayOneShot(leverPullClip, 0.9f);
        public void PlayReelStop() => PlayOneShot(reelStopClip, 0.85f);
        public void PlayAnticipation() => PlayOneShot(anticipationClip, 1.0f);
        public void PlayWinSmall() => PlayOneShot(winSmallClip, 0.9f);
        public void PlayJackpot() => PlayOneShot(winJackpotClip, 1.0f);
        public void PlayCoins() => PlayOneShot(coinPayoutClip, 0.8f);
        public void PlayButtonClick() => PlayOneShot(buttonClickClip, 0.7f);

        public void PlayReelSpin()
        {
            if (isMuted || reelSpinLoopClip == null) return;
            reelLoopSource.clip = reelSpinLoopClip;
            reelLoopSource.volume = masterVolume * 0.7f;
            reelLoopSource.Play();
        }

        public void StopReelSpin()
        {
            if (reelLoopSource != null && reelLoopSource.isPlaying)
            {
                reelLoopSource.Stop();
            }
        }

        private void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (isMuted || clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volumeScale * masterVolume);
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            if (isMuted) StopReelSpin();
        }

        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (reelLoopSource != null) reelLoopSource.volume = masterVolume * 0.7f;
        }
    }
}
