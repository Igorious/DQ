namespace DQ.Core
{
    public sealed class DqSection
    {
        public DqSection(DqPageSize pageSize, DqPageMargin pageMargin, DqFooters footers)
        {
            PageSize = pageSize;
            PageMargin = pageMargin;
            Footers = footers;
        }

        public DqPageSize PageSize { get; }
        public DqPageMargin PageMargin { get; }
        public DqFooters Footers { get; }
    }
}