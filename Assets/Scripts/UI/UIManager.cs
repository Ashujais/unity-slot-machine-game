using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotGame.Core;
using SlotGame.Audio;

namespace SlotGame.UI
{
    /// <summary>
    /// Coordinates all UI HUD elements: Balance, Bet, Win displays,
    /// buttons, banners, and state indicators.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI winText;
        [SerializeField] private TextMeshProUGUI statusMessageText;
        [SerializeField] private TextMeshProUGUI freeSpinsText;

        [Header("Containers & Banners")]
        [SerializeField] private GameObject freeSpinsBanner;
        [SerializeField] private GameObject anticipationBanner;

        [Header("Control Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button betMinusButton;
        [SerializeField] private Button maxBetButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private TextMeshProUGUI autoSpinButtonText;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        public event Action OnSpinClicked;
        public event Action OnBetPlusClicked;
        public event Action OnBetMinusClicked;
        public event Action OnMaxBetClicked;
        public event Action OnAutoSpinClicked;

        private Coroutine _balanceRollCoroutine;

        private void Awake()
        {
            BindButtons();

            if (freeSpinsBanner != null) freeSpinsBanner.SetActive(false);
            if (anticipationBanner != null) anticipationBanner.SetActive(false);
        }

        private void BindButtons()
        {
            if (spinButton != null)
                spinButton.onClick.AddListener(() => { PlayClick(); OnSpinClicked?.Invoke(); });

            if (betPlusButton != null)
                betPlusButton.onClick.AddListener(() => { PlayClick(); OnBetPlusClicked?.Invoke(); });

            if (betMinusButton != null)
                betMinusButton.onClick.AddListener(() => { PlayClick(); OnBetMinusClicked?.Invoke(); });

            if (maxBetButton != null)
                maxBetButton.onClick.AddListener(() => { PlayClick(); OnMaxBetClicked?.Invoke(); });

            if (autoSpinButton != null)
                autoSpinButton.onClick.AddListener(() => { PlayClick(); OnAutoSpinClicked?.Invoke(); });
        }

        private void PlayClick()
        {
            if (audioManager != null) audioManager.PlayButtonClick();
        }

        public void UpdateBalance(int newBalance, bool animate = false)
        {
            if (balanceText == null) return;

            if (animate)
            {
                if (_balanceRollCoroutine != null) StopCoroutine(_balanceRollCoroutine);
                _balanceRollCoroutine = StartCoroutine(RollBalanceRoutine(newBalance));
            }
            else
            {
                balanceText.text = $"{newBalance:N0}";
            }
        }

        private IEnumerator RollBalanceRoutine(int target)
        {
            int current = 0;
            int.TryParse(balanceText.text.Replace(",", ""), out current);

            float duration = 0.8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                int val = Mathf.RoundToInt(Mathf.Lerp(current, target, t));
                balanceText.text = $"{val:N0}";
                yield return null;
            }

            balanceText.text = $"{target:N0}";
        }

        public void UpdateBet(int bet)
        {
            if (betText != null) betText.text = $"{bet}";
        }

        public void UpdateWin(int winAmount)
        {
            if (winText != null) winText.text = winAmount > 0 ? $"+{winAmount:N0}" : "0";
        }

        public void SetStatusMessage(string message)
        {
            if (statusMessageText != null) statusMessageText.text = message;
        }

        public void SetButtonsInteractable(bool canSpin, bool canChangeBet)
        {
            if (spinButton != null) spinButton.interactable = canSpin;
            if (betPlusButton != null) betPlusButton.interactable = canChangeBet;
            if (betMinusButton != null) betMinusButton.interactable = canChangeBet;
            if (maxBetButton != null) maxBetButton.interactable = canChangeBet;
        }

        public void SetAutoSpinState(bool isAutoSpinning, int remaining = 0)
        {
            if (autoSpinButtonText != null)
            {
                autoSpinButtonText.text = isAutoSpinning ? $"STOP ({remaining})" : "AUTO";
            }
        }

        public void SetFreeSpinsActive(bool active, int remaining = 0, float multiplier = 2f)
        {
            if (freeSpinsBanner != null) freeSpinsBanner.SetActive(active);
            if (freeSpinsText != null)
            {
                freeSpinsText.text = $"FREE SPINS: {remaining} LEFT ({multiplier}x MULTIPLIER)";
            }
        }

        public void SetAnticipationActive(bool active)
        {
            if (anticipationBanner != null) anticipationBanner.SetActive(active);
        }
    }
}
