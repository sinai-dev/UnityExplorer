using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using ExplorerBeta;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    public sealed class KeywordMatch : MatchLexer
    {
        public string keywords;

        public override Color HighlightColor => this.highlightColor;
        public Color highlightColor;

        private readonly HashSet<string> shortlist = new HashSet<string>();
        private readonly Stack<string> removeList = new Stack<string>();
        private string[] keywordCache = null;

        public override bool IsImplicitMatch(ILexer lexer)
        {
            BuildKeywordCache();

            if (!char.IsWhiteSpace(lexer.Previous) &&
                !lexer.IsSpecialSymbol(lexer.Previous, SpecialCharacterPosition.End))
                return false;

            shortlist.Clear();

            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            for (int i = 0; i < keywordCache.Length; i++)
                if (CompareChar(keywordCache[i][0], currentChar))
                    shortlist.Add(keywordCache[i]);

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
                    lexer.IsSpecialSymbol(currentChar, SpecialCharacterPosition.Start))
                {
                    RemoveLongStrings(currentIndex);
                    lexer.Rollback(1);
                    break;
                }

                foreach (string keyword in shortlist)
                {
                    if (currentIndex >= keyword.Length ||
                        !CompareChar(keyword[currentIndex], currentChar))
                    {
                        removeList.Push(keyword);
                    }
                }

                while (removeList.Count > 0)
                    shortlist.Remove(removeList.Pop());
            }
            while (shortlist.Count > 0);

            return shortlist.Count > 0;
        }

        private void RemoveLongStrings(int length)
        {
            foreach (string keyword in shortlist)
                if (keyword.Length > length)
                    removeList.Push(keyword);

            while (removeList.Count > 0)
                shortlist.Remove(removeList.Pop());
        }

        private void BuildKeywordCache()
        {
            if (keywordCache == null)
            {
                var kwSplit = keywords.Split(' ');

                var list = new List<string>();
                foreach (var kw in kwSplit)
                {
                    if (!string.IsNullOrEmpty(kw) && kw.Length > 0)
                    {
                        list.Add(kw);
                    }
                }
                keywordCache = list.ToArray();
            }
        }

        private bool CompareChar(char a, char b) => 
            (a == b) || (char.ToUpper(a, CultureInfo.CurrentCulture) == char.ToUpper(b, CultureInfo.CurrentCulture));
    }
}
