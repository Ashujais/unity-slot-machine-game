using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Data;

namespace SlotGame.Reels
{
    /// <summary>
    /// Represents an individual visual symbol cell on a reel strip.
    /// Handles sprite assignment, scaling animations, and winning highlight effects.
    /// </summary>
    public class SymbolItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private Image highlightGlow;
        [SerializeField] private RectTransform rectTransform;

        [Header("Animation Settings")]
        [SerializeField] private float bounceScale = 1.15f;
        [SerializeField] private float pulseDuration = 0.6f;

        private SymbolType _currentType;
        private Coroutine _pulseCoroutine;
        private Vector3 _originalScale;

        public SymbolType CurrentType => _currentType;
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            _originalScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

            if (highlightGlow != null)
            {
                highlightGlow.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets the symbol visual data and sprite.
        /// </summary>
        public void SetSymbol(SymbolType type, Sprite sprite)
        {
            _currentType = type;
            if (symbolImage != null)
            {
                symbolImage.sprite = sprite;
                symbolImage.enabled = sprite != null;
            }
        }

        /// <summary>
        /// Plays a pulsating win celebration animation when part of a winning line.
        /// </summary>
        public void PlayWinAnimation()
        {
            StopWinAnimation();
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        public void StopWinAnimation()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = _originalScale;
            }

            if (highlightGlow != null)
            {
                highlightGlow.gameObject.SetActive(false);
            }
        }

        private IEnumerator PulseRoutine()
        {
            if (highlightGlow != null) highlightGlow.gameObject.SetActive(true);

            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime;
                float progress = Mathf.PingPong(timer / pulseDuration, 1f);
                float scale = Mathf.Lerp(1.0f, bounceScale, Mathf.SmoothStep(0f, 1f, progress));

                if (rectTransform != null)
                {
                    rectTransform.localScale = _originalScale * scale;
                }

                if (highlightGlow != null)
                {
                    Color c = highlightGlow.color;
                    c.a = Mathf.Lerp(0.3f, 0.9f, progress);
                    highlightGlow.color = c;
                }

                yield return null;
            }
        }
    }
}
