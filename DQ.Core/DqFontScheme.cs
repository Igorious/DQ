using DocumentFormat.OpenXml.Drawing;

namespace DQ.Core
{
    public sealed class DqFontScheme
    {
        private readonly FontScheme _wFontScheme;

        public DqFontScheme(FontScheme wFontScheme) => _wFontScheme = wFontScheme;

        public MajorFont MajorFont => _wFontScheme.MajorFont;
        public MinorFont MinorFont => _wFontScheme.MinorFont;
    }
}