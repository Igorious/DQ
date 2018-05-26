using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace DQ.Core.Styling 
{
    public sealed class DqStyleTable
    {
        public sealed class DqStyleList
        {
            private readonly Dictionary<string, DqStyle> _styleByID; 

            public DqStyleList(IEnumerable<DqStyle> styles)
            {
                _styleByID = styles.ToDictionary(s => s.ID);
                Default = _styleByID.Values.Single(s => s.IsDefault);
            }

            [CanBeNull]
            public DqStyle this[string id] => 
                id != null && _styleByID.TryGetValue(id, out var value) ? value : null;

            public DqStyle Default { get; }
        }

        public DqStyleTable(IReadOnlyCollection<DqStyle> styles, DqStyleBasis defaultStyle)
        {
            var styleByID = styles.ToDictionary(s => s.ID);
            var dqDefaultStyle = new DqStyle { Current = defaultStyle, Type = DqStyleType.Paragraph };

            foreach (var dqStyle in styleByID.Values)
            {
                dqStyle.BaseStyle = dqStyle.BaseStyleID != null
                    ? styleByID[dqStyle.BaseStyleID]
                    : dqDefaultStyle;
            }

            Paragraph = new DqStyleList(styleByID.Values.Where(s => s.Type == DqStyleType.Paragraph));
        }

        public DqStyleList Paragraph { get; }
    }
}