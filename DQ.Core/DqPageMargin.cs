using System;
using JetBrains.Annotations;

namespace DQ.Core 
{
    public sealed class DqPageMargin : IEquatable<DqPageMargin>
    {
        public DqPageMargin(decimal top, decimal right, decimal bottom, decimal left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public decimal Top { get; }
        public decimal Right { get; }
        public decimal Bottom { get; }
        public decimal Left { get; }

        public override bool Equals(object obj) => Equals(obj as DqPageMargin);

        public bool Equals(DqPageMargin other) => 
            other != null 
            && Top == other.Top 
            && Right == other.Right
            && Bottom == other.Bottom 
            && Left == other.Left;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ Left.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==([CanBeNull] DqPageMargin left, [CanBeNull] DqPageMargin right) => Equals(left, right);

        public static bool operator !=([CanBeNull] DqPageMargin left, [CanBeNull] DqPageMargin right) => !Equals(left, right);
    }
}