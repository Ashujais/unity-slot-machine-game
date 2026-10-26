using System;
using UnityEngine;

namespace SlotGame.Bonus
{
    /// <summary>
    /// Manages the Free Spins bonus feature.
    /// Free spins are awarded when special bonus combinations hit (e.g. 2 or 3 Wilds).
    /// Spins during this mode do not deduct credits from player balance and apply a win multiplier.
    /// </summary>
    public class FreeSpinsManager : MonoBehaviour
    {
        [Header("Bonus Parameters")]
        [SerializeField] private float bonusMultiplier = 2.0f;

        public event Action<int, float> OnFreeSpinsStarted;
        public event Action<int> OnFreeSpinExecuted;
        public event Action<int> OnFreeSpinsEnded;

        private int _remainingSpins = 0;
        private int _accumulatedWinnings = 0;
        private bool _isActive = false;

        public bool IsActive => _isActive;
        public int RemainingSpins => _remainingSpins;
        public float BonusMultiplier => _isActive ? bonusMultiplier : 1.0f;
        public int AccumulatedWinnings => _accumulatedWinnings;

        /// <summary>
        /// Awards free spins to the player and activates bonus mode.
        /// </summary>
        public void AwardFreeSpins(int spinsCount, float multiplier = 2.0f)
        {
            bonusMultiplier = multiplier;
            _remainingSpins += spinsCount;
            _accumulatedWinnings = 0;
            _isActive = true;

            OnFreeSpinsStarted?.Invoke(_remainingSpins, bonusMultiplier);
        }

        /// <summary>
        /// Consumes one free spin before executing the round.
        /// </summary>
        public void ConsumeSpin()
        {
            if (!_isActive || _remainingSpins <= 0) return;

            _remainingSpins--;
            OnFreeSpinExecuted?.Invoke(_remainingSpins);
        }

        /// <summary>
        /// Accumulates winnings earned during the free spins round.
        /// </summary>
        public void AddWinnings(int amount)
        {
            _accumulatedWinnings += amount;
        }

        /// <summary>
        /// Checks if the free spins feature has concluded.
        /// </summary>
        public bool CheckIfFinished()
        {
            if (_isActive && _remainingSpins <= 0)
            {
                _isActive = false;
                OnFreeSpinsEnded?.Invoke(_accumulatedWinnings);
                return true;
            }
            return false;
        }

        public void ResetBonus()
        {
            _isActive = false;
            _remainingSpins = 0;
            _accumulatedWinnings = 0;
        }
    }
}
