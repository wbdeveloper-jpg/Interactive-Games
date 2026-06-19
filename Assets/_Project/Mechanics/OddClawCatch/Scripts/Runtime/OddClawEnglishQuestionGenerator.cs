using System.Collections.Generic;
using UnityEngine;

public enum OddClawEnglishMode
{
    SynonymOnly,
    AntonymOnly,
    MixedRandom
}

[System.Serializable]
public class OddClawWordEntry
{
    public string word;
    public string synonym;
    public string antonym;

    public OddClawWordEntry()
    {
    }

    public OddClawWordEntry(string word, string synonym, string antonym)
    {
        this.word = word;
        this.synonym = synonym;
        this.antonym = antonym;
    }
}

[CreateAssetMenu(menuName = "Odd Claw Catch/Question Generators/English Word Generator", fileName = "OddClawEnglishQuestionGenerator")]
public class OddClawEnglishQuestionGenerator : OddClawQuestionGeneratorBase
{
    [Header("English Mode")]
    public OddClawEnglishMode mode = OddClawEnglishMode.MixedRandom;

    [Header("Word Bank")]
    [Tooltip("Default v7 bank contains 100 word/synonym/antonym entries. Use Tools > Odd Claw Catch > English Word Bank if you need to refill an existing asset.")]
    public List<OddClawWordEntry> wordBank = CreateDefaultWordBank100();

    public static List<OddClawWordEntry> CreateDefaultWordBank100()
    {
        return new List<OddClawWordEntry>
        {
            new OddClawWordEntry("happy", "glad", "sad"),
            new OddClawWordEntry("big", "large", "small"),
            new OddClawWordEntry("fast", "quick", "slow"),
            new OddClawWordEntry("hot", "warm", "cold"),
            new OddClawWordEntry("easy", "simple", "hard"),
            new OddClawWordEntry("brave", "bold", "afraid"),
            new OddClawWordEntry("clean", "neat", "dirty"),
            new OddClawWordEntry("bright", "shiny", "dark"),
            new OddClawWordEntry("kind", "nice", "mean"),
            new OddClawWordEntry("loud", "noisy", "quiet"),
            new OddClawWordEntry("rich", "wealthy", "poor"),
            new OddClawWordEntry("strong", "powerful", "weak"),
            new OddClawWordEntry("early", "soon", "late"),
            new OddClawWordEntry("empty", "blank", "full"),
            new OddClawWordEntry("near", "close", "far"),
            new OddClawWordEntry("soft", "gentle", "rough"),
            new OddClawWordEntry("young", "youthful", "old"),
            new OddClawWordEntry("smart", "clever", "foolish"),
            new OddClawWordEntry("calm", "peaceful", "angry"),
            new OddClawWordEntry("safe", "secure", "dangerous"),
            new OddClawWordEntry("light", "bright", "heavy"),
            new OddClawWordEntry("dry", "arid", "wet"),
            new OddClawWordEntry("open", "unlocked", "closed"),
            new OddClawWordEntry("begin", "start", "finish"),
            new OddClawWordEntry("above", "over", "below"),
            new OddClawWordEntry("true", "correct", "false"),
            new OddClawWordEntry("love", "like", "hate"),
            new OddClawWordEntry("friend", "pal", "enemy"),
            new OddClawWordEntry("win", "succeed", "lose"),
            new OddClawWordEntry("buy", "purchase", "sell"),
            new OddClawWordEntry("come", "arrive", "go"),
            new OddClawWordEntry("push", "shove", "pull"),
            new OddClawWordEntry("give", "offer", "take"),
            new OddClawWordEntry("laugh", "giggle", "cry"),
            new OddClawWordEntry("build", "make", "break"),
            new OddClawWordEntry("find", "discover", "lose"),
            new OddClawWordEntry("raise", "lift", "lower"),
            new OddClawWordEntry("remember", "recall", "forget"),
            new OddClawWordEntry("shout", "yell", "whisper"),
            new OddClawWordEntry("quick", "swift", "slow"),
            new OddClawWordEntry("tiny", "small", "huge"),
            new OddClawWordEntry("huge", "giant", "tiny"),
            new OddClawWordEntry("beautiful", "pretty", "ugly"),
            new OddClawWordEntry("thin", "slim", "thick"),
            new OddClawWordEntry("wide", "broad", "narrow"),
            new OddClawWordEntry("deep", "low", "shallow"),
            new OddClawWordEntry("high", "tall", "low"),
            new OddClawWordEntry("sweet", "sugary", "bitter"),
            new OddClawWordEntry("fresh", "new", "stale"),
            new OddClawWordEntry("smooth", "even", "bumpy"),
            new OddClawWordEntry("sharp", "pointed", "blunt"),
            new OddClawWordEntry("hard", "tough", "soft"),
            new OddClawWordEntry("healthy", "fit", "sick"),
            new OddClawWordEntry("honest", "truthful", "dishonest"),
            new OddClawWordEntry("polite", "respectful", "rude"),
            new OddClawWordEntry("careful", "cautious", "careless"),
            new OddClawWordEntry("lazy", "idle", "active"),
            new OddClawWordEntry("busy", "occupied", "free"),
            new OddClawWordEntry("funny", "silly", "serious"),
            new OddClawWordEntry("noisy", "loud", "silent"),
            new OddClawWordEntry("quiet", "silent", "noisy"),
            new OddClawWordEntry("clever", "smart", "silly"),
            new OddClawWordEntry("weak", "feeble", "strong"),
            new OddClawWordEntry("slow", "sluggish", "fast"),
            new OddClawWordEntry("cold", "chilly", "hot"),
            new OddClawWordEntry("wrong", "incorrect", "right"),
            new OddClawWordEntry("right", "correct", "wrong"),
            new OddClawWordEntry("best", "greatest", "worst"),
            new OddClawWordEntry("worst", "poorest", "best"),
            new OddClawWordEntry("first", "earliest", "last"),
            new OddClawWordEntry("last", "final", "first"),
            new OddClawWordEntry("inside", "within", "outside"),
            new OddClawWordEntry("outside", "outdoors", "inside"),
            new OddClawWordEntry("front", "ahead", "back"),
            new OddClawWordEntry("back", "rear", "front"),
            new OddClawWordEntry("top", "upper", "bottom"),
            new OddClawWordEntry("bottom", "lower", "top"),
            new OddClawWordEntry("left", "port", "right"),
            new OddClawWordEntry("day", "daytime", "night"),
            new OddClawWordEntry("night", "darkness", "day"),
            new OddClawWordEntry("yes", "sure", "no"),
            new OddClawWordEntry("no", "never", "yes"),
            new OddClawWordEntry("up", "above", "down"),
            new OddClawWordEntry("down", "below", "up"),
            new OddClawWordEntry("add", "join", "remove"),
            new OddClawWordEntry("remove", "delete", "add"),
            new OddClawWordEntry("enter", "come in", "exit"),
            new OddClawWordEntry("exit", "leave", "enter"),
            new OddClawWordEntry("accept", "receive", "reject"),
            new OddClawWordEntry("reject", "refuse", "accept"),
            new OddClawWordEntry("allow", "permit", "forbid"),
            new OddClawWordEntry("forbid", "ban", "allow"),
            new OddClawWordEntry("increase", "grow", "decrease"),
            new OddClawWordEntry("decrease", "reduce", "increase"),
            new OddClawWordEntry("arrive", "come", "depart"),
            new OddClawWordEntry("depart", "leave", "arrive"),
            new OddClawWordEntry("float", "drift", "sink"),
            new OddClawWordEntry("sink", "drop", "float"),
            new OddClawWordEntry("catch", "grab", "release"),
            new OddClawWordEntry("release", "free", "catch")
        };
    }

    [ContextMenu("Replace Word Bank With 100 Defaults")]
    public void ReplaceWordBankWithDefault100()
    {
        wordBank = CreateDefaultWordBank100();
    }

    public override OddClawQuestionData GenerateQuestion(int wave, int requestedOptionCount)
    {
        int optionCount = ClampOptionCount(requestedOptionCount);
        EnsureDefaultWords();

        OddClawEnglishMode selectedMode = mode;
        if (mode == OddClawEnglishMode.MixedRandom)
        {
            selectedMode = UnityEngine.Random.Range(0, 2) == 0
                ? OddClawEnglishMode.SynonymOnly
                : OddClawEnglishMode.AntonymOnly;
        }

        OddClawWordEntry entry = GetValidEntry();
        bool synonymQuestion = selectedMode == OddClawEnglishMode.SynonymOnly;
        string answer = synonymQuestion ? entry.synonym : entry.antonym;
        string question = synonymQuestion
            ? "Catch the SYNONYM of " + entry.word
            : "Catch the ANTONYM of " + entry.word;

        List<OddClawAnswerOption> options = new List<OddClawAnswerOption> { new OddClawAnswerOption(answer) };
        FillDistractors(options, optionCount, answer, entry.word);
        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = question,
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }

    private void EnsureDefaultWords()
    {
        if (wordBank == null || wordBank.Count == 0)
        {
            wordBank = CreateDefaultWordBank100();
            return;
        }

        for (int i = wordBank.Count - 1; i >= 0; i--)
        {
            OddClawWordEntry entry = wordBank[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.word) || string.IsNullOrWhiteSpace(entry.synonym) || string.IsNullOrWhiteSpace(entry.antonym))
                wordBank.RemoveAt(i);
        }

        if (wordBank.Count == 0)
            wordBank = CreateDefaultWordBank100();
    }

    private OddClawWordEntry GetValidEntry()
    {
        EnsureDefaultWords();

        int guard = 0;
        while (guard < 200)
        {
            guard++;
            OddClawWordEntry entry = wordBank[UnityEngine.Random.Range(0, wordBank.Count)];
            if (entry != null && !string.IsNullOrWhiteSpace(entry.word) && !string.IsNullOrWhiteSpace(entry.synonym) && !string.IsNullOrWhiteSpace(entry.antonym))
                return entry;
        }

        return CreateDefaultWordBank100()[0];
    }

    private void FillDistractors(List<OddClawAnswerOption> options, int optionCount, string correct, string questionWord)
    {
        List<string> pool = new List<string>();
        for (int i = 0; i < wordBank.Count; i++)
        {
            OddClawWordEntry item = wordBank[i];
            if (item == null)
                continue;

            AddIfValid(pool, item.word, correct, questionWord);
            AddIfValid(pool, item.synonym, correct, questionWord);
            AddIfValid(pool, item.antonym, correct, questionWord);
        }

        int guard = 0;
        while (options.Count < optionCount && pool.Count > 0 && guard < 600)
        {
            guard++;
            string candidate = pool[UnityEngine.Random.Range(0, pool.Count)];
            if (!ContainsText(options, candidate))
                options.Add(new OddClawAnswerOption(candidate));
        }

        while (options.Count < optionCount)
        {
            options.Add(new OddClawAnswerOption("Option " + options.Count));
        }
    }

    private void AddIfValid(List<string> pool, string value, string correct, string questionWord)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (value == correct || value == questionWord)
            return;

        if (!pool.Contains(value))
            pool.Add(value);
    }
}
