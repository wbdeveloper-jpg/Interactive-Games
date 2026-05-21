/*
 * ============================================================
 * InfoSkillButton.cs  —  MonoBehaviour on InfoSkillButton Prefab
 * ============================================================
 * PURPOSE:
 *   Skill selector button inside the Info Panel.
 *   Minimalistic design — icon and skill name only.
 *   Both icon and text are tinted with the skill's assigned color.
 *
 * PREFAB HIERARCHY:
 *   InfoSkillButton  (Button + this script)
 *     ├── Icon     (Image — gets skillColor as tint)
 *     └── Name     (TextMeshProUGUI — gets skillColor)
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
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;

        public Button _button;

        //private void Awake()
        //{
        //    _button = GetComponent<Button>();
        //}

        /// <summary>Populate button with skill data, apply color, wire click callback.</summary>
        public void Setup(BloomSkillData data, System.Action onClick)
        {
            icon.sprite = data.icon;
            nameText.text = data.skillName;

            // Apply skill color to icon tint and text
            icon.color = data.skillColor;
            nameText.color = data.skillColor;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}