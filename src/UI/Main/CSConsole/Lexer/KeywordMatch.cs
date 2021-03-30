using System.Collections.Generic;
using UnityEngine;

namespace UnityExplorer.UI.Main.CSConsole.Lexer
{
    // I use two different KeywordMatch instances (valid and invalid).
    // This class just contains common implementations.
    public class KeywordMatch : Matcher
    {
        public string[] Keywords = new[] {"add", "as", "ascending", "await", "bool", "break", "by", "byte",
"case", "catch", "char", "checked", "const", "continue", "decimal", "default", "descending", "do", "dynamic",
"else", "equals", "false", "finally", "float", "for", "foreach", "from", "global", "goto", "group",
"if", "in", "int", "into", "is", "join", "let", "lock", "long", "new", "null", "object", "on", "orderby", "out",
"ref", "remove", "return", "sbyte", "select", "short", "sizeof", "stackalloc", "string",
"switch", "throw", "true", "try", "typeof", "uint", "ulong", "ushort", "var", "where", "while", "yield",
"abstract", "async", "base", "class", "delegate", "enum", "explicit", "extern", "fixed", "get",
"implicit", "interface", "internal", "namespace", "operator", "override", "params", "private", "protected", "public",
"using", "partial", "readonly", "sealed", "set", "static", "struct", "this", "unchecked", "unsafe", "value", "virtual", "volatile", "void" };

        public override Color HighlightColor => highlightColor;
        public Color highlightColor = new Color(0.33f, 0.61f, 0.83f, 1.0f);

        private readonly HashSet<string> shortlist = new HashSet<string>();
        private readonly Stack<string> removeList = new Stack<string>();

        public override bool IsImplicitMatch(CSLexerHighlighter lexer)
        {
            if (!char.IsWhiteSpace(lexer.Previous) &&
                !lexer.IsSpecialSymbol(lexer.Previous, DelimiterType.End))
            {
                return false;
            }

            shortlist.Clear();

            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            for (int i = 0; i < Keywords.Length; i++)
            {
                if (Keywords[i][0] == currentChar)
                {
                    shortlist.Add(Keywords[i]);
                }
            }

            if (shortlist.Count == 0)
            {
                return false;
            }

            do
            {
                if (lexer.EndOfStream)
                {
                    RemoveLongStrings(currentIndex + 1);
                    break;
                }

                currentChar = lexer.ReadNext();
                currentIndex++;

                if (char.IsWhiteSpace(currentChar) ||
                    lexer.IsSpecialSymbol(currentChar, DelimiterType.Start))
                {
                    RemoveLongStrings(currentIndex);
                    lexer.Rollback(1);
                    break;
                }

                foreach (string keyword in shortlist)
                {
                    if (currentIndex >= keyword.Length || keyword[currentIndex] != currentChar)
                    {
                        removeList.Push(keyword);
                    }
                }

                while (removeList.Count > 0)
                {
                    shortlist.Remove(removeList.Pop());
                }
            }
            while (shortlist.Count > 0);

            return shortlist.Count > 0;
        }

        private void RemoveLongStrings(int length)
        {
            foreach (string keyword in shortlist)
            {
                if (keyword.Length > length)
                {
                    removeList.Push(keyword);
                }
            }

            while (removeList.Count > 0)
            {
                shortlist.Remove(removeList.Pop());
            }
        }
    }
}
