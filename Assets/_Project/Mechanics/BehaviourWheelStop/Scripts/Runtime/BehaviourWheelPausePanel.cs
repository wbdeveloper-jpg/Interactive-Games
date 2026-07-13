using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    public class BehaviourWheelPausePanel : MonoBehaviour
    {
        [Header("Buttons")]
        public Button resumeButton;
        public Button howToPlayButton;
        public Button restartRoundButton;
        public Button homeButton;

        public void SetButtons(UnityAction resume, UnityAction howToPlay, UnityAction restart, UnityAction home)
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(resume);
            }

            if (howToPlayButton != null)
            {
                howToPlayButton.onClick.RemoveAllListeners();
                howToPlayButton.onClick.AddListener(howToPlay);
            }

            if (restartRoundButton != null)
            {
                restartRoundButton.onClick.RemoveAllListeners();
                restartRoundButton.onClick.AddListener(restart);
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveAllListeners();
                homeButton.onClick.AddListener(home);
            }
        }
    }
}
