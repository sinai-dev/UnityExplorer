using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Explorer.UI.Main.Pages.Console.Lexer;
using System.Runtime.InteropServices;

namespace Explorer.UI.Main.Pages.Console
{
    public static class CodeTheme
    {
        internal static readonly StringBuilder sharedBuilder = new StringBuilder();

        private static char[] delimiterSymbolCache = null;
        private static MatchLexer[] matchers = null;

        public static string languageName = "C#";

        public static string delimiterSymbols = "[ ] ( ) { } ; : , .";

        public static KeywordGroupMatch[] keywordGroups = new KeywordGroupMatch[]
        {
            // VALID KEYWORDS

            new KeywordGroupMatch()
            {
                highlightColor = new Color(0.33f, 0.61f, 0.83f, 1.0f),
                caseSensitive = true,
                keywords = @"add as ascending await base bool break by byte 
 case catch char checked const continue decimal default descending do dynamic 
 else enum equals false finally fixed float for foreach from global goto group 
 if in int into is join let lock long new null object on orderby out params 
 partial ref remove return sbyte sealed select short sizeof stackalloc string 
 struct switch this throw true try typeof uint ulong unchecked unsafe ushort 
 using value var void where while yield"
            },

            // INVALID KEYWORDS (cannot use inside method scope)

            new KeywordGroupMatch()
            {
                highlightColor = new Color(0.95f, 0.10f, 0.10f, 1.0f),
                caseSensitive = true,
                keywords = @"abstract async class delegate explicit extern get 
 implicit interface internal namespace operator override private protected public 
 readonly set static virtual volatile"
            }
        };

        /// <summary>
        /// A symbol group used to specify which symbols should be highlighted.
        /// </summary>
        public static SymbolGroupMatch symbolGroup = new SymbolGroupMatch
        {
            symbols = @"[ ] ( ) . ? : + - * / % & | ^ ~ = < > ++ -- && || << >> == != <= >=
 += -= *= /= %= &= |= ^= <<= >>= -> ?? =>",
            highlightColor = new Color(0.58f, 0.47f, 0.37f, 1.0f),
        };

        /// <summary>
        /// A number group used to specify whether numbers should be highlighted.
        /// </summary>
        public static NumberGroupMatch numberGroup = new NumberGroupMatch
        {
            highlightNumbers = true,
            highlightColor = new Color(0.58f, 0.33f, 0.33f, 1.0f)
        };

        /// <summary>
        /// A comment group used to specify which strings open and close comments.
        /// </summary>
        public static CommentGroupMatch commentGroup = new CommentGroupMatch
        {
            blockCommentEnd = @"*/",
            blockCommentStart = @"/*",
            lineCommentStart = @"//",
            lineCommentHasPresedence = true,
            highlightColor = new Color(0.34f, 0.65f, 0.29f, 1.0f),
        };

        /// <summary>
        /// A literal group used to specify whether quote strings should be highlighted.
        /// </summary>
        public static LiteralGroupMatch literalGroup = new LiteralGroupMatch
        {
            highlightLiterals = true,
            highlightColor = new Color(0.79f, 0.52f, 0.32f, 1.0f)
        };

        ///// <summary>
        ///// Options group for all auto indent related settings.
        ///// </summary>
        //public static AutoIndent autoIndent;

        // Properties
        internal static char[] DelimiterSymbols
        {
            get
            {
                if (delimiterSymbolCache == null)
                {
                    // Split by space
                    string[] symbols = delimiterSymbols.Split(' ');

                    int count = 0;

                    // Count the number of valid symbols
                    for (int i = 0; i < symbols.Length; i++)
                        if (symbols[i].Length == 1)
                            count++;

                    // Allocate array
                    delimiterSymbolCache = new char[count];

                    // Copy symbols
                    for (int i = 0, index = 0; i < symbols.Length; i++)
                    {
                        // Require only 1 character
                        if (symbols[i].Length == 1)
                        {
                            // Get the first character for the string
                            delimiterSymbolCache[index] = symbols[i][0];
                            index++;
                        }
                    }
                }
                return delimiterSymbolCache;
            }
        }

        internal static MatchLexer[] Matchers
        {
            get
            {
                if (matchers == null)
                {
                    List<MatchLexer> matcherList = new List<MatchLexer>
                    {
                        commentGroup,
                        symbolGroup,
                        numberGroup,
                        literalGroup
                    };
                    matcherList.AddRange(keywordGroups);

                    matchers = matcherList.ToArray();
                }
                return matchers;
            }
        }

        // Methods
        internal static void Invalidate()
        {
            foreach (KeywordGroupMatch group in keywordGroups)
                group.Invalidate();

            symbolGroup.Invalidate();
            commentGroup.Invalidate();
            numberGroup.Invalidate();
            literalGroup.Invalidate();
        }
    }
}
