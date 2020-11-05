using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Helpers;

namespace UnityExplorer.Console
{
    public struct Suggestion
    {
        public string Full => Prefix + Addition;

        public readonly string Prefix;
        public readonly string Addition;
        public readonly Contexts Context;

        public Color TextColor
        {
            get
            {
                switch (Context)
                {
                    case Contexts.Namespace: return Color.grey;
                    case Contexts.Keyword: return systemBlue;
                    default: return Color.white;
                }
            }
        }
        private static readonly Color systemBlue = new Color(80f / 255f, 150f / 255f, 215f / 255f);

        public Suggestion(string addition, string prefix, Contexts type)
        {
            Addition = addition;
            Prefix = prefix;
            Context = type;
        }

        public enum Contexts
        {
            Namespace,
            Keyword,
            Other
        }

        public static HashSet<string> Namespaces => m_namspaces ?? GetNamespaces();
        private static HashSet<string> m_namspaces;

        private static HashSet<string> GetNamespaces()
        {
            HashSet<string> set = new HashSet<string>(
                        AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(GetTypes)
                            .Where(x => x.IsPublic && !string.IsNullOrEmpty(x.Namespace))
                            .Select(x => x.Namespace));

            return m_namspaces = set;

            IEnumerable<Type> GetTypes(Assembly asm) => asm.TryGetTypes();
        }

        public static HashSet<string> Keywords => m_keywords ?? GetKeywords();
        private static HashSet<string> m_keywords;

        private static HashSet<string> GetKeywords()
        {
            if (CSharpLexer.validKeywordMatcher.keywordCache == null)
            {
                return new HashSet<string>();
            }

            HashSet<string> set = new HashSet<string>();

            foreach (string keyword in CSharpLexer.validKeywordMatcher.keywordCache)
            {
                set.Add(keyword);
            }

            return m_keywords = set;
        }
    }
}
