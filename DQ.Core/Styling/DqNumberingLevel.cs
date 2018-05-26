namespace DQ.Core.Styling
{
    public sealed class DqNumberingLevel
    {
        public DqNumberingLevel(string text) => Text = text;

        public string Text { get; }

        public decimal? Indent { get; set; }
    }
}