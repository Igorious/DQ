namespace DQ.Core
{
    public sealed class DqFooters
    {
        public DqFooters(DqFooter @default, DqFooter first, DqFooter even)
        {
            Default = @default;
            First = first;
            Even = even;
        }

        public DqFooter First { get; }

        public DqFooter Default { get; }

        public DqFooter Even { get; }
    }
}