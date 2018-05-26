using System;
using System.Collections.Generic;
using System.Linq;

namespace DQ.Core 
{
    internal sealed class HeaderHierarchyService
    {
        public Node GetHierarchy(IEnumerable<Token> headers, DqDocument document)
        {
            var root = new Node(new DqParagraph("{root}", document.StyleTable.Paragraph.Default), null, -1);
            var lastLevel = 0;

            var stack = new Stack<Node>();
            stack.Push(root);

            var lastIndex = 0;
            foreach (var header in headers)
            {
                var kv = MainParts.TypeByText.FirstOrDefault(k => string.Equals(k.Key, header.Paragraph.Text, StringComparison.OrdinalIgnoreCase));
                var type = kv.Key != null? kv.Value : (MainPartType?) null;

                var targetNode = stack.Peek().Children.LastOrDefault() ?? stack.Peek();
                for (var i = lastIndex; i < header.Paragraph.Index; i++)
                {
                    targetNode.ContentParagraphs.Add(document.Paragraphs[i + 1]);
                }

                lastIndex = header.Paragraph.Index;

                if (header.Level == lastLevel)
                {
                    stack.Peek().Children.Add(new Node(header.Paragraph, type, header.Level));
                }
                else if (header.Level > lastLevel)
                {
                    var last = stack.Peek().Children.Last();
                    stack.Push(last);
                    stack.Peek().Children.Add(new Node(header.Paragraph, type, header.Level));
                    lastLevel = header.Level;
                }
                else
                {
                    var currentLevel = header.Level;
                    while (currentLevel < lastLevel - 1)
                    {
                        stack.Pop();
                        lastLevel--;
                    }

                    stack.Peek().Children.Add(new Node(header.Paragraph, type, header.Level));
                }
            }

            {
                var targetNode = stack.Peek().Children.LastOrDefault() ?? stack.Peek();
                for (var i = lastIndex; i < document.Paragraphs.Count - 1; i++)
                {
                    targetNode.ContentParagraphs.Add(document.Paragraphs[i + 1]);
                }
            }

            TryRemoveSourcesFromHierarhy(root);

            return root;
        }

        private static void TryRemoveSourcesFromHierarhy(Node root)
        {
            var sourcesNode = root.Children.LastOrDefault(c => c.Type == MainPartType.Bibliography);
            if (sourcesNode == null) return;
            
            var sourceIndexStart = root.Children.IndexOf(sourcesNode) + 1;
            var partAfterSources = root.Children.Skip(sourceIndexStart).FirstOrDefault(c => c.Type != null);

            var sourceIndexEnd = partAfterSources != null
                ? root.Children.IndexOf(partAfterSources)
                : root.Children.Count;
            
            var sources = root.Children.GetRange(sourceIndexStart, sourceIndexEnd - sourceIndexStart);
            foreach (var sourceNode in sources)
            {
                sourceNode.HeaderParagraph.Meta.IsHeader = false;
                sourcesNode.ContentParagraphs.Add(sourceNode.HeaderParagraph);
            }

            root.Children.RemoveRange(sourceIndexStart, sourceIndexEnd - sourceIndexStart);
        }
    }
}