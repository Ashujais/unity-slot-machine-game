using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Data;
using SlotGame.Audio;

namespace SlotGame.Reels
{
    /// <summary>
    /// Coordinates the full 3-reel slot grid.
    /// Manages staggered stop sequencing, near-miss anticipation timing,
    /// and dispatches grid-level events to the GameManager.
    /// </summary>
    public class ReelGridManager : MonoBehaviour
    {
        [Header("Reel Components")]
        [SerializeField] private ReelController[] reels = new ReelController[3];

        [Header("Staggered Timing Settings")]
        [Tooltip("Minimum spin duration before the first reel starts stopping (seconds)")]
        [SerializeField] private float baseSpinDuration = 1.2f;

        [Tooltip("Delay between subsequent reel stops (seconds)")]
        [SerializeField] private float staggerInterval = 0.4f;

        [Tooltip("Extra spin duration applied when first 2 reels match to build suspense")]
        [SerializeField] private float anticipationDuration = 1.2f;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        public event Action<SymbolType[]> OnAllReelsStopped;
        public event Action OnAnticipationTriggered;

        private SymbolType[] _targetOutcomes = new SymbolType[3];
        private int _reelsStoppedCount = 0;
        private Coroutine _stopSequenceCoroutine;

        public bool IsSpinning => _reelsStoppedCount < reels.Length;

        private void Start()
        {
            for (int i = 0; i < reels.Length; i++)
            {
                int index = i;
                reels[i].OnReelStopped += (idx, sym) => HandleIndividualReelStopped(idx, sym);
            }
        }

        /// <summary>
        /// Starts all reels spinning simultaneously.
        /// </summary>
        /// <param name="outcomes">The predetermined 3-symbol target outcome from RNGManager</param>
        public void StartGridSpin(SymbolType[] outcomes)
        {
            _targetOutcomes = outcomes;
            _reelsStoppedCount = 0;
            ClearAllHighlights();

            if (audioManager != null)
            {
                audioManager.PlayReelSpin();
            }

            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].StartSpin();
            }

            if (_stopSequenceCoroutine != null) StopCoroutine(_stopSequenceCoroutine);
            _stopSequenceCoroutine = StartCoroutine(StaggeredStopSequence());
        }

        private IEnumerator StaggeredStopSequence()
        {
            // 1. Initial spin duration
            yield return new WaitForSeconds(baseSpinDuration);

            // 2. Stop Reel 0
            reels[0].StopAtSymbol(_targetOutcomes[0]);

            // 3. Wait stagger interval then stop Reel 1
            yield return new WaitForSeconds(staggerInterval);
            reels[1].StopAtSymbol(_targetOutcomes[1]);

            // 4. Check for Anticipation on Reel 2
            // If reels 0 and 1 match (or either is Wild), reel 2 has jackpot/win potential!
            bool isAnticipation = CheckAnticipationOpportunity(_targetOutcomes[0], _targetOutcomes[1]);

            if (isAnticipation)
            {
                OnAnticipationTriggered?.Invoke();
                if (audioManager != null)
                {
                    audioManager.PlayAnticipation();
                }
                yield return new WaitForSeconds(anticipationDuration);
            }
            else
            {
                yield return new WaitForSeconds(staggerInterval);
            }

            // 5. Stop Reel 2
            reels[2].StopAtSymbol(_targetOutcomes[2]);
        }

        private bool CheckAnticipationOpportunity(SymbolType s0, SymbolType s1)
        {
            if (s0 == SymbolType.Wild || s1 == SymbolType.Wild) return true;
            return s0 == s1;
        }

        private void HandleIndividualReelStopped(int reelIndex, SymbolType landedSymbol)
        {
            _reelsStoppedCount++;

            if (audioManager != null)
            {
                audioManager.PlayReelStop();
            }

            if (_reelsStoppedCount >= reels.Length)
            {
                if (audioManager != null)
                {
                    audioManager.StopReelSpin();
                }

                OnAllReelsStopped?.Invoke(_targetOutcomes);
            }
        }

        public void HighlightWinningReels(List<int> reelIndices)
        {
            if (reelIndices == null) return;

            foreach (int idx in reelIndices)
            {
                if (idx >= 0 && idx < reels.Length)
                {
                    reels[idx].HighlightWinningSymbol();
                }
            }
        }

        public void ClearAllHighlights()
        {
            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].ClearHighlights();
            }
        }
    }
}
