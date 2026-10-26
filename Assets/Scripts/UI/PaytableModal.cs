using UnityEngine;
using UnityEngine.UI;
using SlotGame.Audio;

namespace SlotGame.UI
{
    /// <summary>
    /// Displays an informative paytable modal outlining all winning combinations,
    /// symbol multipliers, wild rules, and free spins bonus information.
    /// </summary>
    public class PaytableModal : MonoBehaviour
    {
        [Header("Modal References")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button infoButton;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (infoButton != null) infoButton.onClick.AddListener(Toggle);

            if (modalRoot != null) modalRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (modalRoot == null) return;
            if (modalRoot.activeSelf) Hide();
            else Show();
        }

        public void Show()
        {
            if (audioManager != null) audioManager.PlayButtonClick();
            if (modalRoot != null) modalRoot.SetActive(true);
        }

        public void Hide()
        {
            if (audioManager != null) audioManager.PlayButtonClick();
            if (modalRoot != null) modalRoot.SetActive(false);
        }
    }
}
