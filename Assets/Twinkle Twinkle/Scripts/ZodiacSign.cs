using UnityEngine;

public enum ZodiacSign
{
    Capricorn = 0,
    Aquarius = 1,
    Pisces = 2,
    Aries = 3,
    Taurus = 4,
    Gemini = 5,
    Cancer = 6,
    Leo = 7,
    Virgo = 8,
    Libra = 9,
    Scorpio = 10,
    Sagittarius = 11
}

public static class ZodiacSignExtensions
{
    public static string GetDisplayName(this ZodiacSign sign)
    {
        switch (sign)
        {
            case ZodiacSign.Capricorn: return "Capricorn";
            case ZodiacSign.Aquarius: return "Aquarius";
            case ZodiacSign.Pisces: return "Pisces";
            case ZodiacSign.Aries: return "Aries";
            case ZodiacSign.Taurus: return "Taurus";
            case ZodiacSign.Gemini: return "Gemini";
            case ZodiacSign.Cancer: return "Cancer";
            case ZodiacSign.Leo: return "Leo";
            case ZodiacSign.Virgo: return "Virgo";
            case ZodiacSign.Libra: return "Libra";
            case ZodiacSign.Scorpio: return "Scorpio";
            case ZodiacSign.Sagittarius: return "Sagittarius";
            default: return sign.ToString();
        }
    }

    public static string GetArticle(this ZodiacSign sign)
    {
        string name = sign.GetDisplayName();
        if (string.IsNullOrEmpty(name)) return "a";
        char first = char.ToLowerInvariant(name[0]);
        return "aeiou".IndexOf(first) >= 0 ? "an" : "a";
    }

    public static string GetDefaultDescription(this ZodiacSign sign)
    {
        switch (sign)
        {
            case ZodiacSign.Aries: return "Brave, bold and adventurous!";
            case ZodiacSign.Taurus: return "Calm, patient and strong!";
            case ZodiacSign.Gemini: return "Curious, clever and social!";
            case ZodiacSign.Cancer: return "Caring, protective and kind!";
            case ZodiacSign.Leo: return "Proud, generous and warm!";
            case ZodiacSign.Virgo: return "Practical, helpful and precise!";
            case ZodiacSign.Libra: return "Friendly, fair and charming!";
            case ZodiacSign.Scorpio: return "Passionate, brave and focused!";
            case ZodiacSign.Sagittarius: return "Free, fun and honest!";
            case ZodiacSign.Capricorn: return "Disciplined, smart and steady!";
            case ZodiacSign.Aquarius: return "Original, friendly and independent!";
            case ZodiacSign.Pisces: return "Dreamy, gentle and artistic!";
            default: return "You're special and magical!";
        }
    }
}
