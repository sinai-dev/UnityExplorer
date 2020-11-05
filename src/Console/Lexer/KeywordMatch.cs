using System.Collections.Generic;
using UnityEngine;

namespace UnityExplorer.Console.Lexer
{
    public sealed class KeywordMatch : Matcher
    {
        public string keywords;

        public override Color HighlightColor => highlightColor;
        public Color highlightColor;

        private readonly HashSet<string> shortlist = new HashSet<string>();
        private readonly Stack<string> removeList = new Stack<string>();
        public string[] keywordCache = null;

        public override bool IsImplicitMatch(InputLexer lexer)
        {
            BuildKeywordCache();

            if (!char.IsWhiteSpace(lexer.Previous) &&
                !lexer.IsSpecialSymbol(lexer.Previous, SpecialCharacterPosition.End))
            {
                return false;
            }

            shortlist.Clear();

            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            for (int i = 0; i < keywordCache.Length; i++)
            {
                if (keywordCache[i][0] == currentChar)
                {
                    shortlist.Add(keywordCache[i]);
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
                    lexer.IsSpecialSymbol(currentChar, SpecialCharacterPosition.Start))
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

        private void BuildKeywordCache()
        {
            if (keywordCache == null)
            {
                string[] kwSplit = keywords.Split(' ');

                List<string> list = new List<string>();
                foreach (string kw in kwSplit)
                {
                    if (!string.IsNullOrEmpty(kw) && kw.Length > 0)
                    {
                        list.Add(kw);
                    }
                }
                keywordCache = list.ToArray();
            }
        }
    }
}
