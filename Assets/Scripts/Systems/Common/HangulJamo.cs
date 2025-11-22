using System.Collections.Generic;

public static class HangulJamo
{
    private const int Base = 0xAC00;

    private static readonly char[] initials =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ','ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    private static readonly char[] vowels =
    {
        'ㅏ','ㅐ','ㅑ','ㅒ','ㅓ','ㅔ','ㅕ','ㅖ','ㅗ','ㅘ','ㅙ','ㅚ','ㅛ','ㅜ','ㅝ','ㅞ','ㅟ','ㅠ','ㅡ','ㅢ','ㅣ'
    };

    private static readonly char[] finals =
    {
        '\0','ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ','ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ','ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    public struct JamoTriple
    {
        public char initial, vowel, finalJ;
    }

    public static List<JamoTriple> Decompose(string text)
    {
        List<JamoTriple> list = new List<JamoTriple>();

        foreach (char c in text)
        {
            if (c < Base || c > 0xD7A3) continue;

            int code = c - Base;
            list.Add(new JamoTriple
            {
                initial = initials[code / (21 * 28)],
                vowel = vowels[(code % (21 * 28)) / 28],
                finalJ = finals[code % 28]
            });
        }

        return list;
    }
}
