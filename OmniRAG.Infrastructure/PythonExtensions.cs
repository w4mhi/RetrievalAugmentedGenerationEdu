using System;
using System.Collections.Generic;

using Python.Runtime;

namespace OmniRAG.Infrastructure;

/// <summary>
/// Extension methods for Python.NET interop.
/// </summary>
public static class PythonExtensions
{
    public static PyObject ToPython(this object obj)
    {
        return obj switch
        {
            null => Runtime.None,
            int i => new PyInt(i),
            long l => new PyInt(l),
            float f => new PyFloat(f),
            double d => new PyFloat(d),
            string s => new PyString(s),
            bool b => b.ToPython(),
            Dictionary<string, object> dict => DictToPython(dict),
            _ => throw new NotSupportedException($"Type {obj.GetType()} not supported for Python conversion")
        };
    }

    private static PyDict DictToPython(Dictionary<string, object> dict)
    {
        PyDict pyDict = new PyDict();
        foreach (KeyValuePair<string, object> kvp in dict)
        {
            pyDict[kvp.Key.ToPython()] = kvp.Value.ToPython();
        }
        return pyDict;
    }
}
