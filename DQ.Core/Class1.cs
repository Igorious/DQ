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

    public enum PartType
    {
        Abstract,
        Title,
        Toc,
        Introduction,
        Main,
        Conclusion,
        Bibliography,     	
        Annex,
    }

    public class Class1
    {
        public DqDocument Go(string path)
        {
            var dqDocument = new DocxParser().Parse(path);
            new NumberingService().RestoreNumbering(dqDocument);

            var dqPartParser = new DqPartParser();
            dqDocument.Structure = dqPartParser.PrimaryParse(dqDocument);
            var mainPartHeaders = new HeaderParser().GetHeaders(dqDocument);
            dqPartParser.SecondaryParse(dqDocument.Structure);

            new HeaderHierarchyService().ParseMainPartHierarchy(mainPartHeaders, dqDocument);
           
            new DqReferenceParser().ParseReferences(dqDocument);
            new ParagraphAnalyzer().Analyze(dqDocument);
            new SourceAnalyzer().Analyze(dqDocument);
           
            var result = new PageAnalyzer().Analyze(dqDocument);
            dqDocument.Paragraphs.Insert(0, new DqParagraph("<Информация о полях документа>", dqDocument.StyleTable.Paragraph.Default) { Index = -1 });
            dqDocument.Paragraphs[0].Meta.Errors.AddRange(result);

            return dqDocument;
        }
    }
}
