using JetBrains.Annotations;

namespace DQ.Core.Styling 
{
    public sealed class DqStyleBasis
    {
        public bool? IsDefault { get; set; }
        public bool? IsBold { get; set; }
        public decimal? FontSize { get; set; }
        [CanBeNull] public string FontName { get; set; }
        public int? OutlineLevel { get; set; }
        public int? InlineLevel { get; set; }
        public decimal? Indent { get; set; }
        public decimal? OtherIndent { get; set; }
        public decimal? SpacingBetweenLines { get; set; }
        public DqAligment? Aligment { get; set; }
        [CanBeNull] public DqNumbering Numbering { get; set; }
    }
}