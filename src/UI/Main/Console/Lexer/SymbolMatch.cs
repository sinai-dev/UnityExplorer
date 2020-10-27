using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExplorerBeta.UI.Main.Console.Lexer
{
    public sealed class SymbolMatch : MatchLexer
    {
        public override Color HighlightColor => new Color(0.58f, 0.47f, 0.37f, 1.0f);

        public string Symbols => @"[ ] ( ) . ? : + - * / % & | ^ ~ = < > ++ -- && || << >> == != <= >= 
 += -= *= /= %= &= |= ^= <<= >>= -> ?? =>";

        private static readonly List<string> shortlist = new List<string>();
        private static readonly Stack<string> removeList = new Stack<string>();
        private string[] symbolCache = null;

        public override IEnumerable<char> StartChars
        {
            get
            {
                BuildSymbolCache();
                foreach (string symbol in symbolCache.Where(x => x.Length > 0))
                {
                    yield return symbol[0];
                }
            }
        }

        public override IEnumerable<char> EndChars
        {
            get
            {
                BuildSymbolCache();
                foreach (string symbol in symbolCache.Where(x => x.Length > 0))
                {
                    yield return symbol[0];
                }
            }
        }

        public override bool IsImplicitMatch(ILexer lexer)
        {
            if (lexer == null)
            {
                return false;
            }

            BuildSymbolCache();

            if (!char.IsWhiteSpace(lexer.Previous) &&
                !char.IsLetter(lexer.Previous) &&
                !char.IsDigit(lexer.Previous) &&
                !lexer.IsSpecialSymbol(lexer.Previous, SpecialCharacterPosition.End))
            {
                return false;
            }

            shortlist.Clear();

            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            for (int i = symbolCache.Length - 1; i >= 0; i--)
            {
                if (symbolCache[i][0] == currentChar)
                {
                    shortlist.Add(symbolCache[i]);
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
                    char.IsLetter(currentChar) ||
                    char.IsDigit(currentChar) ||
                    lexer.IsSpecialSymbol(currentChar, SpecialCharacterPosition.Start))
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

        private void BuildSymbolCache()
        {
            if (symbolCache != null)
            {
                return;
            }

            string[] symSplit = Symbols.Split(' ');
            List<string> list = new List<string>();
            foreach (string sym in symSplit)
            {
                if (!string.IsNullOrEmpty(sym) && sym.Length > 0)
                {
                    list.Add(sym);
                }
            }
            symbolCache = list.ToArray();
        }
    }
}
