using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.CSConsole.Lexers
{
    public class KeywordLexer : Lexer
    {
        // system blue
        protected override Color HighlightColor => new Color(0.33f, 0.61f, 0.83f, 1.0f);

        public static readonly HashSet<string> keywords = new HashSet<string>
        {
// reserved keywords
"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", 
"decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
"fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", 
"long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", 
"readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", 
"this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", 
"volatile", "while",
// contextual keywords
"add", "and", "alias", "ascending", "async", "await", "by", "descending", "dynamic", "equals", "from", "get",
"global", "group", "init", "into", "join", "let", "managed", "nameof",  "not", "notnull", "on",
"or", "orderby", "partial", "record", "remove", "select", "set", "unmanaged", "value", "var", "when", "where",
"where", "with", "yield", "nint", "nuint"
        };

        public override bool TryMatchCurrent(LexerBuilder lexer)
        {
            var prev = lexer.Previous;
            var first = lexer.Current;

            // check for keywords
            if (lexer.IsDelimiter(prev, true) && char.IsLetter(first))
            {
                // can be a keyword...

                var sb = new StringBuilder();
                sb.Append(lexer.Current);
                while (!lexer.EndOfInput && char.IsLetter(lexer.PeekNext()))
                    sb.Append(lexer.Current);

                // next must be whitespace or delimiter
                if (!lexer.EndOfInput && !(char.IsWhiteSpace(lexer.Current) || lexer.IsDelimiter(lexer.Current)))
                    return false;

                if (keywords.Contains(sb.ToString()))
                {
                    if (!lexer.EndOfInput)
                        lexer.RollbackBy(1);
                    lexer.Commit();
                    return true;
                }

                return false;

            }
            else
                return false;
        }
    }
}
