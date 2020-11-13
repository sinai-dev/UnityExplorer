using System.Collections.Generic;
using UnityExplorer.Unstrip;
using UnityEngine;
using System.Linq;

namespace UnityExplorer.CSConsole.Lexer
{
    public abstract class Matcher
    {
        public abstract Color HighlightColor { get; }

        public string HexColor => htmlColor ?? (htmlColor = "<color=#" + HighlightColor.ToHex() + ">");
        private string htmlColor;

        public virtual IEnumerable<char> StartChars => Enumerable.Empty<char>();
        public virtual IEnumerable<char> EndChars => Enumerable.Empty<char>();

        public abstract bool IsImplicitMatch(CSharpLexer lexer);

        public bool IsMatch(CSharpLexer lexer)
        {
            if (IsImplicitMatch(lexer))
            {
                lexer.Commit();
                return true;
            }

            lexer.Rollback();
            return false;
        }
    }
}
