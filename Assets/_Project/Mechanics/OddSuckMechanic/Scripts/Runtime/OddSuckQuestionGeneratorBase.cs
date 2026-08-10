using System;
using System.Collections.Generic;
using UnityEngine;

namespace OddSuckMechanic
{
    public enum OddSuckItemDisplayMode
    {
        Text,
        Sprite
    }

    public enum OddSuckDifficultyMode
    {
        Normal,
        Easy
    }

    public enum OddSuckPlayMode
    {
        MathOnly = 0,
        SpriteOnly = 1,
        MixedRandom = 2,
        EnglishOnly = 3,
        GeneralQuestionOnly = 4
    }

    [Serializable]
    public class OddSuckItemData
    {
        public string displayText;
        public Sprite icon;
        public bool isOdd;
    }

    [Serializable]
    public class OddSuckGeneratedQuestion
    {
        public string questionText = "Find the odd one";
        public OddSuckItemDisplayMode displayMode = OddSuckItemDisplayMode.Text;
        public List<OddSuckItemData> items = new List<OddSuckItemData>();
    }

    public abstract class OddSuckQuestionGeneratorBase : MonoBehaviour
    {
        [Header("Generator")]
        [SerializeField] private string generatorName = "Question Generator";
        [SerializeField, Min(1)] private int selectionWeight = 1;

        [Header("Wave Size")]
        [SerializeField, Min(3)] private int minItems = 4;
        [SerializeField, Min(3)] private int maxItems = 6;

        public string GeneratorName => generatorName;
        public int SelectionWeight => Mathf.Max(1, selectionWeight);

        public abstract bool CanGenerate();
        public abstract OddSuckGeneratedQuestion Generate(int waveIndex);

        protected int GetRandomItemCount()
        {
            int safeMin = Mathf.Max(3, minItems);
            int safeMax = Mathf.Max(safeMin, maxItems);
            return UnityEngine.Random.Range(safeMin, safeMax + 1);
        }

        protected static void Shuffle<T>(IList<T> list)
        {
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}
