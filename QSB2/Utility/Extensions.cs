using System;
using System.Collections.Generic;
using System.Linq;

namespace QSB2.Utility;

public static class Extensions
{
    public static IEnumerable<Type> GetDerivedTypes(this Type type)
    {
        return type.Assembly
            .GetTypes()
            .Where(x => !x.IsInterface && !x.IsAbstract && type.IsAssignableFrom(x))
            .OrderBy(x => x.FullName);
    }
}