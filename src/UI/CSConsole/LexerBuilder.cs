using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.CSharpConsole.Lexers;

namespace UnityExplorer.UI.CSharpConsole
{
    public struct MatchInfo
    {
        public int startIndex;
        public int endIndex;
        public string htmlColorTag;
    }

    public class LexerBuilder
    {
        #region Core and initialization

        public const char WHITESPACE = ' ';
        public const char INDENT_OPEN = '{';
        public const char INDENT_CLOSE = '}';

        private readonly Lexer[] lexers;
        private readonly HashSet<char> delimiters = new HashSet<char>();

        public LexerBuilder()
        {
            lexers = new Lexer[]
            {
                new CommentLexer(),
                new SymbolLexer(),
                new StringLexer(),
                new NumberLexer(),
                new KeywordLexer(),
            };

            foreach (var matcher in lexers)
            {
                foreach (char c in matcher.Delimiters)
                {
                    if (!delimiters.Contains(c))
                        delimiters.Add(c);
                }
            }
        }

        #endregion

        public int LastCommittedIndex { get; private set; }
        public int LookaheadIndex { get; private set; }

        public char Current => !EndOfInput ? currentInput[LookaheadIndex] : WHITESPACE;
        public char Previous => LookaheadIndex >= 1 ? currentInput[LookaheadIndex - 1] : WHITESPACE;

        public bool EndOfInput => LookaheadIndex > currentEndIdx;
        public bool EndOrNewLine => EndOfInput || Current == '\n' || Current == '\r';

        private string currentInput;
        private int currentStartIdx;
        private int currentEndIdx;

        /// <summary>
        /// Parse the range of the string with the Lexer and build a RichText-highlighted representation of it.
        /// </summary>
        /// <param name="input">The entire input string which you want to parse a section (or all) of</param>
        /// <param name="startIdx">The first character you want to highlight</param>
        /// <param name="endIdx">The last character you want to highlight</param>
        /// <param name="leadingLines">The amount of leading empty lines you want before the first character in the return string.</param>
        /// <returns>A string which contains the amount of leading lines specified, as well as the rich-text highlighted section.</returns>
        public string BuildHighlightedString(string input, int startIdx, int endIdx, int leadingLines)
        {
            if (string.IsNullOrEmpty(input) || endIdx <= startIdx)
                return input;

            currentInput = input;
            currentStartIdx = startIdx;
            currentEndIdx = endIdx;

            var sb = new StringBuilder();

            for (int i = 0; i < leadingLines; i++)
                sb.Append('\n');

            int lastUnhighlighted = startIdx;
            foreach (var match in GetMatches())
            {
                // append non-highlighted text between last match and this
                for (int i = lastUnhighlighted; i < match.startIndex; i++)
                    sb.Append(input[i]);

                // append the highlighted match
                sb.Append(match.htmlColorTag);

                for (int i = match.startIndex; i <= match.endIndex && i <= currentEndIdx; i++)
                    sb.Append(input[i]);

                sb.Append(SignatureHighlighter.CLOSE_COLOR);

                // update the last unhighlighted start index
                lastUnhighlighted = match.endIndex + 1;
            }

            return sb.ToString();
        }


        // Match builder, iterates through each Lexer and returns all matches found.

        private IEnumerable<MatchInfo> GetMatches()
        {
            LastCommittedIndex = currentStartIdx - 1;
            Rollback();

            while (!EndOfInput)
            {
                SkipWhitespace();
                bool anyMatch = false;
                int startIndex = LastCommittedIndex + 1;

                foreach (var lexer in lexers)
                {
                    if (lexer.TryMatchCurrent(this))
                    {
                        anyMatch = true;

                        yield return new MatchInfo
                        {
                            startIndex = startIndex,
                            endIndex = LastCommittedIndex,
                            htmlColorTag = lexer.ColorTag,
                        };
                        break;
                    }
                    else
                        Rollback();
                }

                if (!anyMatch)
                {
                    LookaheadIndex = LastCommittedIndex + 1;
                    Commit();
                }
            }
        }

        // Methods used by the Lexers for interfacing with the current parse process

        public char PeekNext(int amount = 1)
        {
            LookaheadIndex += amount;
            return Current;
        }

        public void Commit()
        {
            LastCommittedIndex = Math.Min(currentEndIdx, LookaheadIndex);
        }

        public void Rollback()
        {
            LookaheadIndex = LastCommittedIndex + 1;
        }

        public void RollbackBy(int amount)
        {
            LookaheadIndex = Math.Max(LastCommittedIndex + 1, LookaheadIndex - amount);
        }

        public bool IsDelimiter(char character, bool orWhitespace = false, bool orLetterOrDigit = false)
        {
            return delimiters.Contains(character)
                || (orWhitespace && char.IsWhiteSpace(character))
                || (orLetterOrDigit && char.IsLetterOrDigit(character));
        }

        private void SkipWhitespace()
        {
            // peek and commit as long as there is whitespace
            while (!EndOfInput && char.IsWhiteSpace(Current))
            {
                Commit();
                PeekNext();
            }

            // revert the last PeekNext which would have returned false
            Rollback();
        }
    }
}
