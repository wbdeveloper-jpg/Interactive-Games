/*
 * ============================================================
 * InfoSkillButton.cs  —  MonoBehaviour on InfoSkillButton Prefab
 * ============================================================
 * PURPOSE:
 *   A simple skill selector button inside the Info Panel.
 *   Shows the skill icon and name. On click, fires callback
 *   to open the modal with that skill's full data.
 *
 * PREFAB HIERARCHY:
 *   InfoSkillButton (Button + this script)
 *     ├── Icon     (Image)
 *     └── Name     (TextMeshProUGUI)
 *
 * SETUP:
 *   Assign icon and nameText in inspector.
 *   InfoPanel calls Setup() after instantiation.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    public class InfoSkillButton : MonoBehaviour
    {
        [SerializeField] private Image            icon;
        [SerializeField] private TextMeshProUGUI  nameText;

        public Button _button;

        private void Awake()
        {
            //_button = GetComponent<Button>();
        }

        /// <summary>Populate this button and wire its click callback.</summary>
        public void Setup(BloomSkillData data, System.Action onClick)
        {
            icon.sprite   = data.icon;
            nameText.text = data.skillName;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
