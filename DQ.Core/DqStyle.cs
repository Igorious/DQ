using System.Diagnostics;
using JetBrains.Annotations;

namespace DQ.Core 
{
    public enum Aligment
    {
        Left,
        Right,
        Center,
        Justify,
    }

    [DebuggerDisplay("[Style] {ID,nq}")]
    public sealed class DqStyle
    {
        public DqStyle(string id, [CanBeNull] string baseStyleID, bool isDefault, bool? isBold, decimal? fontSize,  [CanBeNull] string fontName, int outlineLevel, int? inlineLevel, [CanBeNull] DqNumbering numbering)
        {
            ID = id;
            BaseStyleID = baseStyleID;
            IsDefault = isDefault;
            IsBold = isBold;
            FontSize = fontSize;
            FontName = fontName;
            OutlineLevel = outlineLevel;
            InlineLevel = inlineLevel;
            Numbering = numbering;
        }

        public string ID { get; }

        [CanBeNull]
        public string BaseStyleID { get; }

        [CanBeNull]
        public DqStyle BaseStyle { get; set; }

        public bool IsDefault { get; }
        public bool? IsBold { get; }
        public decimal? FontSize { get; set; }

        [CanBeNull]
        public string FontName { get; set; }

        public int OutlineLevel { get; }
        public int? InlineLevel { get; }

        public decimal? Indent { get; set; }
        public decimal? OtherIndent { get; set; }
        public decimal? SpacingBetweenLines { get; set; }
        public Aligment? Aligment { get; set; }

        [CanBeNull]
        public DqNumbering Numbering { get; }

        public decimal GetFontSize() => FontSize ?? BaseStyle.GetFontSize();
        public string GetFontName() => FontName ?? BaseStyle.GetFontName();
        public bool GetIsBold() => IsBold ?? BaseStyle.GetIsBold();
        public decimal GetIndent() => GetIndentInternal() ?? GetOtherIndent();
        protected decimal? GetIndentInternal() => Indent ?? BaseStyle?.GetIndent();
        public decimal GetOtherIndent() => OtherIndent ?? BaseStyle?.GetOtherIndent() ?? 0;
        public decimal GetSpacingBetweenLines() => SpacingBetweenLines ?? BaseStyle?.GetSpacingBetweenLines() ?? 1;
        public Aligment GetAligment() => Aligment ?? BaseStyle?.GetAligment() ?? Core.Aligment.Left;

        public DqStyle Clone() => (DqStyle) MemberwiseClone();
    }
}