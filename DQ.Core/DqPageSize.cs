using System;
using JetBrains.Annotations;

namespace DQ.Core 
{
    public sealed class DqPageSize : IEquatable<DqPageSize>
    {
        public DqPageSize(decimal width, decimal height)
        {
            Width = width;
            Height = height;
        }

        public decimal Width { get; }
        public decimal Height { get; }

        public override bool Equals(object obj) => Equals(obj as DqPageSize);

        public bool Equals(DqPageSize other) => 
            other != null 
            && Width == other.Width 
            && Height == other.Height;

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width.GetHashCode() * 397) ^ Height.GetHashCode();
            }
        }

        public static bool operator ==([CanBeNull] DqPageSize left, [CanBeNull] DqPageSize right) => Equals(left, right);

        public static bool operator !=([CanBeNull] DqPageSize left, [CanBeNull] DqPageSize right) => !Equals(left, right);
    }
}