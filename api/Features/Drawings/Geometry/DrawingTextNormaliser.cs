using System.Text;

namespace Jewel.JPMS.Api.Features.Drawings.Geometry;

/// <summary>
/// Maps ligature glyphs back to their letters. Archicad's PDF export (and Word-era Calibri sheets)
/// emit "ti" and "ft" as single private glyphs whose Unicode mapping comes out as Ɵ and Ō —
/// "posiƟon", "LoŌ" — and the standard fi/fl/ff ligatures as their own code points. None of that
/// is the drawing's fault and none of it should reach a search index, a callout, or a model.
/// The two odd glyphs are only substituted between letters, so a real Ɵ in a name survives.
/// </summary>
public static class DrawingTextNormaliser
{
    public static string Normalise(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var builder = new StringBuilder(text.Length + 8);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            switch (character)
            {
                case 'ﬁ': builder.Append("fi"); continue;
                case 'ﬂ': builder.Append("fl"); continue;
                case 'ﬀ': builder.Append("ff"); continue;
                case 'ﬃ': builder.Append("ffi"); continue;
                case 'ﬄ': builder.Append("ffl"); continue;
                case 'Ɵ' when BetweenLetters(text, index): builder.Append("ti"); continue;
                case 'Ō' when BetweenLetters(text, index): builder.Append("ft"); continue;
                default: builder.Append(character); continue;
            }
        }
        return builder.ToString();
    }

    private static bool BetweenLetters(string text, int index)
    {
        var before = index > 0 && char.IsLetter(text[index - 1]);
        var after = index + 1 < text.Length && char.IsLetter(text[index + 1]);
        return before || after;
    }
}
