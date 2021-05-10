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
        // Initialization and core

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

        public bool IsDelimiter(char character, bool orWhitespace = false, bool orLetterOrDigit = false)
        {
            return delimiters.Contains(character)
                || (orWhitespace && char.IsWhiteSpace(character))
                || (orLetterOrDigit && char.IsLetterOrDigit(character));
        }

        // Lexer enumeration

        public string InputString { get; private set; }
        public int Length => InputString.Length;

        public int LastCommittedIndex { get; private set; }
        public int LookaheadIndex { get; private set; }

        public char Current => !EndOfInput ? InputString[LookaheadIndex] : WHITESPACE;
        public char Previous => LookaheadIndex >= 1 ? InputString[LookaheadIndex - 1] : WHITESPACE;

        public bool EndOfInput => LookaheadIndex >= Length;
        public bool EndOrNewLine => EndOfInput || Current == '\n' || Current == '\r';

        public string SyntaxHighlight(string input)
        {
            var sb = new StringBuilder();
            int lastUnhighlighted = 0;

            // TODO auto indent as part of this parse

            foreach (var match in GetMatches(input))
            {
                // append non-highlighted text between last match and this
                for (int i = lastUnhighlighted; i < match.startIndex; i++)
                    sb.Append(input[i]);

                // append the highlighted match
                sb.Append(match.htmlColorTag);

                for (int i = match.startIndex; i <= match.endIndex && i < input.Length; i++)
                    sb.Append(input[i]);

                sb.Append(SignatureHighlighter.CLOSE_COLOR);

                // update the last unhighlighted start index
                lastUnhighlighted = match.endIndex + 1;
            }

            return sb.ToString();
        }

        public IEnumerable<MatchInfo> GetMatches(string input)
        {
            if (string.IsNullOrEmpty(input))
                yield break;

            InputString = input;
            LastCommittedIndex = -1;
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

        public char PeekNext(int amount = 1)
        {
            LookaheadIndex += amount;
            return Current;
        }

        public void Commit()
        {
            LastCommittedIndex = Math.Min(Length - 1, LookaheadIndex);
        }

        public void Rollback()
        {
            LookaheadIndex = LastCommittedIndex + 1;
        }

        public void RollbackBy(int amount)
        {
            LookaheadIndex = Math.Max(LastCommittedIndex + 1, LookaheadIndex - amount);
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

        #region AUTO INDENT TODO

        // Auto-indenting

        //public int GetIndentLevel(string input, int toIndex)
        //{
        //    bool stringState = false;
        //    int indent = 0;
        //
        //    for (int i = 0; i < toIndex && i < input.Length; i++)
        //    {
        //        char character = input[i];
        //
        //        if (character == '"')
        //            stringState = !stringState;
        //        else if (!stringState && character == INDENT_OPEN)
        //            indent++;
        //        else if (!stringState && character == INDENT_CLOSE)
        //            indent--;
        //    }
        //
        //    if (indent < 0)
        //        indent = 0;
        //
        //    return indent;
        //}

        //// TODO not quite correct, but almost there.
        //
        //public string AutoIndentOnEnter(string input, ref int caretPos)
        //{
        //    var sb = new StringBuilder(input);
        //
        //    bool inString = false;
        //    bool inChar = false;
        //    int currentIndent = 0;
        //    int curLineIndent = 0;
        //    bool prevWasNewLine = true;
        //
        //    // process before caret position
        //    for (int i = 0; i < caretPos; i++)
        //    {
        //        char c = sb[i];
        //
        //        ExplorerCore.Log(i + ": " + c);
        //
        //        // update string/char state
        //        if (!inChar && c == '\"')
        //            inString = !inString;
        //        else if (!inString && c == '\'')
        //            inChar = !inChar;
        //
        //        // continue if inside string or char
        //        if (inString || inChar)
        //            continue;
        //
        //        // check for new line
        //        if (c == '\n')
        //        {
        //            ExplorerCore.Log("new line, resetting line counts");
        //            curLineIndent = 0;
        //            prevWasNewLine = true;
        //        }
        //        // check for indent
        //        else if (c == '\t' && prevWasNewLine)
        //        {
        //            ExplorerCore.Log("its a tab");
        //            if (curLineIndent > currentIndent)
        //            {
        //                ExplorerCore.Log("too many tabs, removing");
        //                // already reached the indent we should have
        //                sb.Remove(i, 1);
        //                i--;
        //                caretPos--;
        //                curLineIndent--;
        //            }
        //            else
        //                curLineIndent++;
        //        }
        //        // remove spaces on new lines
        //        else if (c == ' ' && prevWasNewLine)
        //        {
        //            ExplorerCore.Log("removing newline-space");
        //            sb.Remove(i, 1);
        //            i--;
        //            caretPos--;
        //        }
        //        else
        //        {
        //            if (c == INDENT_CLOSE)
        //                currentIndent--;
        //
        //            if (prevWasNewLine && curLineIndent < currentIndent)
        //            {
        //                ExplorerCore.Log("line is not indented enough");
        //                // line is not indented enough
        //                int diff = currentIndent - curLineIndent;
        //                sb.Insert(i, new string('\t', diff));
        //                caretPos += diff;
        //                i += diff;
        //            }
        //
        //            // check for brackets
        //            if ((c == INDENT_CLOSE || c == INDENT_OPEN) && !prevWasNewLine)
        //            {
        //                ExplorerCore.Log("bracket needs new line");
        //
        //                // need to put it on a new line
        //                sb.Insert(i, $"\n{new string('\t', currentIndent)}");
        //                caretPos += 1 + currentIndent;
        //                i += 1 + currentIndent;
        //            }
        //
        //            if (c == INDENT_OPEN)
        //                currentIndent++;
        //
        //            prevWasNewLine = false;
        //        }
        //    }
        //
        //    // todo put caret on new line after previous bracket if needed
        //    // indent caret to current indent
        //
        //    // process after caret position, make sure there are equal opened/closed brackets
        //    ExplorerCore.Log("-- after caret --");
        //    for (int i = caretPos; i < sb.Length; i++)
        //    {
        //        char c = sb[i];
        //        ExplorerCore.Log(i + ": " + c);
        //
        //        // update string/char state
        //        if (!inChar && c == '\"')
        //            inString = !inString;
        //        else if (!inString && c == '\'')
        //            inChar = !inChar;
        //
        //        if (inString || inChar)
        //            continue;
        //
        //        if (c == INDENT_OPEN)
        //            currentIndent++;
        //        else if (c == INDENT_CLOSE)
        //            currentIndent--;
        //    }
        //
        //    if (currentIndent > 0)
        //    {
        //        ExplorerCore.Log("there are not enough closing brackets, curIndent is " + currentIndent);
        //        // There are not enough close brackets
        //
        //        // TODO this should append in reverse indent order (small indents inserted first, then biggest).
        //        while (currentIndent > 0)
        //        {
        //            ExplorerCore.Log("Inserting closing bracket with " + currentIndent + " indent");
        //            // append the indented '}' on a new line
        //            sb.Insert(caretPos, $"\n{new string('\t', currentIndent - 1)}}}");
        //
        //            currentIndent--;
        //        }
        //
        //    }
        //    //else if (currentIndent < 0)
        //    //{
        //    //    // There are too many close brackets
        //    //
        //    //    // todo?
        //    //}
        //
        //    return sb.ToString();
        //}

        #endregion
    }
}
