using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BehaviourWheelStop
{
    [Serializable]
    public class BehaviourWheelSharedIcon
    {
        public string answerText;
        public Sprite icon;
    }

    public class BehaviourWheelQuestionBank : MonoBehaviour
    {
        private const int MinWheelOptions = 3;
        private const int MaxWheelOptions = 6;

        [Header("Default Data")]
        [SerializeField] private bool populateDefaultsIfEmpty = true;

        [Header("Behaviour Mode Questions")]
        [Tooltip("Fixed behaviour game list. Default has 30 questions.")]
        [SerializeField] private List<BehaviourWheelQuestionData> behaviourQuestions = new List<BehaviourWheelQuestionData>();

        [Header("Behaviour Mode Shared Icons")]
        [Tooltip("Assign one icon per behaviour answer. These icons are reused by all Behaviour mode questions.")]
        [SerializeField] private List<BehaviourWheelSharedIcon> behaviourSharedIcons = new List<BehaviourWheelSharedIcon>
        {
            new BehaviourWheelSharedIcon { answerText = "Caring" },
            new BehaviourWheelSharedIcon { answerText = "Selfish" },
            new BehaviourWheelSharedIcon { answerText = "Kind" },
            new BehaviourWheelSharedIcon { answerText = "Respectful" },
            new BehaviourWheelSharedIcon { answerText = "Ignorant" },
            new BehaviourWheelSharedIcon { answerText = "Protective" }
        };

        [Header("General Mode Questions")]
        [Tooltip("Editable list for Science, EVS, GK, English, etc. Default has 25 plant science questions. Replace/add your own questions here later.")]
        [SerializeField] private List<BehaviourWheelQuestionData> generalQuestions = new List<BehaviourWheelQuestionData>();

        [Header("Runtime Math Generator")]
        public BehaviourWheelMathSettings mathSettings = new BehaviourWheelMathSettings();

        public IReadOnlyList<BehaviourWheelQuestionData> BehaviourQuestions => behaviourQuestions;
        public IReadOnlyList<BehaviourWheelQuestionData> GeneralQuestions => generalQuestions;

        private static readonly string[] BehaviourOptions =
        {
            "Caring",
            "Selfish",
            "Kind",
            "Respectful",
            "Ignorant",
            "Protective"
        };

        private void Awake()
        {
            if (populateDefaultsIfEmpty)
                PopulateMissingDefaults();
        }

        public List<BehaviourWheelQuestionData> GetRoundQuestions(int count, BehaviourWheelQuizMode mode, BehaviourWheelDifficulty difficulty, bool filterByDifficulty)
        {
            if (mode == BehaviourWheelQuizMode.Maths)
                return GenerateMathQuestions(count);

            if (populateDefaultsIfEmpty)
                PopulateMissingDefaults();

            List<BehaviourWheelQuestionData> source = mode == BehaviourWheelQuizMode.Behaviour
                ? behaviourQuestions
                : generalQuestions;

            List<BehaviourWheelQuestionData> validQuestions = new List<BehaviourWheelQuestionData>();
            for (int i = 0; i < source.Count; i++)
            {
                BehaviourWheelQuestionData question = source[i];
                if (question == null)
                    continue;

                if (filterByDifficulty && question.difficulty != difficulty)
                    continue;

                if (question.HasValidOptions(MinWheelOptions, MaxWheelOptions))
                {
                    BehaviourWheelQuestionData clone = CloneWithLimitedOptions(question, MaxWheelOptions);

                    if (mode == BehaviourWheelQuizMode.Behaviour)
                        ApplyBehaviourSharedIcons(clone);

                    validQuestions.Add(clone);
                }
            }

            Shuffle(validQuestions);

            int finalCount = Mathf.Clamp(count, 0, validQuestions.Count);
            return validQuestions.GetRange(0, finalCount);
        }

        public void PopulateDefaultQuestions()
        {
            behaviourQuestions.Clear();
            generalQuestions.Clear();
            AddBehaviourQuestions();
            AddGeneralPlantScienceQuestions();
        }

        private void PopulateMissingDefaults()
        {
            if (behaviourQuestions == null)
                behaviourQuestions = new List<BehaviourWheelQuestionData>();

            if (generalQuestions == null)
                generalQuestions = new List<BehaviourWheelQuestionData>();

            if (behaviourSharedIcons == null)
                behaviourSharedIcons = new List<BehaviourWheelSharedIcon>();

            if (behaviourQuestions.Count == 0)
                AddBehaviourQuestions();

            if (generalQuestions.Count == 0)
                AddGeneralPlantScienceQuestions();
        }

        [ContextMenu("Fill Behaviour Shared Icon Names")]
        private void FillBehaviourSharedIconNames()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Fill Behaviour Shared Icon Names");
#endif

            List<BehaviourWheelSharedIcon> rebuilt = new List<BehaviourWheelSharedIcon>();

            for (int i = 0; i < BehaviourOptions.Length; i++)
            {
                string answerText = BehaviourOptions[i];
                rebuilt.Add(new BehaviourWheelSharedIcon
                {
                    answerText = answerText,
                    icon = GetBehaviourSharedIcon(answerText)
                });
            }

            behaviourSharedIcons = rebuilt;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        private Sprite GetBehaviourSharedIcon(string answerText)
        {
            if (behaviourSharedIcons == null || string.IsNullOrWhiteSpace(answerText))
                return null;

            string target = answerText.Trim();
            for (int i = 0; i < behaviourSharedIcons.Count; i++)
            {
                BehaviourWheelSharedIcon entry = behaviourSharedIcons[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.answerText))
                    continue;

                if (string.Equals(entry.answerText.Trim(), target, StringComparison.OrdinalIgnoreCase))
                    return entry.icon;
            }

            return null;
        }

        private void ApplyBehaviourSharedIcons(BehaviourWheelQuestionData question)
        {
            if (question == null || question.options == null)
                return;

            for (int i = 0; i < question.options.Count; i++)
            {
                BehaviourWheelOptionData option = question.options[i];
                if (option == null)
                    continue;

                Sprite sharedIcon = GetBehaviourSharedIcon(option.answerText);
                if (sharedIcon != null)
                    option.icon = sharedIcon;
            }
        }

        private void AddBehaviourQuestions()
        {
            AddBehaviour("The King ignores the elderly people. What behaviour is this?", "Ignorant", "Ignoring people who need care is ignorant behaviour.");
            AddBehaviour("The King only thinks about his own wishes. What behaviour is this?", "Selfish", "Thinking only about ourselves is selfish.");
            AddBehaviour("Dadu guides Dev with care. What behaviour is this?", "Caring", "Guiding someone gently shows caring behaviour.");
            AddBehaviour("Dadu tries to keep Dev safe. What behaviour is this?", "Protective", "Keeping someone safe is protective behaviour.");
            AddBehaviour("Dev listens to Dadu carefully. What behaviour is this?", "Respectful", "Listening carefully to elders is respectful.");
            AddBehaviour("The Princess speaks kindly to others. What behaviour is this?", "Kind", "Speaking gently is kind behaviour.");
            AddBehaviour("The King does not care about the feelings of others. What behaviour is this?", "Selfish", "Not caring about others can be selfish.");
            AddBehaviour("Dev helps an elderly person. What behaviour is this?", "Kind", "Helping others is kind behaviour.");
            AddBehaviour("Dadu worries about Dev's safety. What behaviour is this?", "Protective", "Worrying about safety shows protective behaviour.");
            AddBehaviour("The Princess shows concern for people around her. What behaviour is this?", "Caring", "Concern for others is caring behaviour.");
            AddBehaviour("Dev talks politely to Dadu. What behaviour is this?", "Respectful", "Polite words show respect.");
            AddBehaviour("The King refuses to listen to good advice. What behaviour is this?", "Ignorant", "Refusing good advice can be ignorant.");
            AddBehaviour("The Minister follows only what pleases the King. What behaviour is this?", "Selfish", "Choosing only pleasing actions without thinking of others is selfish.");
            AddBehaviour("The Saint gives guidance to others. What behaviour is this?", "Caring", "Helping others understand is caring.");
            AddBehaviour("Dev thinks about what is right and wrong. What behaviour is this?", "Respectful", "Thinking carefully about right and wrong shows respect.");
            AddBehaviour("The Princess cares about the problems of others. What behaviour is this?", "Caring", "Caring people notice other people's problems.");
            AddBehaviour("The King treats elderly people as unimportant. What behaviour is this?", "Ignorant", "Treating people as unimportant is ignorant.");
            AddBehaviour("Dadu supports Dev during confusion. What behaviour is this?", "Kind", "Support during confusion is kind.");
            AddBehaviour("Dev respects elderly people. What behaviour is this?", "Respectful", "Respecting elders is respectful behaviour.");
            AddBehaviour("Dadu warns Dev about possible danger. What behaviour is this?", "Protective", "Warning someone about danger is protective.");
            AddBehaviour("The Princess tries to understand others. What behaviour is this?", "Caring", "Trying to understand others is caring.");
            AddBehaviour("The King wants things only his own way. What behaviour is this?", "Selfish", "Wanting only our own way is selfish.");
            AddBehaviour("The Minister ignores the suffering of elderly people. What behaviour is this?", "Ignorant", "Ignoring suffering is ignorant behaviour.");
            AddBehaviour("Dev behaves gently with Dadu. What behaviour is this?", "Kind", "Gentle behaviour is kind.");
            AddBehaviour("Dadu speaks wisely to guide Dev. What behaviour is this?", "Caring", "Wise guidance can show care.");
            AddBehaviour("The Saint helps people understand what is right. What behaviour is this?", "Kind", "Helping people learn what is right is kind.");
            AddBehaviour("Dev does not make fun of elderly people. What behaviour is this?", "Respectful", "Not making fun of others shows respect.");
            AddBehaviour("Dadu stands by Dev when Dev needs help. What behaviour is this?", "Protective", "Standing by someone in need can be protective.");
            AddBehaviour("The King ignores advice because he thinks he knows best. What behaviour is this?", "Ignorant", "Ignoring good advice is ignorant.");
            AddBehaviour("The Princess shows gentle behaviour toward people. What behaviour is this?", "Kind", "Gentle behaviour is kind.");
        }

        private void AddGeneralPlantScienceQuestions()
        {
            AddGeneral("Which part of a plant takes in water from the soil?", "Root", "Roots take in water and minerals from the soil.", "Root", "Stem", "Leaf", "Flower");
            AddGeneral("Which part of a plant carries water to different parts?", "Stem", "The stem carries water and supports the plant.", "Stem", "Root", "Fruit", "Seed");
            AddGeneral("Which part of a plant usually makes food?", "Leaf", "Leaves make food for the plant using sunlight.", "Leaf", "Root", "Flower", "Fruit");
            AddGeneral("Which part of a plant grows into a new plant?", "Seed", "A seed can grow into a new plant.", "Seed", "Flower", "Stem", "Leaf");
            AddGeneral("Which part of a plant often becomes a fruit?", "Flower", "Many flowers later form fruits.", "Flower", "Root", "Stem", "Leaf");
            AddGeneral("Which part protects the seeds in many plants?", "Fruit", "Fruit protects seeds in many plants.", "Fruit", "Root", "Stem", "Leaf");
            AddGeneral("What does a green plant need to make food?", "Sunlight", "Plants need sunlight to make food.", "Sunlight", "Stone", "Plastic", "Metal");
            AddGeneral("What gas do plants use to make food?", "Carbon dioxide", "Plants use carbon dioxide from the air.", "Carbon dioxide", "Oxygen", "Smoke", "Dust");
            AddGeneral("What do plants release into the air during photosynthesis?", "Oxygen", "Plants release oxygen during photosynthesis.", "Oxygen", "Carbon dioxide", "Sand", "Steam");
            AddGeneral("Which plant part is usually under the ground?", "Root", "Roots usually grow under the ground.", "Root", "Flower", "Fruit", "Leaf");
            AddGeneral("Which plant part holds the plant upright?", "Stem", "The stem supports the plant and keeps it upright.", "Stem", "Seed", "Fruit", "Petal");
            AddGeneral("Which part of a flower is often colourful?", "Petal", "Petals are often colourful and attractive.", "Petal", "Root", "Stem", "Seed");
            AddGeneral("Which part of a plant absorbs minerals?", "Root", "Roots absorb minerals from the soil.", "Root", "Leaf", "Flower", "Fruit");
            AddGeneral("What is the flat green part of a plant called?", "Leaf", "The flat green part is called a leaf.", "Leaf", "Root", "Fruit", "Seed");
            AddGeneral("Which part of a plant can be eaten in carrot?", "Root", "Carrot is a root vegetable.", "Root", "Flower", "Stem", "Fruit");
            AddGeneral("Which part of a plant can be eaten in spinach?", "Leaf", "Spinach leaves are eaten as food.", "Leaf", "Root", "Fruit", "Seed");
            AddGeneral("Which part of a plant can be eaten in sugarcane?", "Stem", "Sugarcane is a stem that stores sugar.", "Stem", "Root", "Flower", "Seed");
            AddGeneral("Which part of a plant can be eaten in apple?", "Fruit", "Apple is a fruit.", "Fruit", "Root", "Stem", "Leaf");
            AddGeneral("Which part helps a plant stay fixed in the soil?", "Root", "Roots hold the plant firmly in the soil.", "Root", "Flower", "Fruit", "Petal");
            AddGeneral("What do seeds need to start growing?", "Water", "Seeds need water to start germination.", "Water", "Paint", "Glass", "Plastic");
            AddGeneral("What is the baby plant inside a seed called?", "Embryo", "The embryo is the baby plant inside a seed.", "Embryo", "Petal", "Bark", "Fruit");
            AddGeneral("Which part joins leaves and flowers to the root?", "Stem", "The stem connects roots, leaves, and flowers.", "Stem", "Seed", "Fruit", "Petal");
            AddGeneral("Which part of a plant is important for reproduction?", "Flower", "Flowers help many plants reproduce.", "Flower", "Root", "Stem", "Leaf");
            AddGeneral("Which part of a tree is the hard outer covering?", "Bark", "Bark protects the tree trunk.", "Bark", "Seed", "Leaf", "Flower");
            AddGeneral("Which plant part can spread to make new plants?", "Seed", "Seeds can spread and grow into new plants.", "Seed", "Bark", "Leaf", "Stem");
        }

        private void AddBehaviour(string questionText, string correctAnswer, string explanation)
        {
            BehaviourWheelQuestionData question = new BehaviourWheelQuestionData
            {
                questionText = questionText,
                correctAnswer = correctAnswer,
                explanation = explanation,
                difficulty = BehaviourWheelDifficulty.Easy,
                options = new List<BehaviourWheelOptionData>()
            };

            for (int i = 0; i < BehaviourOptions.Length; i++)
            {
                string optionText = BehaviourOptions[i];
                question.options.Add(new BehaviourWheelOptionData(optionText, GetBehaviourSharedIcon(optionText)));
            }

            behaviourQuestions.Add(question);
        }

        private void AddGeneral(string questionText, string correctAnswer, string explanation, params string[] optionTexts)
        {
            BehaviourWheelQuestionData question = new BehaviourWheelQuestionData
            {
                questionText = questionText,
                correctAnswer = correctAnswer,
                explanation = explanation,
                difficulty = BehaviourWheelDifficulty.Easy,
                options = new List<BehaviourWheelOptionData>()
            };

            if (optionTexts != null)
            {
                for (int i = 0; i < optionTexts.Length && i < MaxWheelOptions; i++)
                    question.options.Add(new BehaviourWheelOptionData(optionTexts[i]));
            }

            generalQuestions.Add(question);
        }

        private List<BehaviourWheelQuestionData> GenerateMathQuestions(int count)
        {
            List<BehaviourWheelQuestionData> result = new List<BehaviourWheelQuestionData>();
            HashSet<string> usedQuestions = new HashSet<string>();
            int safety = 0;

            while (result.Count < count && safety < count * 40)
            {
                safety++;
                BehaviourWheelQuestionData question = CreateMathQuestion();
                if (question == null || usedQuestions.Contains(question.questionText))
                    continue;

                usedQuestions.Add(question.questionText);
                result.Add(question);
            }

            return result;
        }

        private BehaviourWheelQuestionData CreateMathQuestion()
        {
            BehaviourWheelMathOperator op = PickMathOperator();
            int a = 0;
            int b = 0;
            int answer = 0;
            string symbol = "+";

            switch (op)
            {
                case BehaviourWheelMathOperator.Addition:
                    PickAdditionSubtractionNumbers(out a, out b);
                    answer = a + b;
                    symbol = "+";
                    break;

                case BehaviourWheelMathOperator.Subtraction:
                    PickAdditionSubtractionNumbers(out a, out b);
                    if (mathSettings.keepSubtractionPositive && b > a)
                    {
                        int temp = a;
                        a = b;
                        b = temp;
                    }
                    answer = a - b;
                    symbol = "-";
                    break;

                case BehaviourWheelMathOperator.Multiplication:
                    a = PickNumberByDigitRange(
                        mathSettings.multiplicationLeftMinDigits,
                        mathSettings.multiplicationLeftMaxDigits);

                    b = PickNumberByDigitRange(
                        mathSettings.multiplicationRightMinDigits,
                        mathSettings.multiplicationRightMaxDigits);

                    answer = a * b;
                    symbol = "\u00D7";
                    break;

                case BehaviourWheelMathOperator.Division:
                    symbol = "\u00F7";
                    if (mathSettings.divisionAnswersWholeNumber)
                    {
                        if (!TryCreateWholeNumberDivision(out a, out b, out answer))
                        {
                            b = Mathf.Max(1, PickNumberByDigitRange(
                                mathSettings.divisionDivisorMinDigits,
                                mathSettings.divisionDivisorMaxDigits));
                            answer = Mathf.Max(1, PickNumberByDigitRange(1, 1));
                            a = answer * b;
                        }
                    }
                    else
                    {
                        a = PickNumberByDigitRange(
                            mathSettings.divisionDividendMinDigits,
                            mathSettings.divisionDividendMaxDigits);

                        b = Mathf.Max(1, PickNumberByDigitRange(
                            mathSettings.divisionDivisorMinDigits,
                            mathSettings.divisionDivisorMaxDigits));

                        answer = Mathf.RoundToInt((float)a / b);
                    }
                    break;
            }

            int optionCount = Mathf.Clamp(mathSettings.optionCount, MinWheelOptions, MaxWheelOptions);
            List<BehaviourWheelOptionData> options = BuildMathOptions(answer, optionCount);

            return new BehaviourWheelQuestionData
            {
                questionText = $"What is {a} {symbol} {b}?",
                correctAnswer = answer.ToString(),
                explanation = $"{a} {symbol} {b} = {answer}",
                difficulty = BehaviourWheelDifficulty.Easy,
                options = options
            };
        }

        private void PickAdditionSubtractionNumbers(out int a, out int b)
        {
            int min = Mathf.Max(1, mathSettings.minNumber, GetMinByDigits(mathSettings.minDigits));
            int digitMax = GetMaxByDigits(mathSettings.maxDigits);
            int max = Mathf.Min(Mathf.Max(mathSettings.maxNumber, min + 1), digitMax);

            if (max <= min)
                max = min + 1;

            a = UnityEngine.Random.Range(min, max + 1);
            b = UnityEngine.Random.Range(min, max + 1);
        }

        private int PickNumberByDigitRange(int minDigits, int maxDigits)
        {
            minDigits = Mathf.Clamp(minDigits, 1, 4);
            maxDigits = Mathf.Clamp(maxDigits, 1, 4);

            if (maxDigits < minDigits)
                maxDigits = minDigits;

            int min = GetMinByDigits(minDigits);
            int max = GetMaxByDigits(maxDigits);

            if (max <= min)
                max = min + 1;

            return UnityEngine.Random.Range(min, max + 1);
        }

        private bool TryCreateWholeNumberDivision(out int dividend, out int divisor, out int answer)
        {
            int dividendMin = GetMinByDigits(mathSettings.divisionDividendMinDigits);
            int dividendMax = GetMaxByDigits(mathSettings.divisionDividendMaxDigits);
            int divisorMin = Mathf.Max(1, GetMinByDigits(mathSettings.divisionDivisorMinDigits));
            int divisorMax = Mathf.Max(divisorMin, GetMaxByDigits(mathSettings.divisionDivisorMaxDigits));

            for (int attempt = 0; attempt < 200; attempt++)
            {
                divisor = UnityEngine.Random.Range(divisorMin, divisorMax + 1);
                if (divisor <= 0)
                    continue;

                int minAnswer = Mathf.Max(1, Mathf.CeilToInt((float)dividendMin / divisor));
                int maxAnswer = Mathf.Max(1, dividendMax / divisor);

                if (maxAnswer < minAnswer)
                    continue;

                answer = UnityEngine.Random.Range(minAnswer, maxAnswer + 1);
                dividend = answer * divisor;

                if (dividend >= dividendMin && dividend <= dividendMax)
                    return true;
            }

            for (int tryDivisor = divisorMin; tryDivisor <= divisorMax; tryDivisor++)
            {
                if (tryDivisor <= 0)
                    continue;

                int minAnswer = Mathf.Max(1, Mathf.CeilToInt((float)dividendMin / tryDivisor));
                int maxAnswer = Mathf.Max(1, dividendMax / tryDivisor);

                if (maxAnswer < minAnswer)
                    continue;

                answer = minAnswer;
                divisor = tryDivisor;
                dividend = answer * divisor;
                return true;
            }

            dividend = 12;
            divisor = 3;
            answer = 4;
            return false;
        }

        private BehaviourWheelMathOperator PickMathOperator()
        {
            List<BehaviourWheelMathOperator> allowed = new List<BehaviourWheelMathOperator>();
            if (mathSettings.addition) allowed.Add(BehaviourWheelMathOperator.Addition);
            if (mathSettings.subtraction) allowed.Add(BehaviourWheelMathOperator.Subtraction);
            if (mathSettings.multiplication) allowed.Add(BehaviourWheelMathOperator.Multiplication);
            if (mathSettings.division) allowed.Add(BehaviourWheelMathOperator.Division);

            if (allowed.Count == 0)
                allowed.Add(BehaviourWheelMathOperator.Addition);

            return allowed[UnityEngine.Random.Range(0, allowed.Count)];
        }

        private List<BehaviourWheelOptionData> BuildMathOptions(int correctAnswer, int optionCount)
        {
            HashSet<int> values = new HashSet<int> { correctAnswer };
            int spread = Mathf.Max(3, mathSettings.wrongAnswerSpread);
            int safety = 0;

            while (values.Count < optionCount && safety < 120)
            {
                safety++;
                int offset = UnityEngine.Random.Range(-spread, spread + 1);
                if (offset == 0)
                    continue;

                int candidate = correctAnswer + offset;
                if (candidate < 0)
                    candidate = Mathf.Abs(candidate) + UnityEngine.Random.Range(1, 4);

                values.Add(candidate);
            }

            List<BehaviourWheelOptionData> options = new List<BehaviourWheelOptionData>();
            foreach (int value in values)
                options.Add(new BehaviourWheelOptionData(value.ToString()));

            Shuffle(options);
            return options;
        }

        private BehaviourWheelQuestionData CloneWithLimitedOptions(BehaviourWheelQuestionData source, int maxOptions)
        {
            BehaviourWheelQuestionData clone = new BehaviourWheelQuestionData
            {
                questionText = source.questionText,
                correctAnswer = source.correctAnswer,
                explanation = source.explanation,
                difficulty = source.difficulty,
                options = new List<BehaviourWheelOptionData>()
            };

            for (int i = 0; i < source.options.Count && clone.options.Count < maxOptions; i++)
            {
                BehaviourWheelOptionData option = source.options[i];
                if (option != null && !string.IsNullOrWhiteSpace(option.answerText))
                    clone.options.Add(new BehaviourWheelOptionData(option.answerText, option.icon));
            }

            return clone;
        }

        private static int GetMinByDigits(int digits)
        {
            digits = Mathf.Clamp(digits, 1, 4);
            if (digits <= 1)
                return 1;

            int value = 1;
            for (int i = 1; i < digits; i++)
                value *= 10;

            return value;
        }

        private static int GetMaxByDigits(int digits)
        {
            digits = Mathf.Clamp(digits, 1, 4);
            int value = 1;
            for (int i = 0; i < digits; i++)
                value *= 10;

            return value - 1;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
