using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DQ.Core
{
    public static class CollectionExtensions
    {
        public static bool IsEmpty(this ICollection collection) => collection.Count == 0;
    }
}
