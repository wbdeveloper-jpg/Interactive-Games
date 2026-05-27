public static class SentenceWordSearchUtility
{
    public static string CleanWord(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToUpperInvariant(ch));
        }

        return builder.ToString();
    }

    public static string Reverse(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        char[] chars = value.ToCharArray();
        System.Array.Reverse(chars);
        return new string(chars);
    }

    public static string CompleteSentence(string sentenceWithBlank, string answer)
    {
        if (string.IsNullOrEmpty(sentenceWithBlank))
            return answer;

        int firstUnderscore = sentenceWithBlank.IndexOf('_');
        if (firstUnderscore < 0)
            return sentenceWithBlank + " " + answer;

        int lastUnderscore = firstUnderscore;
        while (lastUnderscore + 1 < sentenceWithBlank.Length && sentenceWithBlank[lastUnderscore + 1] == '_')
            lastUnderscore++;

        string before = sentenceWithBlank.Substring(0, firstUnderscore);
        string after = sentenceWithBlank.Substring(lastUnderscore + 1);
        return before + answer + after;
    }

    public static string FormatTime(float time)
    {
        int totalSeconds = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0f, time));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
