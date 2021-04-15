using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Core;

namespace UnityExplorer.Core.CSharp
{
    public struct Suggestion
    {
        public enum Contexts
        {
            Namespace,
            Keyword,
            Other
        }

        // ~~~~ Instance ~~~~

        public readonly string Prefix;
        public readonly string Addition;
        public readonly Contexts Context;

        public string Full => Prefix + Addition;

        public Color TextColor => GetTextColor();

        public Suggestion(string addition, string prefix, Contexts type)
        {
            Addition = addition;
            Prefix = prefix;
            Context = type;
        }

        private Color GetTextColor()
        {
            switch (Context)
            {
                case Contexts.Namespace: return Color.grey;
                case Contexts.Keyword: return keywordColor;
                default: return Color.white;
            }
        }

        // ~~~~ Static ~~~~

        public static HashSet<string> Namespaces => m_namespaces ?? GetNamespaces();
        private static HashSet<string> m_namespaces;

        public static HashSet<string> Keywords => throw new NotImplementedException("TODO!"); // m_keywords ?? (m_keywords = new HashSet<string>(CSLexerHighlighter.validKeywordMatcher.Keywords));
        //private static HashSet<string> m_keywords;

        private static readonly Color keywordColor = new Color(80f / 255f, 150f / 255f, 215f / 255f);

        private static HashSet<string> GetNamespaces()
        {
            HashSet<string> set = new HashSet<string>(
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(GetTypes)
                            .Where(x => x.IsPublic && !string.IsNullOrEmpty(x.Namespace))
                            .Select(x => x.Namespace));

            return m_namespaces = set;

            IEnumerable<Type> GetTypes(Assembly asm) => asm.TryGetTypes();
        }
    }
}
