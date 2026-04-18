using UnityEngine;

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance;
    public bool HasColorSelected { get; private set; } = false;
    public ColorData SelectedColorData { get; private set; }

    private ColorButton currentButton;

    private void Awake()
    {
        Instance = this;
    }

    public void SetColor(ColorData data, ColorButton button)
    {
        if (currentButton == button) return;

        if (currentButton != null)
            currentButton.SetHighlight(false);

        currentButton = button;
        SelectedColorData = data;
        HasColorSelected = true;
        GirlsGameManager.instance.ShowMessage(data.colorName, data.color);
        currentButton.SetHighlight(true);
    }
}