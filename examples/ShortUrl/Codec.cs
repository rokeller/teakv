using System;
using System.Text;

namespace ShortUrl;

internal static class Codec
{
    private static readonly string Alphabet = "abcdefghijklmnopqrstuvwxyz-ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789";
    private static readonly ulong Base = Convert.ToUInt64(Alphabet.Length);

    public static string Encode(ulong l)
    {
        if (l == 0)
        {
            return Alphabet[0].ToString();
        }

        StringBuilder sb = new(capacity: 16);

        while (l > 0)
        {
            sb.Insert(0, Alphabet[(int)(l % Base)]);
            l = l / Base;
        }

        return sb.ToString();
    }

    public static ulong? Decode(string s)
    {
        ulong l = 0;

        foreach (char c in s)
        {
            int val = Alphabet.IndexOf(c);
            if (val < 0)
            {
                return default;
            }

            l = (l * Base) + (ulong)val;
        }

        return l;
    }
}
