using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Data;

namespace SlotGame.Reels
{
    /// <summary>
    /// Controls a single vertical reel in the slot machine.
    /// Handles infinite symbol scrolling, acceleration, high-speed cycling,
    /// target symbol stopping, and realistic physical overshoot/bounce.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Identity")]
        [SerializeField] private int reelIndex = 0;

        [Header("Layout Settings")]
        [SerializeField] private RectTransform reelContainer;
        [SerializeField] private float symbolHeight = 110f;
        [SerializeField] private float symbolSpacing = 10f;
        [SerializeField] private int visibleSymbolCount = 3; // Top, Center, Bottom

        [Header("Spin Dynamics")]
        [SerializeField] private float maxSpinSpeed = 1600f; // px per second
        [SerializeField] private float accelerationTime = 0.35f;
        [SerializeField] private float kickbackDistance = 25f;
        [SerializeField] private float bounceOvershootDistance = 28f;
        [SerializeField] private float bounceDuration = 0.28f;

        [Header("Symbol Sprites Dictionary")]
        [SerializeField] private Sprite sevenSprite;
        [SerializeField] private Sprite cherrySprite;
        [SerializeField] private Sprite bellSprite;
        [SerializeField] private Sprite barSprite;
        [SerializeField] private Sprite wildSprite;

        [Header("Symbol Pool")]
        [SerializeField] private List<SymbolItem> symbolItems = new List<SymbolItem>();

        // Events
        public event Action<int> OnReelSpinStarted;
        public event Action<int, SymbolType> OnReelStopped;

        private bool _isSpinning = false;
        private bool _isStopping = false;
        private SymbolType _targetSymbol;
        private float _currentSpeed = 0f;
        private Coroutine _spinCoroutine;

        public int ReelIndex => reelIndex;
        public bool IsSpinning => _isSpinning;
        public SymbolType TargetSymbol => _targetSymbol;

        /// <summary>
        /// Gets the symbol currently positioned at the center payline.
        /// </summary>
        public SymbolItem CenterSymbolItem => symbolItems.Count > 1 ? symbolItems[1] : null;

        /// <summary>
        /// Starts the reel spin animation.
        /// </summary>
        public void StartSpin()
        {
            if (_isSpinning) return;

            if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
            _spinCoroutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>
        /// Requests the reel to decelerate and land on the specified target symbol.
        /// </summary>
        /// <param name="target">The SymbolType to display on the center payline</param>
        public void StopAtSymbol(SymbolType target)
        {
            _targetSymbol = target;
            _isStopping = true;
        }

        private IEnumerator SpinRoutine()
        {
            _isSpinning = true;
            _isStopping = false;
            _currentSpeed = 0f;

            OnReelSpinStarted?.Invoke(reelIndex);

            // 1. Kickback Anticipation (slight upward move before rushing down)
            float kickbackTimer = 0f;
            float kickbackDuration = 0.18f;
            Vector2 initialContainerPos = reelContainer.anchoredPosition;

            while (kickbackTimer < kickbackDuration)
            {
                kickbackTimer += Time.deltaTime;
                float t = kickbackTimer / kickbackDuration;
                float offset = Mathf.Sin(t * Mathf.PI * 0.5f) * kickbackDistance;
                reelContainer.anchoredPosition = initialContainerPos + new Vector2(0, offset);
                yield return null;
            }

            // 2. Acceleration Phase
            float accelTimer = 0f;
            while (accelTimer < accelerationTime)
            {
                accelTimer += Time.deltaTime;
                _currentSpeed = Mathf.Lerp(0f, maxSpinSpeed, accelTimer / accelerationTime);
                ScrollSymbols(_currentSpeed * Time.deltaTime);
                yield return null;
            }
            _currentSpeed = maxSpinSpeed;

            // 3. Constant High-Speed Cycling Phase
            while (!_isStopping)
            {
                ScrollSymbols(_currentSpeed * Time.deltaTime);
                yield return null;
            }

            // 4. Deceleration & Alignment Phase
            // Align center slot to target symbol
            SetSymbolSpritesForStop(_targetSymbol);

            // 5. Overshoot & Elastic Bounce-back (Classic slot machine physical snap)
            float bounceTimer = 0f;
            Vector2 startPos = reelContainer.anchoredPosition;
            Vector2 overshootPos = initialContainerPos - new Vector2(0, bounceOvershootDistance);

            // Snap down overshoot
            while (bounceTimer < bounceDuration * 0.5f)
            {
                bounceTimer += Time.deltaTime;
                float progress = bounceTimer / (bounceDuration * 0.5f);
                reelContainer.anchoredPosition = Vector2.Lerp(startPos, overshootPos, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            // Spring back to resting center position
            bounceTimer = 0f;
            while (bounceTimer < bounceDuration * 0.5f)
            {
                bounceTimer += Time.deltaTime;
                float progress = bounceTimer / (bounceDuration * 0.5f);
                // Ease out elastic spring
                float spring = 1f - Mathf.Pow(1f - progress, 3f);
                reelContainer.anchoredPosition = Vector2.Lerp(overshootPos, initialContainerPos, spring);
                yield return null;
            }

            reelContainer.anchoredPosition = initialContainerPos;
            _isSpinning = false;
            _isStopping = false;

            OnReelStopped?.Invoke(reelIndex, _targetSymbol);
        }

        private void ScrollSymbols(float deltaY)
        {
            float totalSlotHeight = symbolHeight + symbolSpacing;

            for (int i = 0; i < symbolItems.Count; i++)
            {
                RectTransform rt = symbolItems[i].RectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y -= deltaY;

                // When symbol moves below visible buffer, recycle to the top
                float bottomThreshold = -2.5f * totalSlotHeight;
                if (pos.y < bottomThreshold)
                {
                    pos.y += symbolItems.Count * totalSlotHeight;
                    // Assign random symbol while fast spinning
                    AssignRandomSprite(symbolItems[i]);
                }

                rt.anchoredPosition = pos;
            }
        }

        private void AssignRandomSprite(SymbolItem item)
        {
            SymbolType rand = (SymbolType)UnityEngine.Random.Range(0, 5);
            item.SetSymbol(rand, GetSpriteForType(rand));
        }

        private void SetSymbolSpritesForStop(SymbolType centerTarget)
        {
            if (symbolItems.Count < 3) return;

            // Top symbol
            SymbolType top = (SymbolType)(((int)centerTarget + 1) % 5);
            symbolItems[0].SetSymbol(top, GetSpriteForType(top));

            // Center target (the evaluated payline symbol)
            symbolItems[1].SetSymbol(centerTarget, GetSpriteForType(centerTarget));

            // Bottom symbol
            SymbolType bottom = (SymbolType)(((int)centerTarget + 4) % 5);
            symbolItems[2].SetSymbol(bottom, GetSpriteForType(bottom));
        }

        public Sprite GetSpriteForType(SymbolType type)
        {
            switch (type)
            {
                case SymbolType.Seven: return sevenSprite;
                case SymbolType.Cherry: return cherrySprite;
                case SymbolType.Bell: return bellSprite;
                case SymbolType.Bar: return barSprite;
                case SymbolType.Wild: return wildSprite;
                default: return barSprite;
            }
        }

        public void HighlightWinningSymbol()
        {
            if (CenterSymbolItem != null)
            {
                CenterSymbolItem.PlayWinAnimation();
            }
        }

        public void ClearHighlights()
        {
            foreach (var item in symbolItems)
            {
                item.StopWinAnimation();
            }
        }
    }
}
