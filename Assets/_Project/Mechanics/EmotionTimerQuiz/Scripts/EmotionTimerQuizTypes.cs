using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmotionTimerQuiz
{
    public enum CharacterType
    {
        RAJ,
        TINA,
        TANVI,
        RAJES
    }

    public enum ExpressionType
    {
        HAPPY,
        SAD,
        ANGRY,
        SCARED,
        CONFIDENT,
        EXCITED
    }

    public enum EmotionQuizState
    {
        None,
        Loading,
        ShowingHowToPlay,
        Tutorial,
        Playing,
        AnswerLocked,
        Timeout,
        Paused,
        Result
    }

    public enum HowToPlayDisplayMode
    {
        FirstTimeAutomatically,
        EveryGameStartAutomatically,
        ManualButtonOnly
    }

    [Serializable]
    public class ExpressionSpriteEntry
    {
        public ExpressionType expression;
        public Sprite sprite;
    }

    [Serializable]
    public class CharacterSpriteEntry
    {
        public CharacterType character;
        public List<ExpressionSpriteEntry> expressionSprites = new List<ExpressionSpriteEntry>();

        public Sprite GetSprite(ExpressionType expression)
        {
            for (int i = 0; i < expressionSprites.Count; i++)
            {
                if (expressionSprites[i] != null && expressionSprites[i].expression == expression)
                {
                    return expressionSprites[i].sprite;
                }
            }

            return null;
        }

        public List<ExpressionType> GetRegisteredExpressions()
        {
            List<ExpressionType> result = new List<ExpressionType>();

            for (int i = 0; i < expressionSprites.Count; i++)
            {
                if (expressionSprites[i] == null)
                {
                    continue;
                }

                if (!result.Contains(expressionSprites[i].expression))
                {
                    result.Add(expressionSprites[i].expression);
                }
            }

            return result;
        }
    }

    [Serializable]
    public class SituationQuestion
    {
        public string id = "Q001";

        [TextArea(2, 5)]
        public string situationText = "Raj sees a massive spider on his bed!";

        public CharacterType targetCharacter = CharacterType.RAJ;
        public ExpressionType correctExpression = ExpressionType.SCARED;

        [Min(1)]
        public int timeLimitSeconds = 15;
    }

    [Serializable]
    public class EmotionOptionData
    {
        public ExpressionType expression;
        public Sprite sprite;
        public bool isCorrect;

        public EmotionOptionData(ExpressionType expression, Sprite sprite, bool isCorrect)
        {
            this.expression = expression;
            this.sprite = sprite;
            this.isCorrect = isCorrect;
        }
    }

    public static class EmotionTimerQuizUtility
    {
        public static readonly ExpressionType[] AllExpressions =
        {
            ExpressionType.HAPPY,
            ExpressionType.SAD,
            ExpressionType.ANGRY,
            ExpressionType.SCARED,
            ExpressionType.CONFIDENT,
            ExpressionType.EXCITED
        };

        public static readonly CharacterType[] AllCharacters =
        {
            CharacterType.RAJ,
            CharacterType.TINA,
            CharacterType.TANVI,
            CharacterType.RAJES
        };

        public static string ToDisplayText(ExpressionType expression)
        {
            return expression.ToString();
        }

        public static void FisherYatesShuffle<T>(IList<T> list)
        {
            if (list == null)
            {
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        public static List<CharacterSpriteEntry> CreateEmptySpriteRegistry()
        {
            List<CharacterSpriteEntry> registry = new List<CharacterSpriteEntry>();

            for (int c = 0; c < AllCharacters.Length; c++)
            {
                CharacterSpriteEntry characterEntry = new CharacterSpriteEntry();
                characterEntry.character = AllCharacters[c];

                for (int e = 0; e < AllExpressions.Length; e++)
                {
                    ExpressionSpriteEntry expressionEntry = new ExpressionSpriteEntry();
                    expressionEntry.expression = AllExpressions[e];
                    expressionEntry.sprite = null;
                    characterEntry.expressionSprites.Add(expressionEntry);
                }

                registry.Add(characterEntry);
            }

            return registry;
        }
    }
}
