using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Data;
using SlotGame.Reels;
using SlotGame.Audio;
using SlotGame.UI;
using SlotGame.Bonus;

namespace SlotGame.Core
{
    /// <summary>
    /// Central game manager and state controller.
    /// Orchestrates game economy, spin lifecycle, RNG outcomes,
    /// payout processing, bonus rounds, and UI/Audio dispatching.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Economy Settings")]
        [SerializeField] private int startingCredits = 1000;
        [SerializeField] private int minBet = 10;
        [SerializeField] private int maxBet = 100;
        [SerializeField] private int betStep = 10;

        [Header("Auto-Spin Settings")]
        [SerializeField] private int autoSpinBatchCount = 10;
        [SerializeField] private float autoSpinDelay = 1.0f;

        [Header("Subsystem References")]
        [SerializeField] private RNGManager rngManager;
        [SerializeField] private PayoutManager payoutManager;
        [SerializeField] private ReelGridManager reelGridManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private LeverController leverController;
        [SerializeField] private WinPopupController winPopupController;
        [SerializeField] private FreeSpinsManager freeSpinsManager;

        // Current state variables
        private GameState _currentState = GameState.Idle;
        private int _currentBalance;
        private int _currentBet;
        private int _lastWinAmount;
        private int _totalWinnings;
        private bool _isAutoSpinning = false;
        private int _autoSpinsRemaining = 0;
        private Coroutine _autoSpinCoroutine;

        public GameState CurrentState => _currentState;
        public int CurrentBalance => _currentBalance;
        public int CurrentBet => _currentBet;
        public int LastWinAmount => _lastWinAmount;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            InitializeEconomy();
        }

        private void Start()
        {
            RegisterEventHandlers();
            RefreshUI();
            SetState(GameState.Idle);
        }

        private void InitializeEconomy()
        {
            _currentBalance = startingCredits;
            _currentBet = minBet;
            _lastWinAmount = 0;
            _totalWinnings = 0;
        }

        private void RegisterEventHandlers()
        {
            // UI Button events
            if (uiManager != null)
            {
                uiManager.OnSpinClicked += HandleSpinRequest;
                uiManager.OnBetPlusClicked += IncreaseBet;
                uiManager.OnBetMinusClicked += DecreaseBet;
                uiManager.OnMaxBetClicked += SetMaxBet;
                uiManager.OnAutoSpinClicked += ToggleAutoSpin;
            }

            // Mechanical Lever event
            if (leverController != null)
            {
                leverController.OnLeverPulled += HandleSpinRequest;
            }

            // Grid stop event
            if (reelGridManager != null)
            {
                reelGridManager.OnAllReelsStopped += HandleAllReelsStopped;
                reelGridManager.OnAnticipationTriggered += () => uiManager.SetAnticipationActive(true);
            }
        }

        /// <summary>
        /// Initiates a spin if allowed by current state and credits.
        /// </summary>
        public void HandleSpinRequest()
        {
            if (_currentState != GameState.Idle && _currentState != GameState.FreeSpinsBonus)
            {
                return;
            }

            bool isFreeSpin = freeSpinsManager != null && freeSpinsManager.IsActive;

            // Check credit balance (free spins do not deduct credits)
            if (!isFreeSpin && _currentBalance < _currentBet)
            {
                SetState(GameState.InsufficientBet);
                uiManager.SetStatusMessage("Insufficient credits! Lower bet to spin.");
                if (_isAutoSpinning) StopAutoSpin();
                return;
            }

            // Deduct bet if not free spin
            if (!isFreeSpin)
            {
                _currentBalance -= _currentBet;
                uiManager.UpdateBalance(_currentBalance);
            }
            else
            {
                freeSpinsManager.ConsumeSpin();
                uiManager.SetFreeSpinsActive(true, freeSpinsManager.RemainingSpins, freeSpinsManager.BonusMultiplier);
            }

            SetState(GameState.Spinning);
            uiManager.SetAnticipationActive(false);
            uiManager.UpdateWin(0);
            uiManager.SetStatusMessage(isFreeSpin ? "Free Spin in progress..." : "Spinning...");

            // Generate RNG outcome
            SymbolType[] outcome = rngManager.GenerateSpinOutcome();

            // Launch reels
            reelGridManager.StartGridSpin(outcome);
        }

        private void HandleAllReelsStopped(SymbolType[] outcome)
        {
            SetState(GameState.Evaluating);
            uiManager.SetAnticipationActive(false);

            // Compute payout
            float activeMultiplier = (freeSpinsManager != null && freeSpinsManager.IsActive)
                ? freeSpinsManager.BonusMultiplier
                : 1.0f;

            SpinResult result = payoutManager.EvaluateSpin(outcome, _currentBet, activeMultiplier);
            _lastWinAmount = result.TotalPayout;

            if (result.IsWin)
            {
                HandleWinOutcome(result);
            }
            else
            {
                HandleNoWinOutcome(result);
            }
        }

        private void HandleWinOutcome(SpinResult result)
        {
            SetState(GameState.CelebratingWin);
            _currentBalance += result.TotalPayout;
            _totalWinnings += result.TotalPayout;

            uiManager.UpdateWin(result.TotalPayout);
            uiManager.UpdateBalance(_currentBalance, true);
            uiManager.SetStatusMessage(result.WinDescription);

            // Highlight winning symbols on reels
            reelGridManager.HighlightWinningReels(result.WinningReelIndices);

            // Check if Free Spins were awarded
            if (result.IsFreeSpinsTriggered && freeSpinsManager != null)
            {
                freeSpinsManager.AwardFreeSpins(result.FreeSpinsAwarded);
                uiManager.SetFreeSpinsActive(true, freeSpinsManager.RemainingSpins, freeSpinsManager.BonusMultiplier);
            }

            if (freeSpinsManager != null && freeSpinsManager.IsActive)
            {
                freeSpinsManager.AddWinnings(result.TotalPayout);
            }

            // Big Win or Jackpot Celebration Modal
            if (result.IsJackpot || result.TotalPayout >= _currentBet * 25)
            {
                string title = result.IsJackpot ? "JACKPOT!" : "BIG WIN!";
                winPopupController.ShowWin(title, result.TotalPayout, result.WinDescription, result.IsJackpot, () =>
                {
                    ConcludeSpinCycle();
                });
            }
            else
            {
                audioManager.PlayWinSmall();
                StartCoroutine(DelayedConcludeRoutine(1.2f));
            }
        }

        private void HandleNoWinOutcome(SpinResult result)
        {
            uiManager.SetStatusMessage(result.WinDescription);
            StartCoroutine(DelayedConcludeRoutine(0.4f));
        }

        private IEnumerator DelayedConcludeRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            ConcludeSpinCycle();
        }

        private void ConcludeSpinCycle()
        {
            // Check if Free Spins mode is ongoing or finished
            if (freeSpinsManager != null && freeSpinsManager.IsActive)
            {
                if (freeSpinsManager.CheckIfFinished())
                {
                    uiManager.SetFreeSpinsActive(false);
                    winPopupController.ShowWin("BONUS COMPLETE!", freeSpinsManager.AccumulatedWinnings,
                        $"Total Free Spins Won: {freeSpinsManager.AccumulatedWinnings} Credits", false);
                    SetState(GameState.Idle);
                }
                else
                {
                    SetState(GameState.FreeSpinsBonus);
                    // Continue free spins automatically after brief pause
                    StartCoroutine(AutoContinueFreeSpinRoutine());
                    return;
                }
            }
            else
            {
                SetState(GameState.Idle);
            }

            // If auto-spin is running, proceed to next round
            if (_isAutoSpinning)
            {
                if (_autoSpinsRemaining > 0 && _currentBalance >= _currentBet)
                {
                    _autoSpinsRemaining--;
                    uiManager.SetAutoSpinState(true, _autoSpinsRemaining);
                    StartCoroutine(DelayedAutoSpinRoutine());
                }
                else
                {
                    StopAutoSpin();
                }
            }
        }

        private IEnumerator AutoContinueFreeSpinRoutine()
        {
            yield return new WaitForSeconds(0.8f);
            HandleSpinRequest();
        }

        private IEnumerator DelayedAutoSpinRoutine()
        {
            yield return new WaitForSeconds(autoSpinDelay);
            if (_isAutoSpinning)
            {
                HandleSpinRequest();
            }
        }

        public void IncreaseBet()
        {
            if (_currentState != GameState.Idle) return;
            if (_currentBet + betStep <= maxBet)
            {
                _currentBet += betStep;
                uiManager.UpdateBet(_currentBet);
            }
        }

        public void DecreaseBet()
        {
            if (_currentState != GameState.Idle) return;
            if (_currentBet - betStep >= minBet)
            {
                _currentBet -= betStep;
                uiManager.UpdateBet(_currentBet);
            }
        }

        public void SetMaxBet()
        {
            if (_currentState != GameState.Idle) return;
            _currentBet = maxBet;
            uiManager.UpdateBet(_currentBet);
        }

        public void ToggleAutoSpin()
        {
            if (_isAutoSpinning)
            {
                StopAutoSpin();
            }
            else
            {
                StartAutoSpin(autoSpinBatchCount);
            }
        }

        public void StartAutoSpin(int count)
        {
            _isAutoSpinning = true;
            _autoSpinsRemaining = count;
            uiManager.SetAutoSpinState(true, _autoSpinsRemaining);

            if (_currentState == GameState.Idle)
            {
                HandleSpinRequest();
            }
        }

        public void StopAutoSpin()
        {
            _isAutoSpinning = false;
            _autoSpinsRemaining = 0;
            uiManager.SetAutoSpinState(false);
            if (_autoSpinCoroutine != null) StopCoroutine(_autoSpinCoroutine);
        }

        private void SetState(GameState newState)
        {
            _currentState = newState;
            bool canInteract = (_currentState == GameState.Idle);

            if (uiManager != null)
            {
                uiManager.SetButtonsInteractable(canInteract, canInteract);
            }

            if (leverController != null)
            {
                leverController.SetInteractable(canInteract);
            }
        }

        private void RefreshUI()
        {
            if (uiManager != null)
            {
                uiManager.UpdateBalance(_currentBalance);
                uiManager.UpdateBet(_currentBet);
                uiManager.UpdateWin(0);
                uiManager.SetStatusMessage("Pull the lever or press SPIN to play!");
            }
        }
    }
}
