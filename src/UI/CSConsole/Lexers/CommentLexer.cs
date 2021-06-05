using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityExplorer.UI.CSConsole.Lexers
{
    public class CommentLexer : Lexer
    {
        private enum CommentType
        {
            Line,
            Block
        }

        // forest green
        protected override Color HighlightColor => new Color(0.34f, 0.65f, 0.29f, 1.0f);

        public override bool TryMatchCurrent(LexerBuilder lexer)
        {
            if (lexer.Current == '/')
            {
                lexer.PeekNext();
                if (lexer.Current == '/')
                {
                    // line comment. read to end of line or file.
                    do
                    {
                        lexer.Commit();
                        lexer.PeekNext();
                    }
                    while (!lexer.EndOrNewLine);

                    return true;
                }
                else if (lexer.Current == '*')
                {
                    // block comment, read until end of file or closing '*/'
                    lexer.PeekNext();
                    do
                    {
                        lexer.PeekNext();
                        lexer.Commit();
                    }
                    while (!lexer.EndOfInput && !(lexer.Current == '/' && lexer.Previous == '*'));

                    return true;
                }
            }

            return false;
        }
    }
}
