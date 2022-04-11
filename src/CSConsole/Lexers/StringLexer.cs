using System.Collections.Generic;
using UnityEngine;

namespace UnityExplorer.CSConsole.Lexers
{
    public class StringLexer : Lexer
    {
        public override IEnumerable<char> Delimiters => new[] { '"', '\'', };

        // orange
        protected override Color HighlightColor => new(0.79f, 0.52f, 0.32f, 1.0f);

        public override bool TryMatchCurrent(LexerBuilder lexer)
        {
            if (lexer.Current == '"')
            {
                if (lexer.Previous == '@')
                {
                    // verbatim string, continue until un-escaped quote.
                    while (!lexer.EndOfInput)
                    {
                        lexer.Commit();
                        if (lexer.PeekNext() == '"')
                        {
                            lexer.Commit();
                            // possibly the end, check for escaped quotes.
                            // commit the character and flip the escape bool for each quote.
                            bool escaped = false;
                            while (lexer.PeekNext() == '"')
                            {
                                lexer.Commit();
                                escaped = !escaped;
                            }
                            // if the last quote wasnt escaped, that was the end of the string.
                            if (!escaped)
                                break;
                        }
                    }
                }
                else
                {
                    // normal string
                    // continue until a quote which is not escaped, or end of input

                    while (!lexer.EndOfInput)
                    {
                        lexer.Commit();
                        lexer.PeekNext();
                        if ((lexer.Current == '"') && lexer.Previous != '\\')
                        {
                            lexer.Commit();
                            break;
                        }
                    }
                }

                return true;
            }
            else if (lexer.Current == '\'')
            {
                // char

                while (!lexer.EndOfInput)
                {
                    lexer.Commit();
                    lexer.PeekNext();
                    if ((lexer.Current == '\'') && lexer.Previous != '\\')
                    {
                        lexer.Commit();
                        break;
                    }
                }

                return true;
            }
            else
                return false;
        }
    }
}
