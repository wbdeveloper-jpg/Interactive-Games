using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OddClawSpriteCategory
{
    public string categoryName;
    public List<Sprite> sprites = new List<Sprite>();
}

public enum OddClawSpriteQuestionMode
{
    OneOddCategoryAmongSameCategory,
    ImageBasedAnswers
}

[CreateAssetMenu(menuName = "Odd Claw Catch/Question Generators/Sprite Category Generator", fileName = "OddClawSpriteQuestionGenerator")]
public class OddClawSpriteQuestionGenerator : OddClawQuestionGeneratorBase
{
    [Header("Sprite Mode")]
    public OddClawSpriteQuestionMode mode = OddClawSpriteQuestionMode.OneOddCategoryAmongSameCategory;

    [Header("Categories")]
    public List<OddClawSpriteCategory> categories = new List<OddClawSpriteCategory>();

    [Header("Fallback")]
    public OddClawMathQuestionGenerator fallbackMathGenerator;

    public override OddClawQuestionData GenerateQuestion(int wave, int requestedOptionCount)
    {
        int optionCount = ClampOptionCount(requestedOptionCount);

        if (!HasEnoughCategoryData(optionCount))
        {
            if (fallbackMathGenerator != null)
                return fallbackMathGenerator.GenerateQuestion(wave, requestedOptionCount);

            return BuildFallbackTextQuestion(optionCount);
        }

        return mode == OddClawSpriteQuestionMode.OneOddCategoryAmongSameCategory
            ? GenerateOddCategoryQuestion(optionCount)
            : GenerateImageBasedQuestion(optionCount);
    }

    private bool HasEnoughCategoryData(int optionCount)
    {
        int validCategories = 0;
        for (int i = 0; i < categories.Count; i++)
        {
            if (categories[i] != null && categories[i].sprites != null && categories[i].sprites.Count > 0)
                validCategories++;
        }

        return validCategories >= 2 && optionCount >= 2;
    }

    private OddClawQuestionData GenerateOddCategoryQuestion(int optionCount)
    {
        OddClawSpriteCategory commonCategory = GetRandomValidCategory(null);
        OddClawSpriteCategory oddCategory = GetRandomValidCategory(commonCategory);

        List<OddClawAnswerOption> options = new List<OddClawAnswerOption>();
        Sprite oddSprite = GetRandomSprite(oddCategory);
        options.Add(new OddClawAnswerOption(oddCategory.categoryName, oddSprite, oddCategory.categoryName));

        int guard = 0;
        while (options.Count < optionCount && guard < 100)
        {
            guard++;
            Sprite sprite = GetRandomSprite(commonCategory);
            if (sprite == null)
                continue;

            options.Add(new OddClawAnswerOption(commonCategory.categoryName, sprite, commonCategory.categoryName));
        }

        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = "Catch the image that does not belong",
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Sprite
        };
    }

    private OddClawQuestionData GenerateImageBasedQuestion(int optionCount)
    {
        OddClawSpriteCategory targetCategory = GetRandomValidCategory(null);
        Sprite targetSprite = GetRandomSprite(targetCategory);

        List<OddClawAnswerOption> options = new List<OddClawAnswerOption>
        {
            new OddClawAnswerOption(targetCategory.categoryName, targetSprite, targetCategory.categoryName)
        };

        int guard = 0;
        while (options.Count < optionCount && guard < 100)
        {
            guard++;
            OddClawSpriteCategory other = GetRandomValidCategory(targetCategory);
            Sprite sprite = GetRandomSprite(other);
            if (sprite != null)
                options.Add(new OddClawAnswerOption(other.categoryName, sprite, other.categoryName));
        }

        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = "Catch the " + targetCategory.categoryName,
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Sprite
        };
    }

    private OddClawSpriteCategory GetRandomValidCategory(OddClawSpriteCategory excluded)
    {
        List<OddClawSpriteCategory> valid = new List<OddClawSpriteCategory>();
        for (int i = 0; i < categories.Count; i++)
        {
            OddClawSpriteCategory category = categories[i];
            if (category == null || category == excluded || category.sprites == null || category.sprites.Count == 0)
                continue;

            valid.Add(category);
        }

        if (valid.Count == 0)
            return null;

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private Sprite GetRandomSprite(OddClawSpriteCategory category)
    {
        if (category == null || category.sprites == null || category.sprites.Count == 0)
            return null;

        return category.sprites[UnityEngine.Random.Range(0, category.sprites.Count)];
    }

    private OddClawQuestionData BuildFallbackTextQuestion(int optionCount)
    {
        List<OddClawAnswerOption> options = new List<OddClawAnswerOption>();
        for (int i = 0; i < optionCount; i++)
            options.Add(new OddClawAnswerOption((i + 1).ToString()));

        return new OddClawQuestionData
        {
            questionText = "Assign sprite categories to use image questions. Catch 1 for now.",
            answerOptions = options,
            correctAnswerIndex = 0,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }
}
