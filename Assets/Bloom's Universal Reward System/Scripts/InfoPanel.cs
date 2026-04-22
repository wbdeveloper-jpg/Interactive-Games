/*
 * ============================================================
 * InfoPanel.cs  —  MonoBehaviour on Panel 3 GameObject
 * ============================================================
 * PURPOSE:
 *   The Info Panel shows all Bloom skill descriptions.
 *   It contains:
 *     • A row of skill buttons (one per skill) — tap to open modal
 *     • A modal overlay showing icon, heading, definition, details
 *     • A "Got It" button on the modal to close it
 *     • A "Got It" / close button on the panel itself
 *
 *   When opened from Pre-Game panel it signals PreGamePanel to
 *   pause its countdown. When closed it resumes the countdown.
 *
 * HIERARCHY:
 *   Panel_Info (this script + CanvasGroup)
 *     ├── SkillButtonHolder     (Layout Group)
 *     │     └── [SkillButton prefabs spawned here]
 *     ├── BtnGotIt              (Button — closes whole panel)
 *     └── Modal_SkillDetail
 *           ├── ModalIcon       (Image)
 *           ├── ModalHeading    (TextMeshProUGUI)
 *           ├── ModalDefinition (TextMeshProUGUI)
 *           ├── ModalDetails    (TextMeshProUGUI)
 *           └── BtnModalGotIt   (Button — closes only modal)
 *
 * SETUP:
 *   • Assign all references in inspector.
 *   • SkillButtonPrefab: simple Button + icon Image + name TMP.
 *   • RewardManager calls SetupSkills() once on init.
 *   • Call Open(fromPreGame) / Close() to show/hide.
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    public class InfoPanel : MonoBehaviour
    {
        [Header("Skill Buttons")]
        [SerializeField] private InfoSkillButton skillButtonPrefab;
        [SerializeField] private Transform       skillButtonHolder;

        [Header("Panel Close Button")]
        [SerializeField] private Button btnClose;

        [Header("Modal References")]
        [SerializeField] private GameObject      modal;
        [SerializeField] private Image           modalIcon;
        [SerializeField] private TextMeshProUGUI modalHeading;
        [SerializeField] private TextMeshProUGUI modalDefinition;
        [SerializeField] private TextMeshProUGUI modalDetails;
        [SerializeField] private Button          btnModalGotIt;

        // Callbacks set by RewardManager
        private System.Action _onOpenFromPreGame;
        private System.Action _onCloseToPreGame;
        private bool          _openedFromPreGame;

        private List<BloomSkillData> _allSkillData = new();

        // ── Unity ───────────────────────────────────────────────

        private void Awake()
        {
            btnClose.onClick.AddListener(Close);
            btnModalGotIt.onClick.AddListener(CloseModal);
            modal.SetActive(false);
            gameObject.SetActive(false);
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>
        /// Initialize skill buttons once. Called by RewardManager on startup.
        /// </summary>
        public void SetupSkills(
            List<BloomSkillData> skillDataList,
            System.Action        onOpenFromPreGame,
            System.Action        onCloseToPreGame)
        {
            _allSkillData      = skillDataList;
            _onOpenFromPreGame = onOpenFromPreGame;
            _onCloseToPreGame  = onCloseToPreGame;

            // Clear old buttons
            foreach (Transform child in skillButtonHolder)
                Destroy(child.gameObject);

            // Spawn one button per skill
            foreach (var data in skillDataList)
            {
                InfoSkillButton btn = Instantiate(skillButtonPrefab, skillButtonHolder);
                btn.Setup(data, () => OpenModal(data));
            }
        }

        /// <summary>
        /// Open the info panel.
        /// fromPreGame = true means we must pause pre-game countdown.
        /// </summary>
        public void Open(bool fromPreGame = false)
        {
            _openedFromPreGame = fromPreGame;
            gameObject.SetActive(true);
            modal.SetActive(false); // always start with modal closed

            if (fromPreGame)
                _onOpenFromPreGame?.Invoke();
        }

        /// <summary>Close the info panel entirely.</summary>
        public void Close()
        {
            modal.SetActive(false);
            gameObject.SetActive(false);

            if (_openedFromPreGame)
                _onCloseToPreGame?.Invoke();

            _openedFromPreGame = false;
        }

        // ── Private ─────────────────────────────────────────────

        private void OpenModal(BloomSkillData data)
        {
            modalIcon.sprite       = data.icon;
            modalHeading.text      = data.skillName;
            modalDefinition.text   = data.definition;
            modalDetails.text      = data.details;
            modal.SetActive(true);
        }

        private void CloseModal()
        {
            modal.SetActive(false);
        }
    }
}
