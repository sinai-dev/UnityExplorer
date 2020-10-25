using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    /// <summary>
    /// A matcher that checks for a number of predefined keywords in the lexer stream.
    /// </summary>
    public sealed class KeywordGroupMatch : MatchLexer
    {
        // Private
        private static readonly HashSet<string> shortlist = new HashSet<string>();
        private static readonly Stack<string> removeList = new Stack<string>();
        private string[] keywordCache = null;
        private string htmlColor = null;

        // Public
        /// <summary>
        /// Used for editor gui only. Has no purpose other than to give the inspector foldout a nice name
        /// </summary>
        public string group = "Keyword Group";      // This value is not used - it just gives the inspector foldout a nice name
        /// <summary>
        /// A string containing one or more keywords separated by a space character that will be used by this matcher.
        /// </summary>
        public string keywords;
        /// <summary>
        /// The color that any matched keywords will be highlighted.
        /// </summary>
        public Color highlightColor = Color.black;
        /// <summary>
        /// Should keyword matching be case sensitive.
        /// </summary>
        public bool caseSensitive = true;

        // Properties
        /// <summary>
        /// Get a value indicating whether keyword highlighting is enabled based upon the number of valid keywords found.
        /// </summary>
        public bool HasKeywordHighlighting
        {
            get
            {
                // Check for valid keyword
                if (string.IsNullOrEmpty(keywords) == false)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Get the html formatted color tag that any matched keywords will be highlighted with.
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

        // Methods
        /// <summary>
        /// Causes any cached data to be reloaded.
        /// </summary>
        public override void Invalidate()
        {
            this.htmlColor = null;
        }

        /// <summary>
        /// Check whether the specified lexer has a valid keyword at its current position.
        /// </summary>
        /// <param name="lexer">The input lexer to check</param>
        /// <returns>True if the stream has a keyword or false if not</returns>
        public override bool IsImplicitMatch(ILexer lexer)
        {
            // Make sure cache is built
            BuildKeywordCache();

            // Require whitespace before character
            if (char.IsWhiteSpace(lexer.Previous) == false &&
                lexer.IsSpecialSymbol(lexer.Previous, SpecialCharacterPosition.End) == false)
                return false;

            // Clear old data
            shortlist.Clear();

            // Read the first character
            int currentIndex = 0;
            char currentChar = lexer.ReadNext();

            // Add to shortlist
            for (int i = 0; i < keywordCache.Length; i++)
                if (CompareChar(keywordCache[i][0], currentChar) == true)
                    shortlist.Add(keywordCache[i]);

            // Check for no matches we can skip the heavy work quickly
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

                // Check for end of word
                if (char.IsWhiteSpace(currentChar) == true ||
                    lexer.IsSpecialSymbol(currentChar, SpecialCharacterPosition.Start) == true)
                {
                    // Finalize any matching candidates and undo the reading of the space or special character
                    RemoveLongStrings(currentIndex);
                    lexer.Rollback(1);
                    break;
                }

                // Check for shortlist match
                foreach (string keyword in shortlist)
                {
                    if (currentIndex >= keyword.Length ||
                        CompareChar(keyword[currentIndex], currentChar) == false)
                    {
                        removeList.Push(keyword);
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

        private void BuildKeywordCache()
        {
            // Check if we need to build the cache
            if (keywordCache == null)
            {
                // Get keyowrds and insert them into a cache array for quick reference
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

        private bool CompareChar(char a, char b)
        {
            // Check for direct match
            if (a == b)
                return true;

            // Check for case sensitive
            if (caseSensitive == false)
            {
                if (char.ToUpper(a, CultureInfo.CurrentCulture) ==
                    char.ToUpper(b, CultureInfo.CurrentCulture))
                {
                    // Check for match ignoring case
                    return true;
                }
            }
            return false;
        }
    }
}
