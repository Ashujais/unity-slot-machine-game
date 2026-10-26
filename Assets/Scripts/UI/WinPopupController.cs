using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotGame.Audio;

namespace SlotGame.UI
{
    /// <summary>
    /// Controls the victory popup dialog modal.
    /// Utilizes the ornate popup.png frame, rolling number animations,
    /// and the golden banner claim button.
    /// </summary>
    public class WinPopupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI winAmountText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button claimButton;
        [SerializeField] private RectTransform dialogBox;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        public event Action OnPopupClosed;

        private Coroutine _countUpCoroutine;
        private Action _onClaimCallback;

        private void Awake()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(HandleClaimClicked);
            }
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Displays the win popup with animated count-up and celebration.
        /// </summary>
        public void ShowWin(string title, int amount, string description, bool isJackpot, Action onClaim = null)
        {
            _onClaimCallback = onClaim;

            if (popupRoot != null) popupRoot.SetActive(true);

            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;

            if (isJackpot)
            {
                if (audioManager != null) audioManager.PlayJackpot();
            }
            else
            {
                if (audioManager != null) audioManager.PlayWinSmall();
            }

            // Animate popup entrance
            StartCoroutine(AnimateEntrance());

            // Rolling credit count-up
            if (_countUpCoroutine != null) StopCoroutine(_countUpCoroutine);
            _countUpCoroutine = StartCoroutine(CountUpCreditsRoutine(amount));
        }

        private IEnumerator AnimateEntrance()
        {
            if (dialogBox == null) yield break;

            dialogBox.localScale = Vector3.zero;
            float timer = 0f;
            float duration = 0.35f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                // Overshoot bounce pop
                float scale = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
                dialogBox.localScale = Vector3.one * Mathf.Clamp(scale, 0f, 1.2f);
                yield return null;
            }
            dialogBox.localScale = Vector3.one;
        }

        private IEnumerator CountUpCreditsRoutine(int targetAmount)
        {
            if (winAmountText == null) yield break;

            float duration = 1.5f;
            float elapsed = 0f;
            int current = 0;

            if (audioManager != null) audioManager.PlayCoins();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                current = Mathf.RoundToInt(Mathf.Lerp(0, targetAmount, Mathf.SmoothStep(0f, 1f, progress)));
                winAmountText.text = $"+{current:N0}";
                yield return null;
            }

            winAmountText.text = $"+{targetAmount:N0}";
        }

        private void HandleClaimClicked()
        {
            if (audioManager != null) audioManager.PlayButtonClick();

            if (popupRoot != null) popupRoot.SetActive(false);

            _onClaimCallback?.Invoke();
            OnPopupClosed?.Invoke();
        }
    }
}
