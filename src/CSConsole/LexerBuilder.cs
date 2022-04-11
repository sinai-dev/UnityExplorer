using System;
using System.Collections.Generic;
using System.Text;
using UnityExplorer.CSConsole.Lexers;
using UniverseLib.Utility;

namespace UnityExplorer.CSConsole
{
    public struct MatchInfo
    {
        public int startIndex;
        public int endIndex;
        public bool isStringOrComment;
        public bool matchToEndOfLine;
        public string htmlColorTag;
    }

    public class LexerBuilder
    {
        #region Core and initialization

        public const char WHITESPACE = ' ';
        public readonly HashSet<char> IndentOpenChars = new() { '{', '(' };
        public readonly HashSet<char> IndentCloseChars = new() { '}', ')' };

        private readonly Lexer[] lexers;
        private readonly HashSet<char> delimiters = new();

        private readonly StringLexer stringLexer = new();
        private readonly CommentLexer commentLexer = new();

        public LexerBuilder()
        {
            lexers = new Lexer[]
            {
                commentLexer,
                stringLexer,
                new SymbolLexer(),
                new NumberLexer(),
                new KeywordLexer(),
            };

            foreach (Lexer matcher in lexers)
            {
                foreach (char c in matcher.Delimiters)
                {
                    if (!delimiters.Contains(c))
                        delimiters.Add(c);
                }
            }
        }

        #endregion

        /// <summary>The last committed index for a match or no-match. Starts at -1 for a new parse.</summary>
        public int CommittedIndex { get; private set; }
        /// <summary>The index of the character we are currently parsing, at minimum it will be CommittedIndex + 1.</summary>
        public int CurrentIndex { get; private set; }

        /// <summary>The current character we are parsing, determined by CurrentIndex.</summary>
        public char Current => !EndOfInput ? currentInput[CurrentIndex] : WHITESPACE;
        /// <summary>The previous character (CurrentIndex - 1), or whitespace if no previous character.</summary>
        public char Previous => CurrentIndex >= 1 ? currentInput[CurrentIndex - 1] : WHITESPACE;

        /// <summary>Returns true if CurrentIndex is >= the current input length.</summary>
        public bool EndOfInput => CurrentIndex > currentEndIdx;
        /// <summary>Returns true if EndOfInput or current character is a new line.</summary>
        public bool EndOrNewLine => EndOfInput || IsNewLine(Current);

        public static bool IsNewLine(char c) => c == '\n' || c == '\r';

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
        public string BuildHighlightedString(string input, int startIdx, int endIdx, int leadingLines, int caretIdx, out bool caretInStringOrComment)
        {
            caretInStringOrComment = false;

            if (string.IsNullOrEmpty(input) || endIdx <= startIdx)
                return input;

            currentInput = input;
            currentStartIdx = startIdx;
            currentEndIdx = endIdx;

            StringBuilder sb = new();

            for (int i = 0; i < leadingLines; i++)
                sb.Append('\n');

            int lastUnhighlighted = startIdx;
            foreach (MatchInfo match in GetMatches())
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

                int matchEndIdx = match.endIndex;
                if (match.matchToEndOfLine)
                {
                    while (input.Length - 1 >= matchEndIdx)
                    {
                        matchEndIdx++;
                        if (IsNewLine(input[matchEndIdx]))
                            break;
                    }
                }

                // check caretIdx to determine inStringOrComment state
                if (caretIdx >= match.startIndex && (caretIdx <= (matchEndIdx + 1) || (caretIdx >= input.Length && matchEndIdx >= input.Length - 1)))
                    caretInStringOrComment = match.isStringOrComment;
            }

            // Append trailing unhighlighted input
            while (lastUnhighlighted <= endIdx)
            {
                sb.Append(input[lastUnhighlighted]);
                lastUnhighlighted++;
            }

            return sb.ToString();
        }


        // Match builder, iterates through each Lexer and returns all matches found.

        public IEnumerable<MatchInfo> GetMatches()
        {
            CommittedIndex = currentStartIdx - 1;
            Rollback();

            while (!EndOfInput)
            {
                SkipWhitespace();
                bool anyMatch = false;
                int startIndex = CommittedIndex + 1;

                foreach (Lexer lexer in lexers)
                {
                    if (lexer.TryMatchCurrent(this))
                    {
                        anyMatch = true;

                        yield return new MatchInfo
                        {
                            startIndex = startIndex,
                            endIndex = CommittedIndex,
                            htmlColorTag = lexer.ColorTag,
                            isStringOrComment = lexer is StringLexer || lexer is CommentLexer,
                        };
                        break;
                    }
                    else
                        Rollback();
                }

                if (!anyMatch)
                {
                    CurrentIndex = CommittedIndex + 1;
                    Commit();
                }
            }
        }

        // Methods used by the Lexers for interfacing with the current parse process

        public char PeekNext(int amount = 1)
        {
            CurrentIndex += amount;
            return Current;
        }

        public void Commit()
        {
            CommittedIndex = Math.Min(currentEndIdx, CurrentIndex);
        }

        public void Rollback()
        {
            CurrentIndex = CommittedIndex + 1;
        }

        public void RollbackBy(int amount)
        {
            CurrentIndex = Math.Max(CommittedIndex + 1, CurrentIndex - amount);
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

            if (!char.IsWhiteSpace(Current))
                Rollback();
        }

        #region Auto Indenting

        // Using the Lexer for indenting as it already has what we need to tokenize strings and comments.
        // At the moment this only handles when a single newline or close-delimiter is composed.
        // Does not handle copy+paste or any other characters yet.

        public string IndentCharacter(string input, ref int caretIndex)
        {
            int lastCharIndex = caretIndex - 1;
            char c = input[lastCharIndex];

            // we only want to indent for new lines and close indents
            if (!IsNewLine(c) && !IndentCloseChars.Contains(c))
                return input;

            // perform a light parse up to the caret to determine indent level
            currentInput = input;
            currentStartIdx = 0;
            currentEndIdx = lastCharIndex;
            CommittedIndex = -1;
            Rollback();

            int indent = 0;

            while (!EndOfInput)
            {
                if (CurrentIndex >= lastCharIndex)
                {
                    // reached the caret index
                    if (indent <= 0)
                        break;

                    if (IsNewLine(c))
                        input = IndentNewLine(input, indent, ref caretIndex);
                    else // closing indent
                        input = IndentCloseDelimiter(input, indent, lastCharIndex, ref caretIndex);

                    break;
                }

                // Try match strings and comments (Lexer will commit to the end of the match)
                if (stringLexer.TryMatchCurrent(this) || commentLexer.TryMatchCurrent(this))
                {
                    PeekNext();
                    continue;
                }

                // Still parsing, check indent

                if (IndentOpenChars.Contains(Current))
                    indent++;
                else if (IndentCloseChars.Contains(Current))
                    indent--;

                Commit();
                PeekNext();
            }

            return input;
        }

        private string IndentNewLine(string input, int indent, ref int caretIndex)
        {
            // continue until the end of line or next non-whitespace character.
            // if there's a close-indent on this line, reduce the indent level.
            while (CurrentIndex < input.Length - 1)
            {
                CurrentIndex++;
                char next = input[CurrentIndex];
                if (IsNewLine(next))
                    break;
                if (char.IsWhiteSpace(next))
                    continue;
                else if (IndentCloseChars.Contains(next))
                    indent--;

                break;
            }

            if (indent > 0)
            {
                input = input.Insert(caretIndex, new string('\t', indent));
                caretIndex += indent;
            }

            return input;
        }

        private string IndentCloseDelimiter(string input, int indent, int lastCharIndex, ref int caretIndex)
        {
            if (CurrentIndex > lastCharIndex)
            {
                return input;
            }

            // lower the indent level by one as we would not have accounted for this closing symbol
            indent--;

            // go back from the caret to the start of the line, calculate how much indent we need to adjust.
            while (CurrentIndex > 0)
            {
                CurrentIndex--;
                char prev = input[CurrentIndex];
                if (IsNewLine(prev))
                    break;
                if (!char.IsWhiteSpace(prev))
                {
                    // the line containing the closing bracket has non-whitespace characters before it. do not indent.
                    indent = 0;
                    break;
                }
                else if (prev == '\t')
                    indent--;
            }

            if (indent > 0)
            {
                input = input.Insert(caretIndex, new string('\t', indent));
                caretIndex += indent;
            }
            else if (indent < 0)
            {
                // line is overly indented
                input = input.Remove(lastCharIndex - 1, -indent);
                caretIndex += indent;
            }

            return input;
        }

        #endregion
    }
}
