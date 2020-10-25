using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using ExplorerBeta;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    /// <summary>
    /// A matcher that checks for a number of predefined symbols in the lexer stream.
    /// </summary>
    [Serializable]
    public sealed class SymbolGroupMatch : MatchLexer
    {
        // Private
        private static readonly List<string> shortlist = new List<string>();
        private static readonly Stack<string> removeList = new Stack<string>();
        [NonSerialized]
        private string[] symbolCache = null;
        [NonSerialized]
        private string htmlColor = null;

        // Public
        /// <summary>
        /// A string containing one or more symbols separated by a space character that will be used by the matcher.
        /// Symbols can be one or more characters long and should not contain numbers or letters.
        /// </summary>
        public string symbols;
        /// <summary>
        /// The color that any matched symbols will be highlighted.
        /// </summary>
        public Color highlightColor = Color.black;

        // Properties
        /// <summary>
        /// Get a value indicating whether symbol highlighting is enabled based upon the number of valid symbols found.
        /// </summary>
        public bool HasSymbolHighlighting
        {
            get { return symbols.Length > 0; }
        }

        /// <summary>
        /// Get the html formatted color tag that any matched symbols will be highlighted with.
        /// </summary>
        public override string HTMLColor
        {
            get
            {
                // Get html color
                if (htmlColor == null)
                    htmlColor = "<#" + highlightColor.ToHex() + ">";

                return htmlColor;
            }
        }

        /// <summary>
        /// Returns special symbols that can act as delimiters when appearing before a word. 
        /// </summary>
        public override IEnumerable<char> SpecialStartCharacters
        {
            get
            {
                // Make sure cahce is created
                BuildSymbolCache();

                // Get the first character of each symbol
                foreach (string symbol in symbolCache.Where(x => x.Length > 0))
                    yield return symbol[0];
            }
        }

        /// <summary>
        /// Returns special symbols that can act as delimiters when appearing after a word.
        /// In this case '"' will be returned.
        /// </summary>
        public override IEnumerable<char> SpecialEndCharacters
        {
            get
            {
                // Make sure cahce is created
                BuildSymbolCache();

                // Get the first character of each symbol
                foreach (string symbol in symbolCache.Where(x => x.Length > 0))
                    yield return symbol[0];
            }
        }

        // Methods
        /// <summary>
        /// Causes any cached data to be reloaded.
        /// </summary>
        public override void Invalidate()
        {
            this.htmlColor = null;
        }

        /// <summary>
        /// Checks whether the specified lexer has a valid symbol at its current posiiton.
        /// </summary>
        /// <param name="lexer">The input lexer to check</param>
        /// <returns>True if the stream has a symbol or false if not</returns>
        public override bool IsImplicitMatch(ILexer lexer)
        {
            // Make sure cache is created
            BuildSymbolCache();

            if (lexer == null)
            {
                return false;
            }

            // Require whitespace, letter or digit before symbol
            if (char.IsWhiteSpace(lexer.Previous) == false &&
                char.IsLetter(lexer.Previous) == false &&
                char.IsDigit(lexer.Previous) == false &&
                lexer.IsSpecialSymbol(lexer.Previous, SpecialCharacterPosition.End) == false)
                return false;

            // Clear old data
            shortlist.Clear();

            // Read the first character
            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            // Add to shortlist
            for (int i = symbolCache.Length - 1; i >= 0; i--)
            {
                if (symbolCache[i][0] == currentChar)
                    shortlist.Add(symbolCache[i]);
            }

            // No potential matches
            if (shortlist.Count == 0)
                return false;

            do
            {
                // Check for end of stream
                if (lexer.EndOfStream == true)
                {
                    RemoveLongStrings(currentIndex + 1);
                    break;
                }

                // Read next character
                currentChar = lexer.ReadNext();
                currentIndex++;

                if (char.IsWhiteSpace(currentChar) == true ||
                    char.IsLetter(currentChar) == true ||
                    char.IsDigit(currentChar) == true ||
                    lexer.IsSpecialSymbol(currentChar, SpecialCharacterPosition.Start) == true)
                {
                    RemoveLongStrings(currentIndex);
                    lexer.Rollback(1);
                    break;
                }

                // Check for shortlist match
                foreach (string symbol in shortlist)
                {
                    if (currentIndex >= symbol.Length ||
                        symbol[currentIndex] != currentChar)
                    {
                        removeList.Push(symbol);
                    }
                }

                // Remove from short list
                while (removeList.Count > 0)
                    shortlist.Remove(removeList.Pop());
            }
            while (shortlist.Count > 0);

            // Check for any words in the shortlist
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

            // Remove from short list
            while (removeList.Count > 0)
                shortlist.Remove(removeList.Pop());
        }

        private void BuildSymbolCache()
        {
            if (string.IsNullOrEmpty(symbols))
            {
                ExplorerCore.LogWarning("Symbol cache is null!");
                symbolCache = new string[0];
            }
            else
            {
                // Get symbols and insert them into a cache array for quick reference
                var symSplit = symbols.Split(' ');
                var list = new List<string>();
                foreach (var sym in symSplit)
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
}
