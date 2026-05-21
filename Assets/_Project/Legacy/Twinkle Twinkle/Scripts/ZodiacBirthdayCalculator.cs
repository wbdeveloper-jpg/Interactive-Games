using System;

public static class ZodiacBirthdayCalculator
{
    public static bool IsValidMonthDay(int month, int day)
    {
        if (month < 1 || month > 12) return false;
        int maxDay = DateTime.DaysInMonth(2000, month);
        return day >= 1 && day <= maxDay;
    }

    public static ZodiacSign GetSign(int month, int day)
    {
        if (!IsValidMonthDay(month, day))
        {
            throw new ArgumentOutOfRangeException("Invalid birthday date.");
        }

        if ((month == 12 && day >= 22) || (month == 1 && day <= 19)) return ZodiacSign.Capricorn;
        if ((month == 1 && day >= 20) || (month == 2 && day <= 18)) return ZodiacSign.Aquarius;
        if ((month == 2 && day >= 19) || (month == 3 && day <= 20)) return ZodiacSign.Pisces;
        if ((month == 3 && day >= 21) || (month == 4 && day <= 19)) return ZodiacSign.Aries;
        if ((month == 4 && day >= 20) || (month == 5 && day <= 20)) return ZodiacSign.Taurus;
        if ((month == 5 && day >= 21) || (month == 6 && day <= 20)) return ZodiacSign.Gemini;
        if ((month == 6 && day >= 21) || (month == 7 && day <= 22)) return ZodiacSign.Cancer;
        if ((month == 7 && day >= 23) || (month == 8 && day <= 22)) return ZodiacSign.Leo;
        if ((month == 8 && day >= 23) || (month == 9 && day <= 22)) return ZodiacSign.Virgo;
        if ((month == 9 && day >= 23) || (month == 10 && day <= 22)) return ZodiacSign.Libra;
        if ((month == 10 && day >= 23) || (month == 11 && day <= 21)) return ZodiacSign.Scorpio;
        return ZodiacSign.Sagittarius;
    }
}
