using System.Collections.Generic;

namespace DQ.Core 
{
    public class DqMainPart : DqPart
    {
        public DqMainPart() => Type = PartType.Main;

        public List<DqMainPart> Children { get; } = new List<DqMainPart>();
    }

    internal sealed class HeaderHierarchyService
    {
        public void ParseMainPartHierarchy(IEnumerable<Token> mainPartHeaders, DqDocument document)
        {
            var root = document.Structure.MainPart;

            var stack = new Stack<DqMainPart>();
            stack.Push(root);

            foreach (var header in mainPartHeaders)
            {
                while (stack.Count - 1 > header.Level)
                {
                    stack.Pop();
                }

                var dqSubPart = new DqMainPart { Start = header.Paragraph };
                stack.Peek().Children.Add(dqSubPart);
                stack.Push(dqSubPart);
            }
        }
    }
}