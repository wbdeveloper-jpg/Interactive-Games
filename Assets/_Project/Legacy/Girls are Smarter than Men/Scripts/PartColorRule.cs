using System.Collections.Generic;

[System.Serializable]
public class PartColorRule
{
    public PartType partType;
    public List<ColorData> allowedColors;
}