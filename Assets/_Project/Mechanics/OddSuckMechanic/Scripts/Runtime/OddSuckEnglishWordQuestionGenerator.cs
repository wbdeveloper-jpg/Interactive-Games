using System;
using System.Collections.Generic;
using UnityEngine;

namespace OddSuckMechanic
{
    public enum OddSuckEnglishChallengeMode
    {
        SynonymOnly,
        AntonymOnly,
        MixedRandom
    }

    [Serializable]
    public class OddSuckEnglishWordEntry
    {
        public string word;
        public string synonym;
        public string antonym;

        public OddSuckEnglishWordEntry(string word, string synonym, string antonym)
        {
            this.word = word;
            this.synonym = synonym;
            this.antonym = antonym;
        }
    }

    public class OddSuckEnglishWordQuestionGenerator : OddSuckQuestionGeneratorBase
    {
        [Header("English Rules")]
        [SerializeField] private OddSuckEnglishChallengeMode challengeMode = OddSuckEnglishChallengeMode.MixedRandom;
        [SerializeField] private bool titleCaseOptions = false;
        [SerializeField] private bool avoidSameQuestionBackToBack = true;
        [SerializeField] private string synonymQuestionFormat = "Find the synonym of \"{0}\"";
        [SerializeField] private string antonymQuestionFormat = "Find the antonym of \"{0}\"";

        [Header("Word Bank")]
        [Tooltip("Class 3 to 5 friendly words. You can edit/add/remove entries from Inspector.")]
        [SerializeField] private List<OddSuckEnglishWordEntry> wordBank = new List<OddSuckEnglishWordEntry>
        {
            new OddSuckEnglishWordEntry("happy", "glad", "sad"),
            new OddSuckEnglishWordEntry("big", "large", "small"),
            new OddSuckEnglishWordEntry("fast", "quick", "slow"),
            new OddSuckEnglishWordEntry("hot", "warm", "cold"),
            new OddSuckEnglishWordEntry("bright", "shiny", "dark"),
            new OddSuckEnglishWordEntry("easy", "simple", "hard"),
            new OddSuckEnglishWordEntry("kind", "nice", "mean"),
            new OddSuckEnglishWordEntry("clean", "neat", "dirty"),
            new OddSuckEnglishWordEntry("loud", "noisy", "quiet"),
            new OddSuckEnglishWordEntry("brave", "bold", "afraid"),
            new OddSuckEnglishWordEntry("smart", "clever", "silly"),
            new OddSuckEnglishWordEntry("near", "close", "far"),
            new OddSuckEnglishWordEntry("begin", "start", "finish"),
            new OddSuckEnglishWordEntry("finish", "end", "start"),
            new OddSuckEnglishWordEntry("laugh", "giggle", "cry"),
            new OddSuckEnglishWordEntry("tiny", "small", "huge"),
            new OddSuckEnglishWordEntry("pretty", "beautiful", "ugly"),
            new OddSuckEnglishWordEntry("angry", "mad", "calm"),
            new OddSuckEnglishWordEntry("rich", "wealthy", "poor"),
            new OddSuckEnglishWordEntry("empty", "blank", "full"),
            new OddSuckEnglishWordEntry("full", "filled", "empty"),
            new OddSuckEnglishWordEntry("young", "new", "old"),
            new OddSuckEnglishWordEntry("old", "aged", "young"),
            new OddSuckEnglishWordEntry("right", "correct", "wrong"),
            new OddSuckEnglishWordEntry("wrong", "incorrect", "right"),
            new OddSuckEnglishWordEntry("above", "over", "below"),
            new OddSuckEnglishWordEntry("below", "under", "above"),
            new OddSuckEnglishWordEntry("open", "unlock", "close"),
            new OddSuckEnglishWordEntry("close", "shut", "open"),
            new OddSuckEnglishWordEntry("true", "real", "false"),
            new OddSuckEnglishWordEntry("false", "untrue", "true"),
            new OddSuckEnglishWordEntry("safe", "secure", "dangerous"),
            new OddSuckEnglishWordEntry("dangerous", "unsafe", "safe"),
            new OddSuckEnglishWordEntry("strong", "powerful", "weak"),
            new OddSuckEnglishWordEntry("weak", "feeble", "strong"),
            new OddSuckEnglishWordEntry("fresh", "new", "stale"),
            new OddSuckEnglishWordEntry("stale", "old", "fresh"),
            new OddSuckEnglishWordEntry("wet", "damp", "dry"),
            new OddSuckEnglishWordEntry("dry", "arid", "wet"),
            new OddSuckEnglishWordEntry("soft", "gentle", "hard"),
            new OddSuckEnglishWordEntry("hard", "solid", "soft"),
            new OddSuckEnglishWordEntry("light", "pale", "heavy"),
            new OddSuckEnglishWordEntry("heavy", "weighty", "light"),
            new OddSuckEnglishWordEntry("early", "soon", "late"),
            new OddSuckEnglishWordEntry("late", "delayed", "early"),
            new OddSuckEnglishWordEntry("day", "daytime", "night"),
            new OddSuckEnglishWordEntry("night", "darkness", "day"),
            new OddSuckEnglishWordEntry("love", "like", "hate"),
            new OddSuckEnglishWordEntry("hate", "dislike", "love"),
            new OddSuckEnglishWordEntry("friend", "pal", "enemy"),
            new OddSuckEnglishWordEntry("enemy", "foe", "friend"),
            new OddSuckEnglishWordEntry("buy", "purchase", "sell"),
            new OddSuckEnglishWordEntry("sell", "trade", "buy"),
            new OddSuckEnglishWordEntry("give", "offer", "take"),
            new OddSuckEnglishWordEntry("take", "grab", "give"),
            new OddSuckEnglishWordEntry("push", "shove", "pull"),
            new OddSuckEnglishWordEntry("pull", "drag", "push"),
            new OddSuckEnglishWordEntry("win", "succeed", "lose"),
            new OddSuckEnglishWordEntry("lose", "fail", "win"),
            new OddSuckEnglishWordEntry("enter", "come in", "exit"),
            new OddSuckEnglishWordEntry("exit", "leave", "enter"),
            new OddSuckEnglishWordEntry("build", "make", "break"),
            new OddSuckEnglishWordEntry("break", "crack", "fix"),
            new OddSuckEnglishWordEntry("fix", "repair", "break"),
            new OddSuckEnglishWordEntry("remember", "recall", "forget"),
            new OddSuckEnglishWordEntry("forget", "miss", "remember"),
            new OddSuckEnglishWordEntry("find", "discover", "lose"),
            new OddSuckEnglishWordEntry("hide", "cover", "show"),
            new OddSuckEnglishWordEntry("show", "display", "hide"),
            new OddSuckEnglishWordEntry("question", "query", "answer"),
            new OddSuckEnglishWordEntry("answer", "reply", "question"),
            new OddSuckEnglishWordEntry("many", "several", "few"),
            new OddSuckEnglishWordEntry("few", "some", "many"),
            new OddSuckEnglishWordEntry("always", "forever", "never"),
            new OddSuckEnglishWordEntry("never", "not ever", "always"),
            new OddSuckEnglishWordEntry("inside", "within", "outside"),
            new OddSuckEnglishWordEntry("outside", "outdoors", "inside"),
            new OddSuckEnglishWordEntry("front", "ahead", "back"),
            new OddSuckEnglishWordEntry("back", "rear", "front"),
            new OddSuckEnglishWordEntry("top", "upper", "bottom"),
            new OddSuckEnglishWordEntry("bottom", "lower", "top"),
            new OddSuckEnglishWordEntry("straight", "direct", "crooked"),
            new OddSuckEnglishWordEntry("crooked", "bent", "straight"),
            new OddSuckEnglishWordEntry("smooth", "even", "rough"),
            new OddSuckEnglishWordEntry("rough", "bumpy", "smooth"),
            new OddSuckEnglishWordEntry("polite", "respectful", "rude"),
            new OddSuckEnglishWordEntry("rude", "impolite", "polite"),
            new OddSuckEnglishWordEntry("honest", "truthful", "dishonest"),
            new OddSuckEnglishWordEntry("dishonest", "untruthful", "honest"),
            new OddSuckEnglishWordEntry("quickly", "fast", "slowly"),
            new OddSuckEnglishWordEntry("slowly", "gently", "quickly"),
            new OddSuckEnglishWordEntry("quiet", "silent", "loud"),
            new OddSuckEnglishWordEntry("huge", "giant", "tiny"),
            new OddSuckEnglishWordEntry("calm", "peaceful", "angry"),
            new OddSuckEnglishWordEntry("deep", "low", "shallow"),
            new OddSuckEnglishWordEntry("shallow", "not deep", "deep"),
            new OddSuckEnglishWordEntry("better", "improved", "worse"),
            new OddSuckEnglishWordEntry("worse", "poorer", "better"),
            new OddSuckEnglishWordEntry("correct", "right", "wrong"),
            new OddSuckEnglishWordEntry("careful", "cautious", "careless")
        };

        private int lastWordIndex = -1;

        public override bool CanGenerate()
        {
            return GetUsableEntryCount() >= 4;
        }

        public override OddSuckGeneratedQuestion Generate(int waveIndex)
        {
            OddSuckEnglishChallengeMode resolvedMode = ResolveMode();
            int wordIndex = PickQuestionWordIndex();
            OddSuckEnglishWordEntry entry = wordBank[wordIndex];
            string correctAnswer = resolvedMode == OddSuckEnglishChallengeMode.AntonymOnly ? entry.antonym : entry.synonym;
            int itemCount = GetRandomItemCount();

            OddSuckGeneratedQuestion question = new OddSuckGeneratedQuestion
            {
                displayMode = OddSuckItemDisplayMode.Text,
                questionText = string.Format(
                    resolvedMode == OddSuckEnglishChallengeMode.AntonymOnly ? antonymQuestionFormat : synonymQuestionFormat,
                    entry.word)
            };

            question.items.Add(new OddSuckItemData
            {
                displayText = FormatOption(correctAnswer),
                icon = null,
                isOdd = true
            });

            List<string> fillers = BuildFillerPool(resolvedMode, entry, correctAnswer);
            Shuffle(fillers);

            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                correctAnswer
            };

            for (int i = 0; i < fillers.Count && question.items.Count < itemCount; i++)
            {
                string filler = fillers[i];
                if (string.IsNullOrWhiteSpace(filler) || used.Contains(filler))
                {
                    continue;
                }

                used.Add(filler);
                question.items.Add(new OddSuckItemData
                {
                    displayText = FormatOption(filler),
                    icon = null,
                    isOdd = false
                });
            }

            while (question.items.Count < itemCount)
            {
                string fallback = GetFallbackFiller(used);
                used.Add(fallback);
                question.items.Add(new OddSuckItemData
                {
                    displayText = FormatOption(fallback),
                    icon = null,
                    isOdd = false
                });
            }

            Shuffle(question.items);
            return question;
        }

        private OddSuckEnglishChallengeMode ResolveMode()
        {
            if (challengeMode == OddSuckEnglishChallengeMode.MixedRandom)
            {
                return UnityEngine.Random.value > 0.5f
                    ? OddSuckEnglishChallengeMode.SynonymOnly
                    : OddSuckEnglishChallengeMode.AntonymOnly;
            }

            return challengeMode;
        }

        private int PickQuestionWordIndex()
        {
            int safeCount = wordBank == null ? 0 : wordBank.Count;
            if (safeCount <= 0)
            {
                return 0;
            }

            for (int attempt = 0; attempt < 30; attempt++)
            {
                int index = UnityEngine.Random.Range(0, safeCount);
                if (!IsEntryUsable(wordBank[index]))
                {
                    continue;
                }

                if (!avoidSameQuestionBackToBack || index != lastWordIndex || safeCount <= 1)
                {
                    lastWordIndex = index;
                    return index;
                }
            }

            for (int i = 0; i < safeCount; i++)
            {
                if (IsEntryUsable(wordBank[i]))
                {
                    lastWordIndex = i;
                    return i;
                }
            }

            return 0;
        }

        private List<string> BuildFillerPool(OddSuckEnglishChallengeMode resolvedMode, OddSuckEnglishWordEntry questionEntry, string correctAnswer)
        {
            List<string> fillers = new List<string>();
            if (wordBank == null)
            {
                return fillers;
            }

            for (int i = 0; i < wordBank.Count; i++)
            {
                OddSuckEnglishWordEntry entry = wordBank[i];
                if (!IsEntryUsable(entry) || entry == questionEntry)
                {
                    continue;
                }

                string sameBranchWord = resolvedMode == OddSuckEnglishChallengeMode.AntonymOnly ? entry.antonym : entry.synonym;
                string otherBranchWord = resolvedMode == OddSuckEnglishChallengeMode.AntonymOnly ? entry.synonym : entry.antonym;

                AddIfValid(fillers, sameBranchWord, correctAnswer);

                // Add a few cross-branch words too. This keeps repeated waves feeling less predictable.
                if (UnityEngine.Random.value > 0.65f)
                {
                    AddIfValid(fillers, otherBranchWord, correctAnswer);
                }
            }

            return fillers;
        }

        private string GetFallbackFiller(HashSet<string> used)
        {
            string[] fallbackWords = { "blue", "jump", "table", "river", "cloud", "music", "pencil", "garden", "window", "planet" };
            for (int i = 0; i < fallbackWords.Length; i++)
            {
                if (!used.Contains(fallbackWords[i]))
                {
                    return fallbackWords[i];
                }
            }

            return "word" + UnityEngine.Random.Range(10, 999).ToString();
        }

        private string FormatOption(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            if (!titleCaseOptions || value.Length == 0)
            {
                return value;
            }

            return char.ToUpper(value[0]) + value.Substring(1);
        }

        private int GetUsableEntryCount()
        {
            if (wordBank == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < wordBank.Count; i++)
            {
                if (IsEntryUsable(wordBank[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsEntryUsable(OddSuckEnglishWordEntry entry)
        {
            return entry != null
                && !string.IsNullOrWhiteSpace(entry.word)
                && !string.IsNullOrWhiteSpace(entry.synonym)
                && !string.IsNullOrWhiteSpace(entry.antonym);
        }

        private static void AddIfValid(List<string> words, string value, string correctAnswer)
        {
            if (words == null || string.IsNullOrWhiteSpace(value) || string.Equals(value, correctAnswer, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            words.Add(value.Trim());
        }
    }
}
