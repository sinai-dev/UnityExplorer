using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.CSharpConsole.Lexers
{
    public class KeywordLexer : Lexer
    {
        private readonly HashSet<string> keywords = new HashSet<string> 
        { "add", "as", "ascending", "await", "bool", "break", "by", "byte",
"case", "catch", "char", "checked", "const", "continue", "decimal", "default", "descending", "do", "dynamic",
"else", "equals", "false", "finally", "float", "for", "foreach", "from", "global", "goto", "group", "if", "in",
"int", "into", "is", "join", "let", "lock", "long", "new", "null", "object", "on", "orderby", "out", "ref",
"remove", "return", "sbyte", "select", "short", "sizeof", "stackalloc", "string", "switch", "throw", "true",
"try", "typeof", "uint", "ulong", "ushort", "var", "where", "while", "yield", "abstract", "async", "base",
"class", "delegate", "enum", "explicit", "extern", "fixed", "get", "implicit", "interface", "internal",
"namespace", "operator", "override", "params", "private", "protected", "public", "using", "partial", "readonly",
"sealed", "set", "static", "struct", "this", "unchecked", "unsafe", "value", "virtual", "volatile", "void" };

        protected override Color HighlightColor => new Color(0.33f, 0.61f, 0.83f, 1.0f);

        public override bool TryMatchCurrent(LexerBuilder lexer)
        {
            if (!lexer.IsDelimiter(lexer.Previous, true))
                return false;

            var sb = new StringBuilder();
            while (!lexer.EndOfInput)
            {
                sb.Append(lexer.Current);
                var next = lexer.PeekNext();
                if (lexer.IsDelimiter(next, true))
                {
                    lexer.RollbackBy(1);
                    break;
                }
            }

            if (keywords.Contains(sb.ToString()))
            {
                lexer.Commit();
                return true;
            }

            return false;
        }
    }
}
