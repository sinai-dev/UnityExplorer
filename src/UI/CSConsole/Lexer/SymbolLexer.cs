using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.CSharpConsole.Lexers
{
    public class SymbolLexer : Lexer
    {
        protected override Color HighlightColor => Color.white;

        // all symbols are delimiters
        public override IEnumerable<char> Delimiters => uniqueSymbols;

        // all symbol combinations are made of valid individual symbols.
        private readonly HashSet<char> uniqueSymbols = new HashSet<char>
        {
            '[', ']', '{', '}', '(', ')', ',', '.', ';', ':',
            '+', '-', '*', '/', '%', '&', '|', '^', '~', '=', 
            '<', '>', '?', '!', '@'
        };

//        // actual valid symbol combinations
//        private readonly HashSet<string> actualSymbols = new HashSet<string>
//        {
//"[", "]", "(", ")", "{", "}", ".", ",", ";", ":", "+", "-", "*", "/", "%", "&", "|", "^", "~", "=",
//"<", ">", "++", "--", "&&", "||", "<<", ">>", "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", 
//"&=", "|=", "^=", "<<=", ">>=", "->", "!", "?", "??", "@", "=>",
//        };

        public override bool TryMatchCurrent(LexerBuilder lexer)
        {
            // previous character must be delimiter, whitespace, or alphanumeric.
            if (!lexer.IsDelimiter(lexer.Previous, true, true))
                return false;

            if (uniqueSymbols.Contains(lexer.Current))
            {
                do
                {
                    lexer.Commit();
                    lexer.PeekNext();
                }
                while (uniqueSymbols.Contains(lexer.Current));

                return true;
            }

            return false;
        }
    }
}
