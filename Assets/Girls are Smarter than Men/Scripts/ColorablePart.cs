using UnityEngine;
using System.Collections;
using DG.Tweening;

public class ColorablePart : MonoBehaviour
{
    public SpriteRenderer overlay; // assign in inspector
    private SpriteRenderer sr;

    public PartType partType;

    public ColorData correctColorData;


    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetCorrectColor(ColorData data)
    {
        correctColorData = data;

        //Color c = data.color;
        //c.a = 1f; // 🔥 force visible

        sr.color = data.color;
    }

    public void SetUserColor(ColorData data)
    {
        //Color c = data.color;
        //c.a = 1f; // 🔥 force visible
        sr.color = data.color;
    }

    public void ResetToWhite()
    {
        sr.DOColor(Color.white, 0.8f);
    }

    public bool IsCorrect()
    {
        //Color c = correctColorData.color;
        //c.a = 1f; // 🔥 force visible
        return Vector4.Distance(sr.color, correctColorData.color) < 0.1f;
    }
    public IEnumerator EvaluateVisual()
    {
        // ✨ Flicker using overlay (NOT main color)
        for (int i = 0; i < 5; i++)
        {
            SetOverlay(Color.white, 0.6f);
            yield return new WaitForSeconds(0.1f);

            SetOverlay(Color.clear, 0f);
            yield return new WaitForSeconds(0.1f);
        }

        // 🎯 Result
        if (!IsCorrect())
        {
            // ❌ Wrong → red overlay
            SetOverlay(Color.red, 0.5f);
            AudioManager.Instance.PlaySFX(2);
            GirlsGameManager.instance.ShowMessage("Wrong Color!", Color.red);
        }
        else
        {
            // ✅ Correct → small glow (optional)
            SetOverlay(Color.green, 0.5f);
            AudioManager.Instance.PlaySFX(1);
            yield return new WaitForSeconds(0.2f);
            GirlsGameManager.instance.ShowMessage("Correct Color!", Color.green);
            SetOverlay(Color.clear, 0f);
        }
    }

    void SetOverlay(Color color, float alpha)
    {
        if (overlay == null) return;

        color.a = alpha;
        overlay.color = color;
    }

    public void ResetVisual()
    {
        // remove overlay / outline / effects
        if (overlay != null)
        {
            overlay.color = Color.clear;
        }
    }
}