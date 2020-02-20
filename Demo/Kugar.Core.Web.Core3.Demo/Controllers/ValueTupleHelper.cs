using System;
using System.Collections.Concurrent;

namespace Kugar.Core.Web.Core3.Demo.Controllers
{
    internal static class ValueTupleHelper
    {
        private static ConcurrentDictionary<Type,bool> _cacheIsValueTuple=new ConcurrentDictionary<Type, bool>();

        public static bool IsValueTuple(this Type type)
        {
            return _cacheIsValueTuple.GetOrAdd(type, x => x.IsValueType && x.IsGenericType &&
                                                          (x.FullName.StartsWith("System.ValueTuple") || x.FullName
                                                              ?.StartsWith("System.ValueTuple`") == true)
            );

        }
    }
}