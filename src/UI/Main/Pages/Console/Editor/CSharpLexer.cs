using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Explorer.UI.Main.Pages.Console.Lexer;
using System.Runtime.InteropServices;

namespace Explorer.UI.Main.Pages.Console
{
    public static class CSharpLexer
    {
        public static char indentIncreaseCharacter = '{';
        public static char indentDecreaseCharacter = '}';

        public static string delimiterSymbols = "[ ] ( ) { } ; : , .";

        private static readonly StringBuilder indentBuilder = new StringBuilder();

        public static CommentMatch commentMatcher = new CommentMatch();
        public static SymbolMatch symbolMatcher = new SymbolMatch();
        public static NumberMatch numberMatcher = new NumberMatch();
        public static StringMatch stringMatcher = new StringMatch();

        public static KeywordMatch validKeywordMatcher = new KeywordMatch
        {
            highlightColor = new Color(0.33f, 0.61f, 0.83f, 1.0f),
            keywords = @"add as ascending await bool break by byte 
 case catch char checked const continue decimal default descending do dynamic 
 else equals false finally float for foreach from global goto group 
 if in int into is join let lock long new null object on orderby out 
 ref remove return sbyte select short sizeof stackalloc string 
 struct switch throw true try typeof uint ulong ushort 
 value var where while yield"
        };

        public static KeywordMatch invalidKeywordMatcher = new KeywordMatch()
        {
            highlightColor = new Color(0.95f, 0.10f, 0.10f, 1.0f),
            keywords = @"abstract async base class delegate enum explicit extern fixed get 
 implicit interface internal namespace operator override params private protected public 
 using partial readonly sealed set static this unchecked unsafe virtual volatile void"
        };

        private static char[] delimiterSymbolCache = null;
        internal static char[] DelimiterSymbols
        {
            get
            {
                if (delimiterSymbolCache == null)
                {
                    string[] symbols = delimiterSymbols.Split(' ');

                    int count = 0;

                    for (int i = 0; i < symbols.Length; i++)
                        if (symbols[i].Length == 1)
                            count++;

                    delimiterSymbolCache = new char[count];

                    for (int i = 0, index = 0; i < symbols.Length; i++)
                    {
                        if (symbols[i].Length == 1)
                        {
                            delimiterSymbolCache[index] = symbols[i][0];
                            index++;
                        }
                    }
                }
                return delimiterSymbolCache;
            }
        }

        private static MatchLexer[] matchers = null;
        internal static MatchLexer[] Matchers
        {
            get
            {
                if (matchers == null)
                {
                    List<MatchLexer> matcherList = new List<MatchLexer>
                    {
                        commentMatcher,
                        symbolMatcher,
                        numberMatcher,
                        stringMatcher,
                        validKeywordMatcher,
                        invalidKeywordMatcher,
                    };

                    matchers = matcherList.ToArray();
                }
                return matchers;
            }
        }

        public static string GetIndentForInput(string input, int indent, out int caretPosition)
        {
            indentBuilder.Clear();

            indent += 1;

            bool stringState = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '"')
                {
                    stringState = !stringState;
                }

                if (input[i] == '\n')
                {
                    indentBuilder.Append('\n');
                    for (int j = 0; j < indent; j++)
                        indentBuilder.Append("\t");
                }
                else if (input[i] == '\t')
                {
                    continue;
                }
                else if (!stringState && input[i] == indentIncreaseCharacter)
                {
                    indentBuilder.Append(indentIncreaseCharacter);
                    indent++;
                }
                else if (!stringState && input[i] == indentDecreaseCharacter)
                {
                    indentBuilder.Append(indentDecreaseCharacter);
                    indent--;
                }
                else
                {
                    indentBuilder.Append(input[i]);
                }
            }

            string formattedSection = indentBuilder.ToString();

            caretPosition = formattedSection.Length - 1;

            for (int i = formattedSection.Length - 1; i >= 0; i--)
            {
                if (formattedSection[i] == '\n')
                    continue;

                caretPosition = i;
                break;
            }

            return formattedSection;
        }

        public static int GetIndentLevel(string inputString, int startIndex, int endIndex)
        {
            int indent = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                if (inputString[i] == '\t')
                    indent++;

                // Check for end line or other characters
                if (inputString[i] == '\n' || inputString[i] != ' ')
                    break;
            }

            return indent;
        }
    }
}
