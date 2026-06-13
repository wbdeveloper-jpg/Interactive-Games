using UnityEngine;

public abstract class SkyFallContentProviderBase : MonoBehaviour
{
    public abstract string GetPromptText(SkyFallDropContext context);
    public abstract SkyFallDropData GenerateDrop(SkyFallDropContext context);

    public virtual void OnGameStarted() { }
    public virtual void OnCorrectCatch(SkyFallDropData data) { }
    public virtual void OnWrongCatch(SkyFallDropData data) { }
    public virtual void OnCorrectMissed(SkyFallDropData data) { }
}
