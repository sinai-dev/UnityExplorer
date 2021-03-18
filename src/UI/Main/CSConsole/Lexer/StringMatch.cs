using System.Collections.Generic;
using UnityEngine;

namespace UnityExplorer.UI.CSConsole.Lexer
{
    public class StringMatch : Matcher
    {
        public override Color HighlightColor => new Color(0.79f, 0.52f, 0.32f, 1.0f);

        public override IEnumerable<char> StartChars => new[] { '"' };
        public override IEnumerable<char> EndChars => new[] { '"' };

        public override bool IsImplicitMatch(CSLexerHighlighter lexer)
        {
            if (lexer.ReadNext() == '"')
            {
                while (!IsClosingQuoteOrEndFile(lexer, lexer.ReadNext())) { }

                return true;
            }
            return false;
        }

        private bool IsClosingQuoteOrEndFile(CSLexerHighlighter lexer, char character) => lexer.EndOfStream || character == '"';
    }
}
