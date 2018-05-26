using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DQ.Core
{
    [DebuggerDisplay("{Paragraph.Text,nq}")]
    class Token
    {
        public Token(DqParagraph paragraph, int level)
        {
            Paragraph = paragraph;
            Level = level;
        }

        public DqParagraph Paragraph { get; }
        public int Level { get; }
    }

    [DebuggerDisplay("[Node] {Type} {HeaderParagraph.Text,nq}")]
    public class Node
    {
        public Node(DqParagraph paragraph, MainPartType? type, int level)
        {
            HeaderParagraph = paragraph;
            Type = type;
            Level = level;
            paragraph.Meta.Node = this;
        }

        public DqParagraph HeaderParagraph { get; }
        public MainPartType? Type { get; }
        public int Level { get; }
        public List<DqParagraph> ContentParagraphs { get; } = new List<DqParagraph>();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public List<Node> Children { get; } = new List<Node>();
    }

    public class MainParts
    {
        public static IReadOnlyDictionary<string, MainPartType> TypeByText = new Dictionary<string, MainPartType>
        {
            { "реферат", MainPartType.Abstract },
            { "рэферат", MainPartType.Abstract },
            { "abstract", MainPartType.Abstract },

            { "змест", MainPartType.Toc },
            { "содержание", MainPartType.Toc },

            { "введение", MainPartType.Introduction },
            { "уводзіны", MainPartType.Introduction },

            { "вынікі", MainPartType.Conclusion },
            { "заключение", MainPartType.Conclusion },

            { "спіс выкарыстаных крыніц", MainPartType.Bibliography },
            { "список использованных источников", MainPartType.Bibliography },
            { "список использованной литературы", MainPartType.Bibliography },

            { "приложения", MainPartType.Annex },
           // { "приложение a", MainPartType.Annex },
        };
    }

    public enum MainPartType
    {
        Abstract,
        Title,
        Toc,
        Introduction,
        Chapter,
        Conclusion,
        Bibliography,     	
        Annex,
    }

    public class Class1
    {
        public (Node, DqDocument) Go(string path)
        {
            var dqDocument = new DocxParser().Parse(path);
            new NumberingService().RestoreNumbering(dqDocument);

            var dqPartParser = new DqPartParser();
            dqDocument.Structure = dqPartParser.PrimaryParse(dqDocument);
            var headers = new HeaderParser().GetHeaders(dqDocument);
            dqPartParser.SecondaryParse(dqDocument.Structure);

            var root = new HeaderHierarchyService().GetHierarchy(headers, dqDocument);

            var result = new PageAnalyzer().Analyze(dqDocument);
            dqDocument.Paragraphs.Insert(0, new DqParagraph("<Информация о полях документа>", dqDocument.StyleTable.Paragraph.Default) { Index = -1 });
            dqDocument.Paragraphs[0].Meta.Errors.AddRange(result);
           
            new DqReferenceParser().ParseReferences(dqDocument, root);
            new ParagraphAnalyzer().Analyze(dqDocument, root);
            //new SourceAnalyzer().Analyze(root);
            return (root, dqDocument);
        }
    }
}
