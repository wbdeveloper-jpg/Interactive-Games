/*
 * ============================================================
 * IGameSceneCallbacks.cs  —  Interface
 * ============================================================
 * PURPOSE:
 *   Contract that EVERY game scene manager must implement.
 *   RewardManager calls these two functions when the player
 *   presses "Play Again" or "Home" on the post-game panel.
 *
 * SETUP IN EACH GAME SCENE:
 *   Add this to your game's main manager MonoBehaviour:
 *
 *       public class MyGameManager : MonoBehaviour, IGameSceneCallbacks
 *       {
 *           public void OnPlayAgain()
 *           {
 *               SceneManager.LoadScene(SceneManager.GetActiveScene().name);
 *           }
 *
 *           public void OnHome()
 *           {
 *               SceneManager.LoadScene("LoadingScene");
 *           }
 *       }
 *
 * HOW REWARD MANAGER FINDS IT:
 *   RewardManager uses FindObjectOfType to locate any MonoBehaviour
 *   that implements this interface in the current scene.
 *   As long as ONE object in the scene implements it, it works.
 *
 * RULES:
 *   • Keep function names exactly as defined — do not rename.
 *   • One implementing object per scene is enough.
 *   • Both functions must be implemented (no optional members).
 * ============================================================
 */

namespace RewardSystem
{
    public interface IGameSceneCallbacks
    {
        /// <summary>Called when player taps "Play Again" on post-game panel.</summary>
        void OnPlayAgain();

        /// <summary>Called when player taps "Home" on post-game panel.</summary>
        void OnHome();
    }

    /// <summary>
    /// Optional secondary interface — implement this if your game scene
    /// needs to pause or stop its own audio when the reward screen opens.
    /// RewardManager checks for this interface separately — NOT implementing
    /// it will NOT break the reward system or IGameSceneCallbacks.
    ///
    /// USAGE IN GAME SCENE:
    ///   public class MyGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    ///   {
    ///       public void OnRewardScreenOpen()
    ///       {
    ///           AudioManager.Instance.StopBGM();
    ///       }
    ///   }
    /// </summary>
    public interface IGameAudioCallbacks
    {
        /// <summary>
        /// Called by RewardManager just before post-game panel appears.
        /// Use this to stop or pause your game scene's audio manager.
        /// </summary>
        void OnRewardScreenOpen();
    }
}