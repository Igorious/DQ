using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace DQ.Core.Styling 
{
    public sealed class DqNumberingTable
    {
        private readonly Dictionary<int, DqNumbering> _dqNumberingByID;

        public DqNumberingTable() 
            : this(Enumerable.Empty<DqNumbering>()) { }

        public DqNumberingTable(IEnumerable<DqNumbering> dqNumberings) => 
            _dqNumberingByID = dqNumberings.ToDictionary(x => x.Id);

        [CanBeNull]
        public DqNumbering this[int id] =>
            _dqNumberingByID.TryGetValue(id, out var value)? value : null;
    }
}