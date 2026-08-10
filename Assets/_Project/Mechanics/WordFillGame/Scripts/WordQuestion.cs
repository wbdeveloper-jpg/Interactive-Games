using System.Collections.Generic;
using UnityEngine;

public struct WordFillAnswerMatch
{
    public int StartIndex { get; private set; }
    public string SentenceWord { get; private set; }

    public int EndIndex => StartIndex + SentenceWord.Length;

    public WordFillAnswerMatch(int startIndex, string sentenceWord)
    {
        StartIndex = startIndex;
        SentenceWord = sentenceWord;
    }
}

[System.Serializable]
public class WordQuestion
{
    [Header("Visual")]
    public Sprite questionSprite;

    [Header("Hidden Hint")]
    [TextArea(2, 4)]
    public string clueText;

    [Header("Answer Word(s)")]
    [Tooltip("Enter one or more exact words from Completed Line Text, separated by spaces. Every listed word will be detected and hidden independently.")]
    public string answerWord;

    [Tooltip("Complete sentence containing every Answer Word. The words can appear together or in different parts of the sentence.")]
    public string completedLineText;

    [Header("Narration")]
    public AudioClip completedLineNarration;

    [Header("Scoring")]
    public int points = 10;

    [Header("Letter Options")]
    [Min(0)]
    public int extraLetters = 3;

    public string GetCleanAnswer()
    {
        List<string> answerWords = GetAnswerWords();
        return string.Join(" ", answerWords).ToLowerInvariant();
    }

    public List<string> GetAnswerWords()
    {
        List<string> words = new List<string>();

        if (string.IsNullOrWhiteSpace(answerWord))
            return words;

        string[] splitWords = answerWord.Split(
            new[] { ' ', '\t', '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < splitWords.Length; i++)
            words.Add(splitWords[i].Trim());

        return words;
    }

    public string GetCompletedLine()
    {
        string cleanAnswer = GetCleanAnswer();

        if (!string.IsNullOrWhiteSpace(completedLineText))
            return completedLineText.Trim();

        if (string.IsNullOrEmpty(cleanAnswer))
            return "I am.";

        return "I am " + cleanAnswer + ".";
    }

    public bool TryGetAnswerMatches(
        out string sentence,
        out List<WordFillAnswerMatch> matches)
    {
        sentence = GetCompletedLine();
        matches = new List<WordFillAnswerMatch>();

        List<string> answerWords = GetAnswerWords();

        if (string.IsNullOrEmpty(sentence) || answerWords.Count == 0)
            return false;

        for (int i = 0; i < answerWords.Count; i++)
        {
            string answer = answerWords[i];
            int matchIndex = FindAvailableStandaloneMatch(sentence, answer, matches);

            if (matchIndex < 0)
            {
                matches.Clear();
                return false;
            }

            matches.Add(new WordFillAnswerMatch(
                matchIndex,
                sentence.Substring(matchIndex, answer.Length)));
        }

        matches.Sort((first, second) => first.StartIndex.CompareTo(second.StartIndex));
        return true;
    }

    private static int FindAvailableStandaloneMatch(
        string sentence,
        string answer,
        List<WordFillAnswerMatch> existingMatches)
    {
        if (string.IsNullOrEmpty(answer))
            return -1;

        int searchStartIndex = 0;

        while (searchStartIndex <= sentence.Length - answer.Length)
        {
            int matchIndex = sentence.IndexOf(
                answer,
                searchStartIndex,
                System.StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
                return -1;

            int matchEndIndex = matchIndex + answer.Length;
            bool hasValidStartBoundary =
                matchIndex == 0 || !IsWordCharacter(sentence[matchIndex - 1]);
            bool hasValidEndBoundary =
                matchEndIndex == sentence.Length || !IsWordCharacter(sentence[matchEndIndex]);
            bool overlapsExistingMatch =
                OverlapsExistingMatch(matchIndex, matchEndIndex, existingMatches);

            if (hasValidStartBoundary && hasValidEndBoundary && !overlapsExistingMatch)
                return matchIndex;

            searchStartIndex = matchIndex + 1;
        }

        return -1;
    }

    private static bool OverlapsExistingMatch(
        int matchStartIndex,
        int matchEndIndex,
        List<WordFillAnswerMatch> existingMatches)
    {
        for (int i = 0; i < existingMatches.Count; i++)
        {
            WordFillAnswerMatch existingMatch = existingMatches[i];

            if (matchStartIndex < existingMatch.EndIndex &&
                matchEndIndex > existingMatch.StartIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWordCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }
}
