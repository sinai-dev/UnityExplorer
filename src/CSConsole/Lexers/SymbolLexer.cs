using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.CSConsole.Lexers
{
    public class SymbolLexer : Lexer
    {
        // silver
        protected override Color HighlightColor => new Color(0.6f, 0.6f, 0.6f);

        // all symbols are delimiters
        public override IEnumerable<char> Delimiters => symbols.Where(it => it != '.'); // '.' is not a delimiter, only a separator.

        public static bool IsSymbol(char c) => symbols.Contains(c);

        public static readonly HashSet<char> symbols = new HashSet<char>
        {
            '[', '{', '(',                  // open
            ']', '}', ')',                  // close
            '.', ',', ';', ':', '?', '@',   // special

            // operators
            '+', '-', '*', '/', '%', '&', '|', '^', '~', '=', '<', '>', '!',
        };

        public override bool TryMatchCurrent(LexerBuilder lexer)
        {
            // previous character must be delimiter, whitespace, or alphanumeric.
            if (!lexer.IsDelimiter(lexer.Previous, true, true))
                return false;

            if (IsSymbol(lexer.Current))
            {
                do
                {
                    lexer.Commit();
                    lexer.PeekNext();
                }
                while (IsSymbol(lexer.Current));

                return true;
            }

            return false;
        }
    }
}
