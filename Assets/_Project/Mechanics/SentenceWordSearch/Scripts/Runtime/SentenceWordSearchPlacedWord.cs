using System.Collections.Generic;
using UnityEngine;

public class SentenceWordSearchPlacedWord
{
    public string word;
    public Vector2Int start;
    public Vector2Int direction;
    public readonly List<Vector2Int> cells = new List<Vector2Int>();
}
