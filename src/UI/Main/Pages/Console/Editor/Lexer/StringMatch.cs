using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    public sealed class StringMatch : MatchLexer
    {
        public override Color HighlightColor => new Color(0.79f, 0.52f, 0.32f, 1.0f);

        public override IEnumerable<char> StartChars { get { yield return '"'; } }
        public override IEnumerable<char> EndChars { get { yield return '"'; } }

        public override bool IsImplicitMatch(ILexer lexer)
        {
            if (lexer.ReadNext() == '"')
            {
                while (!IsClosingQuoteOrEndFile(lexer, lexer.ReadNext())) ;
                return true;
            }
            return false;
        }

        private bool IsClosingQuoteOrEndFile(ILexer lexer, char character)
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
