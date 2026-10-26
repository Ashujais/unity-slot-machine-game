using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SlotGame.Audio;

namespace SlotGame.UI
{
    /// <summary>
    /// Interactive mechanical lever on the side of the slot machine.
    /// Supports mouse click, dragging downward, touch input, and keyboard Spacebar.
    /// Features authentic mechanical pull and spring-release recoil animation.
    /// </summary>
    public class LeverController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image leverImage;
        [SerializeField] private Sprite leverUpSprite;
        [SerializeField] private Sprite leverDownSprite;
        [SerializeField] private RectTransform leverTransform;

        [Header("Animation Settings")]
        [SerializeField] private float pullDistance = 80f;
        [SerializeField] private float returnDuration = 0.22f;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        public event Action OnLeverPulled;

        private bool _isInteractable = true;
        private bool _isPulled = false;
        private Vector2 _initialPosition;
        private Coroutine _returnCoroutine;

        private void Awake()
        {
            if (leverTransform == null) leverTransform = GetComponent<RectTransform>();
            _initialPosition = leverTransform != null ? leverTransform.anchoredPosition : Vector2.zero;

            SetLeverSprite(false);
        }

        private void Update()
        {
            // Keyboard shortcut (Spacebar triggers lever pull)
            if (_isInteractable && Input.GetKeyDown(KeyCode.Space))
            {
                TriggerPull();
            }
        }

        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
        }

        public void TriggerPull()
        {
            if (!_isInteractable || _isPulled) return;

            StartCoroutine(AutoPullRoutine());
        }

        private IEnumerator AutoPullRoutine()
        {
            _isPulled = true;
            SetLeverSprite(true);

            if (audioManager != null) audioManager.PlayLeverPull();
            OnLeverPulled?.Invoke();

            // Down position hold
            yield return new WaitForSeconds(0.15f);

            // Spring return
            yield return ReturnToRestPosition();

            _isPulled = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isInteractable || _isPulled) return;
            SetLeverSprite(true);
            if (audioManager != null) audioManager.PlayLeverPull();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isInteractable || _isPulled) return;

            float deltaY = Mathf.Clamp(eventData.position.y - eventData.pressPosition.y, -pullDistance, 0f);
            if (leverTransform != null)
            {
                leverTransform.anchoredPosition = _initialPosition + new Vector2(0, deltaY);
            }

            if (deltaY <= -pullDistance * 0.75f && !_isPulled)
            {
                _isPulled = true;
                OnLeverPulled?.Invoke();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isInteractable) return;

            if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
            _returnCoroutine = StartCoroutine(ReturnToRestPosition());
        }

        private IEnumerator ReturnToRestPosition()
        {
            float timer = 0f;
            Vector2 currentPos = leverTransform != null ? leverTransform.anchoredPosition : _initialPosition;

            while (timer < returnDuration)
            {
                timer += Time.deltaTime;
                float t = timer / returnDuration;
                // Elastic spring ease out
                float spring = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
                if (leverTransform != null)
                {
                    leverTransform.anchoredPosition = Vector2.Lerp(currentPos, _initialPosition, Mathf.Clamp01(spring));
                }
                yield return null;
            }

            if (leverTransform != null) leverTransform.anchoredPosition = _initialPosition;
            SetLeverSprite(false);
            _isPulled = false;
        }

        private void SetLeverSprite(bool isDown)
        {
            if (leverImage == null) return;
            leverImage.sprite = isDown ? leverDownSprite : leverUpSprite;
        }
    }
}
