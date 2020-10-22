using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

// Thanks to ManlyMarco for this

namespace Explorer.UI.Main
{
    public struct AutoComplete
    {
        public string Full => Prefix + Addition;

        public readonly string Prefix;
        public readonly string Addition;
        public readonly Contexts Context;

        public Color TextColor => Context == Contexts.Namespace
                                    ? Color.gray
                                    : Color.white;

        public AutoComplete(string addition, string prefix, Contexts type)
        {
            Addition = addition;
            Prefix = prefix;
            Context = type;
        }

        public enum Contexts
        {
            Namespace,
            Other
        }
    }

    public static class AutoCompleteHelpers
    {
        public static HashSet<string> Namespaces => _namespaces ?? GetNamespaces();
        private static HashSet<string> _namespaces;

        private static HashSet<string> GetNamespaces()
        {
            var set = new HashSet<string>(
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(GetTypes)
                            .Where(x => x.IsPublic && !string.IsNullOrEmpty(x.Namespace))
                            .Select(x => x.Namespace));

            return _namespaces = set;

            IEnumerable<Type> GetTypes(Assembly asm) => asm.TryGetTypes();
        }
    }
}
