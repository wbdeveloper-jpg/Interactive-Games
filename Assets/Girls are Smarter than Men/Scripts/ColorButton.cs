using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public Image colorImage;      // The visible color
    public GameObject highlight;  // Highlight object (child UI)
    public Color highlightedColor;
    public Color lowlightedColor;
    public ColorData colorData;

    public void OnClick()
    {
        ColorManager.Instance.SetColor(colorData, this);
        TutorialController.Instance?.OnColorSelected();
    }

    public void SetHighlight(bool state)
    {
        //highlight.SetActive(state);

        Image _highLight = highlight.GetComponent<Image>();

        if (state)
        {
            _highLight.color = highlightedColor;
        }
        else { 
            _highLight.color =lowlightedColor; 
        }
    }
}