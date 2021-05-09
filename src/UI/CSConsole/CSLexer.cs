using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using UnityExplorer.UI.CSharpConsole.Lexer;

namespace UnityExplorer.UI.CSharpConsole
{
    public struct LexerMatchInfo
    {
        public int startIndex;
        public int endIndex;
        public string htmlColorTag;
    }

    public enum DelimiterType
    {
        Start,
        End,
    };

    public class CSLexer
    {
        private string inputString;
        private readonly Matcher[] matchers;
        private readonly HashSet<char> startDelimiters;
        private readonly HashSet<char> endDelimiters;
        private int currentIndex;
        private int currentLookaheadIndex;

        public char Current { get; private set; }
        public char Previous { get; private set; }

        public bool EndOfStream => currentLookaheadIndex >= inputString.Length;

        public static char indentOpen = '{';
        public static char indentClose = '}';
        //private static StringBuilder indentBuilder = new StringBuilder();

        public static char[] delimiters = new[]
        {
            '[', ']', '(', ')', '{', '}', ';', ':', ',', '.'
        };

        private static readonly CommentMatch commentMatcher = new CommentMatch();
        private static readonly SymbolMatch symbolMatcher = new SymbolMatch();
        private static readonly NumberMatch numberMatcher = new NumberMatch();
        private static readonly StringMatch stringMatcher = new StringMatch();
        private static readonly KeywordMatch keywordMatcher = new KeywordMatch();

        public CSLexer()
        {
            startDelimiters = new HashSet<char>(delimiters);
            endDelimiters = new HashSet<char>(delimiters);

            this.matchers = new Matcher[]
            {
                    commentMatcher,
                    symbolMatcher,
                    numberMatcher,
                    stringMatcher,
                    keywordMatcher,
            };

            foreach (Matcher lexer in matchers)
            {
                foreach (char c in lexer.StartChars)
                {
                    if (!startDelimiters.Contains(c))
                        startDelimiters.Add(c);
                }

                foreach (char c in lexer.EndChars)
                {
                    if (!endDelimiters.Contains(c))
                        endDelimiters.Add(c);
                }
            }
        }

        public IEnumerable<LexerMatchInfo> GetMatches(string input)
        {
            if (input == null || matchers == null || matchers.Length == 0)
                yield break;

            inputString = input;
            Current = ' ';
            Previous = ' ';
            currentIndex = 0;
            currentLookaheadIndex = 0;

            while (!EndOfStream)
            {
                bool didMatchLexer = false;

                ReadWhiteSpace();

                foreach (Matcher matcher in matchers)
                {
                    int startIndex = currentIndex;

                    bool isMatched = matcher.IsMatch(this);

                    if (isMatched)
                    {
                        int endIndex = currentIndex;

                        didMatchLexer = true;

                        yield return new LexerMatchInfo
                        {
                            startIndex = startIndex,
                            endIndex = endIndex,
                            htmlColorTag = matcher.HexColorTag,
                        };

                        break;
                    }
                }

                if (!didMatchLexer)
                {
                    ReadNext();
                    Commit();
                }
            }
        }

        // Lexer reading

        public char ReadNext()
        {
            if (EndOfStream)
                return '\0';

            Previous = Current;

            Current = inputString[currentLookaheadIndex];
            currentLookaheadIndex++;

            return Current;
        }

        public void Rollback(int amount = -1)
        {
            if (amount == -1)
                currentLookaheadIndex = currentIndex;
            else
            {
                if (currentLookaheadIndex > currentIndex)
                    currentLookaheadIndex -= amount;
            }

            int previousIndex = currentLookaheadIndex - 1;

            if (previousIndex >= inputString.Length)
                Previous = inputString[inputString.Length - 1];
            else if (previousIndex >= 0)
                Previous = inputString[previousIndex];
            else
                Previous = ' ';
        }

        public void Commit()
        {
            currentIndex = currentLookaheadIndex;
        }

        public bool IsSpecialSymbol(char character, DelimiterType position = DelimiterType.Start)
        {
            if (position == DelimiterType.Start)
                return startDelimiters.Contains(character);

            return endDelimiters.Contains(character);
        }

        private void ReadWhiteSpace()
        {
            while (char.IsWhiteSpace(ReadNext()) == true)
                Commit();

            Rollback();
        }

        // Auto-indenting

        public int GetIndentLevel(string input, int toIndex)
        {
            bool stringState = false;
            int indent = 0;

            for (int i = 0; i < toIndex && i < input.Length; i++)
            {
                char character = input[i];

                if (character == '"')
                    stringState = !stringState;
                else if (!stringState && character == indentOpen)
                    indent++;
                else if (!stringState && character == indentClose)
                    indent--;
            }

            if (indent < 0)
                indent = 0;

            return indent;
        }

        // TODO not quite correct, but almost there.

        public string AutoIndentOnEnter(string input, ref int caretPos)
        {
            var sb = new StringBuilder(input);

            bool inString = false;
            bool inChar = false;
            int currentIndent = 0;
            int curLineIndent = 0;
            bool prevWasNewLine = true;

            // process before caret position
            for (int i = 0; i < caretPos; i++)
            {
                char c = sb[i];

                ExplorerCore.Log(i + ": " + c);

                // update string/char state
                if (!inChar && c == '\"')
                    inString = !inString;
                else if (!inString && c == '\'')
                    inChar = !inChar;

                // continue if inside string or char
                if (inString || inChar)
                    continue;

                // check for new line
                if (c == '\n')
                {
                    ExplorerCore.Log("new line, resetting line counts");
                    curLineIndent = 0;
                    prevWasNewLine = true;
                }
                // check for indent
                else if (c == '\t' && prevWasNewLine)
                {
                    ExplorerCore.Log("its a tab");
                    if (curLineIndent > currentIndent)
                    {
                        ExplorerCore.Log("too many tabs, removing");
                        // already reached the indent we should have
                        sb.Remove(i, 1);
                        i--;
                        caretPos--;
                        curLineIndent--;
                    }
                    else
                        curLineIndent++;
                }
                // remove spaces on new lines
                else if (c == ' ' && prevWasNewLine)
                {
                    ExplorerCore.Log("removing newline-space");
                    sb.Remove(i, 1);
                    i--;
                    caretPos--;
                }
                else
                {
                    if (c == indentClose)
                        currentIndent--;

                    if (prevWasNewLine && curLineIndent < currentIndent)
                    {
                        ExplorerCore.Log("line is not indented enough");
                        // line is not indented enough
                        int diff = currentIndent - curLineIndent;
                        sb.Insert(i, new string('\t', diff));
                        caretPos += diff;
                        i += diff;
                    }

                    // check for brackets
                    if ((c == indentClose || c == indentOpen) && !prevWasNewLine)
                    {
                        ExplorerCore.Log("bracket needs new line");

                        // need to put it on a new line
                        sb.Insert(i, $"\n{new string('\t', currentIndent)}");
                        caretPos += 1 + currentIndent;
                        i += 1 + currentIndent;
                    }

                    if (c == indentOpen)
                        currentIndent++;

                    prevWasNewLine = false;
                }
            }
            
            // todo put caret on new line after previous bracket if needed
            // indent caret to current indent

            // process after caret position, make sure there are equal opened/closed brackets
            ExplorerCore.Log("-- after caret --");
            for (int i = caretPos; i < sb.Length; i++)
            {
                char c = sb[i];
                ExplorerCore.Log(i + ": " + c);

                // update string/char state
                if (!inChar && c == '\"')
                    inString = !inString;
                else if (!inString && c == '\'')
                    inChar = !inChar;

                if (inString || inChar)
                    continue;

                if (c == indentOpen)
                    currentIndent++;
                else if (c == indentClose)
                    currentIndent--;
            }

            if (currentIndent > 0)
            {
                ExplorerCore.Log("there are not enough closing brackets, curIndent is " + currentIndent);
                // There are not enough close brackets

                // TODO this should append in reverse indent order (small indents inserted first, then biggest).
                while (currentIndent > 0)
                {
                    ExplorerCore.Log("Inserting closing bracket with " + currentIndent + " indent");
                    // append the indented '}' on a new line
                    sb.Insert(caretPos, $"\n{new string('\t', currentIndent - 1)}}}");

                    currentIndent--;
                }

            }
            //else if (currentIndent < 0)
            //{
            //    // There are too many close brackets
            //
            //    // todo?
            //}

            return sb.ToString();
        }
    }
}
