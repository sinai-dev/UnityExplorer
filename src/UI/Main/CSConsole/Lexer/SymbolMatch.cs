using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityExplorer.UI.CSConsole.Lexer
{
    public class SymbolMatch : Matcher
    {
        public override Color HighlightColor => new Color(0.58f, 0.47f, 0.37f, 1.0f);

        private readonly string[] symbols = new[]
        {
            "[", "]", "(", ")", ".", "?", ":", "+", "-", "*", "/", "%", "&", "|", "^", "~", "=", "<", ">", 
            "++", "--", "&&", "||", "<<", ">>", "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "&=",
            "|=", "^=", "<<=", ">>=", "->", "??", "=>",
        };

        private static readonly List<string> shortlist = new List<string>();
        private static readonly Stack<string> removeList = new Stack<string>();

        public override IEnumerable<char> StartChars => symbols.Select(s => s[0]);
        public override IEnumerable<char> EndChars => symbols.Select(s => s[0]);

        public override bool IsImplicitMatch(CSLexerHighlighter lexer)
        {
            if (lexer == null)
                return false;

            if (!char.IsWhiteSpace(lexer.Previous) &&
                !char.IsLetter(lexer.Previous) &&
                !char.IsDigit(lexer.Previous) &&
                !lexer.IsSpecialSymbol(lexer.Previous, DelimiterType.End))
            {
                return false;
            }

            shortlist.Clear();

            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            for (int i = symbols.Length - 1; i >= 0; i--)
            {
                if (symbols[i][0] == currentChar)
                    shortlist.Add(symbols[i]);
            }

            if (shortlist.Count == 0)
                return false;

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
                    char.IsLetter(currentChar) ||
                    char.IsDigit(currentChar) ||
                    lexer.IsSpecialSymbol(currentChar, DelimiterType.Start))
                {
                    RemoveLongStrings(currentIndex);
                    lexer.Rollback(1);
                    break;
                }

                foreach (string symbol in shortlist)
                {
                    if (currentIndex >= symbol.Length || symbol[currentIndex] != currentChar)
                    {
                        removeList.Push(symbol);
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
