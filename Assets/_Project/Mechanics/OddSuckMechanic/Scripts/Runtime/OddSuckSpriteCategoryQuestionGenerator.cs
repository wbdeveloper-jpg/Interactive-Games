using System;
using System.Collections.Generic;
using UnityEngine;

namespace OddSuckMechanic
{
    [Serializable]
    public class OddSuckSpriteCategory
    {
        public string categoryName = "Category";
        public List<Sprite> sprites = new List<Sprite>();
    }

    public class OddSuckSpriteCategoryQuestionGenerator : OddSuckQuestionGeneratorBase
    {
        [Header("Sprite Categories")]
        [SerializeField] private string questionText = "Pick the odd one";
        [SerializeField] private List<OddSuckSpriteCategory> categories = new List<OddSuckSpriteCategory>();
        [SerializeField] private bool allowRepeatedSpritesInSameWave = true;
        [SerializeField] private bool useCategoryNameAsHiddenDebugText = false;

        public override bool CanGenerate()
        {
            return GetUsableCategoryCount() >= 2;
        }

        public override OddSuckGeneratedQuestion Generate(int waveIndex)
        {
            List<OddSuckSpriteCategory> usable = GetUsableCategories();
            Shuffle(usable);

            OddSuckSpriteCategory majorityCategory = usable[0];
            OddSuckSpriteCategory oddCategory = usable[1];
            int itemCount = GetRandomItemCount();

            OddSuckGeneratedQuestion question = new OddSuckGeneratedQuestion
            {
                displayMode = OddSuckItemDisplayMode.Sprite,
                questionText = string.IsNullOrWhiteSpace(questionText) ? "Pick the odd one" : questionText
            };

            HashSet<Sprite> usedSprites = new HashSet<Sprite>();

            for (int i = 0; i < itemCount - 1; i++)
            {
                Sprite sprite = PickSprite(majorityCategory, usedSprites);
                question.items.Add(new OddSuckItemData
                {
                    displayText = useCategoryNameAsHiddenDebugText ? majorityCategory.categoryName : string.Empty,
                    icon = sprite,
                    isOdd = false
                });
            }

            Sprite oddSprite = PickSprite(oddCategory, usedSprites);
            question.items.Add(new OddSuckItemData
            {
                displayText = useCategoryNameAsHiddenDebugText ? oddCategory.categoryName : string.Empty,
                icon = oddSprite,
                isOdd = true
            });

            Shuffle(question.items);
            return question;
        }

        private Sprite PickSprite(OddSuckSpriteCategory category, HashSet<Sprite> usedSprites)
        {
            if (category == null || category.sprites == null || category.sprites.Count == 0)
            {
                return null;
            }

            for (int attempt = 0; attempt < 40; attempt++)
            {
                Sprite sprite = category.sprites[UnityEngine.Random.Range(0, category.sprites.Count)];
                if (allowRepeatedSpritesInSameWave || sprite == null || !usedSprites.Contains(sprite))
                {
                    if (sprite != null)
                    {
                        usedSprites.Add(sprite);
                    }

                    return sprite;
                }
            }

            return category.sprites[UnityEngine.Random.Range(0, category.sprites.Count)];
        }

        private int GetUsableCategoryCount()
        {
            int count = 0;
            for (int i = 0; i < categories.Count; i++)
            {
                if (IsCategoryUsable(categories[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private List<OddSuckSpriteCategory> GetUsableCategories()
        {
            List<OddSuckSpriteCategory> usable = new List<OddSuckSpriteCategory>();
            for (int i = 0; i < categories.Count; i++)
            {
                if (IsCategoryUsable(categories[i]))
                {
                    usable.Add(categories[i]);
                }
            }

            return usable;
        }

        private static bool IsCategoryUsable(OddSuckSpriteCategory category)
        {
            return category != null && category.sprites != null && category.sprites.Count > 0;
        }
    }
}
