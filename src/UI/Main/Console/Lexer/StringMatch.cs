using System.Collections.Generic;
using UnityEngine;

namespace UnityExplorer.UI.Main.Console.Lexer
{
    public sealed class StringMatch : Matcher
    {
        public override Color HighlightColor => new Color(0.79f, 0.52f, 0.32f, 1.0f);

        public override IEnumerable<char> StartChars { get { yield return '"'; } }
        public override IEnumerable<char> EndChars { get { yield return '"'; } }

        public override bool IsImplicitMatch(InputLexer lexer)
        {
            if (lexer.ReadNext() == '"')
            {
                while (!IsClosingQuoteOrEndFile(lexer, lexer.ReadNext()))
                {
                    ;
                }

                return true;
            }
            return false;
        }

        private bool IsClosingQuoteOrEndFile(InputLexer lexer, char character)
        {
            if (lexer.EndOfStream == true ||
                character == '"')
            {
                return true;
            }
            return false;
        }
    }
}
