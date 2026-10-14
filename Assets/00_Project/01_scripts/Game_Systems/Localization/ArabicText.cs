using System.Collections.Generic;
using System.Text;

/// <summary> Shapes Arabic into joined presentation forms and orders mixed text for TMP. </summary>
public static class ArabicText
{
    // Per base letter: isolated, final, initial, medial presentation form (0 = none).
    private static readonly Dictionary<char, int[]> Forms = new Dictionary<char, int[]>
    {
        { 'ء', new[] { 0xFE80, 0, 0, 0 } },
        { 'آ', new[] { 0xFE81, 0xFE82, 0, 0 } },
        { 'أ', new[] { 0xFE83, 0xFE84, 0, 0 } },
        { 'ؤ', new[] { 0xFE85, 0xFE86, 0, 0 } },
        { 'إ', new[] { 0xFE87, 0xFE88, 0, 0 } },
        { 'ئ', new[] { 0xFE89, 0xFE8A, 0xFE8B, 0xFE8C } },
        { 'ا', new[] { 0xFE8D, 0xFE8E, 0, 0 } },
        { 'ب', new[] { 0xFE8F, 0xFE90, 0xFE91, 0xFE92 } },
        { 'ة', new[] { 0xFE93, 0xFE94, 0, 0 } },
        { 'ت', new[] { 0xFE95, 0xFE96, 0xFE97, 0xFE98 } },
        { 'ث', new[] { 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C } },
        { 'ج', new[] { 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0 } },
        { 'ح', new[] { 0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4 } },
        { 'خ', new[] { 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8 } },
        { 'د', new[] { 0xFEA9, 0xFEAA, 0, 0 } },
        { 'ذ', new[] { 0xFEAB, 0xFEAC, 0, 0 } },
        { 'ر', new[] { 0xFEAD, 0xFEAE, 0, 0 } },
        { 'ز', new[] { 0xFEAF, 0xFEB0, 0, 0 } },
        { 'س', new[] { 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4 } },
        { 'ش', new[] { 0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8 } },
        { 'ص', new[] { 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC } },
        { 'ض', new[] { 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0 } },
        { 'ط', new[] { 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4 } },
        { 'ظ', new[] { 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8 } },
        { 'ع', new[] { 0xFEC9, 0xFECA, 0xFECB, 0xFECC } },
        { 'غ', new[] { 0xFECD, 0xFECE, 0xFECF, 0xFED0 } },
        { 'ف', new[] { 0xFED1, 0xFED2, 0xFED3, 0xFED4 } },
        { 'ق', new[] { 0xFED5, 0xFED6, 0xFED7, 0xFED8 } },
        { 'ك', new[] { 0xFED9, 0xFEDA, 0xFEDB, 0xFEDC } },
        { 'ل', new[] { 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0 } },
        { 'م', new[] { 0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4 } },
        { 'ن', new[] { 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8 } },
        { 'ه', new[] { 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC } },
        { 'و', new[] { 0xFEED, 0xFEEE, 0, 0 } },
        { 'ى', new[] { 0xFEEF, 0xFEF0, 0, 0 } },
        { 'ي', new[] { 0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4 } },
    };

    private static readonly Dictionary<string, string> VisualCache = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> LayoutCache = new Dictionary<string, string>();

    /// <summary> Longest single-line string drawn as plain visual order; longer text uses TMP right-to-left layout. </summary>
    public const int MaxVisualLength = 24;

    /// <summary> True when the text holds unshaped Arabic letters. </summary>
    public static bool NeedsShaping(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char ch in text)
            if (ch >= '؀' && ch <= 'ۿ') return true;
        return false;
    }

    /// <summary> Joined forms in logical order; other characters are untouched. </summary>
    public static string Shape(string logical)
    {
        var sb = new StringBuilder(logical.Length);
        for (int i = 0; i < logical.Length; i++)
        {
            char ch = logical[i];
            if (!Forms.TryGetValue(ch, out int[] form) && ch != 'ـ')
            {
                sb.Append(ch);
                continue;
            }

            bool prevJoins = PreviousJoinsForward(logical, i);

            // Lam followed by alef becomes one ligature glyph.
            if (ch == 'ل' && NextVisible(logical, i, out int alefIndex) && LamAlef(logical[alefIndex], out int isolatedLig))
            {
                sb.Append((char)(prevJoins ? isolatedLig + 1 : isolatedLig));
                for (int m = i + 1; m < alefIndex; m++) sb.Append(logical[m]); // marks between
                i = alefIndex;
                continue;
            }

            bool nextJoins = NextJoinsBackward(logical, i);
            if (ch == 'ـ') { sb.Append(ch); continue; }

            bool dual = form[2] != 0;
            int pick;
            if (dual) pick = prevJoins ? (nextJoins ? form[3] : form[1]) : (nextJoins ? form[2] : form[0]);
            else pick = prevJoins ? form[1] : form[0];
            sb.Append((char)pick);
        }
        return sb.ToString();
    }

    /// <summary> Shaped text reordered for a normal left-to-right label (single line). </summary>
    public static string ToVisual(string logical)
    {
        if (string.IsNullOrEmpty(logical)) return logical;
        if (VisualCache.TryGetValue(logical, out string cached)) return cached;

        List<string> tags = ProtectTags(logical, out string guarded);
        List<Run> runs = Split(Shape(guarded));
        var sb = new StringBuilder(logical.Length);
        for (int r = runs.Count - 1; r >= 0; r--)
        {
            Run run = runs[r];
            if (run.Rtl) AppendReversed(sb, run.Text);
            else sb.Append(run.Text);
        }
        return VisualCache[logical] = RestoreTags(sb.ToString(), tags);
    }

    private const char TagPlaceholderBase = (char)0x0100; // Latin letters, so they stay left-to-right

    /// <summary> Swaps each rich-text tag for one letter so reordering can't mangle it. </summary>
    private static List<string> ProtectTags(string text, out string guarded)
    {
        guarded = text;
        if (text.IndexOf('<') < 0) return null;

        var tags = new List<string>();
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            int close = text[i] == '<' ? text.IndexOf('>', i) : -1;
            if (close < 0 || tags.Count >= 64)
            {
                sb.Append(text[i]);
                continue;
            }

            sb.Append((char)(TagPlaceholderBase + tags.Count));
            tags.Add(text.Substring(i, close - i + 1));
            i = close;
        }

        guarded = sb.ToString();
        return tags;
    }

    /// <summary> Puts the protected tags back in place of their placeholder letters. </summary>
    private static string RestoreTags(string text, List<string> tags)
    {
        if (tags == null) return text;

        var sb = new StringBuilder(text.Length + 40);
        foreach (char ch in text)
        {
            int index = ch - TagPlaceholderBase;
            if (index >= 0 && index < tags.Count) sb.Append(tags[index]);
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary> Shaped text for a label with right-to-left layout: logical order, Latin and number runs pre-flipped. </summary>
    public static string ToLayout(string logical)
    {
        if (string.IsNullOrEmpty(logical)) return logical;
        if (LayoutCache.TryGetValue(logical, out string cached)) return cached;

        List<string> tags = ProtectTags(logical, out string guarded);
        var sb = new StringBuilder(logical.Length);
        foreach (Run run in Split(Shape(guarded)))
        {
            if (run.Rtl) AppendMirrored(sb, run.Text);
            else AppendReversed(sb, run.Text, mirror: false);
        }
        return LayoutCache[logical] = RestoreTags(sb.ToString(), tags);
    }

    private struct Run
    {
        public bool Rtl;
        public string Text;
    }

    /// <summary> Splits into right-to-left and left-to-right runs; neutrals follow the base direction (right-to-left). </summary>
    private static List<Run> Split(string text)
    {
        int n = text.Length;
        var rtl = new bool[n];
        for (int i = 0; i < n; i++) rtl[i] = IsRtlLetter(text[i]) || (!IsLtrLetter(text[i]));

        // Neutrals between two left-to-right characters stay left-to-right (e.g. 10.5, A-B).
        for (int i = 0; i < n; i++)
        {
            if (!IsNeutral(text[i])) continue;
            int a = i - 1, b = i + 1;
            while (a >= 0 && IsNeutral(text[a])) a--;
            while (b < n && IsNeutral(text[b])) b++;
            bool leftIsLtr = a >= 0 && IsLtrLetter(text[a]);
            bool rightIsLtr = b < n && IsLtrLetter(text[b]);
            rtl[i] = !(leftIsLtr && rightIsLtr);
        }

        var runs = new List<Run>();
        int start = 0;
        for (int i = 1; i <= n; i++)
        {
            if (i < n && rtl[i] == rtl[start]) continue;
            runs.Add(new Run { Rtl = rtl[start], Text = text.Substring(start, i - start) });
            start = i;
        }
        return runs;
    }

    private static bool IsRtlLetter(char ch) =>
        (ch >= '؀' && ch <= 'ۿ') || (ch >= 'ﭐ' && ch <= '﷿') || (ch >= 'ﹰ' && ch <= '﻿');

    private static bool IsLtrLetter(char ch) => char.IsLetterOrDigit(ch) && !IsRtlLetter(ch);

    private static bool IsNeutral(char ch) => !IsRtlLetter(ch) && !IsLtrLetter(ch);

    private static void AppendReversed(StringBuilder sb, string text, bool mirror = true)
    {
        for (int i = text.Length - 1; i >= 0; i--) sb.Append(mirror ? Mirror(text[i]) : text[i]);
    }

    private static void AppendMirrored(StringBuilder sb, string text)
    {
        foreach (char ch in text) sb.Append(Mirror(ch));
    }

    private static char Mirror(char ch)
    {
        switch (ch)
        {
            case '(': return ')';
            case ')': return '(';
            case '[': return ']';
            case ']': return '[';
            case '{': return '}';
            case '}': return '{';
            case '<': return '>';
            case '>': return '<';
            case '«': return '»';
            case '»': return '«';
            default: return ch;
        }
    }

    private static bool IsMark(char ch) => (ch >= 'ً' && ch <= 'ٟ') || ch == 'ٰ';

    /// <summary> True when the closest letter before index i joins forward into it. </summary>
    private static bool PreviousJoinsForward(string text, int i)
    {
        for (int p = i - 1; p >= 0; p--)
        {
            char ch = text[p];
            if (IsMark(ch)) continue;
            return ch == 'ـ' || (Forms.TryGetValue(ch, out int[] form) && form[2] != 0);
        }
        return false;
    }

    /// <summary> True when the closest letter after index i accepts a join from it. </summary>
    private static bool NextJoinsBackward(string text, int i)
    {
        if (!NextVisible(text, i, out int n)) return false;
        char ch = text[n];
        if (ch == 'ـ') return true;
        return Forms.TryGetValue(ch, out int[] form) && form[1] != 0 && !(form[0] == 0xFE80);
    }

    private static bool NextVisible(string text, int i, out int index)
    {
        for (index = i + 1; index < text.Length; index++)
            if (!IsMark(text[index])) return true;
        return false;
    }

    /// <summary> Isolated ligature code for lam plus the given alef; the final form is that plus one. </summary>
    private static bool LamAlef(char alef, out int isolated)
    {
        switch (alef)
        {
            case 'آ': isolated = 0xFEF5; return true;
            case 'أ': isolated = 0xFEF7; return true;
            case 'إ': isolated = 0xFEF9; return true;
            case 'ا': isolated = 0xFEFB; return true;
            default: isolated = 0; return false;
        }
    }
}
