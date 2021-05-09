using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityExplorer.UI.CSharpConsole.Lexer
{
    public abstract class Matcher
    {
        public abstract Color HighlightColor { get; }

        public string HexColorTag => htmlColor ?? (htmlColor = "<color=#" + HighlightColor.ToHex() + ">");
        private string htmlColor;

        public virtual IEnumerable<char> StartChars => Enumerable.Empty<char>();
        public virtual IEnumerable<char> EndChars => Enumerable.Empty<char>();

        public abstract bool IsImplicitMatch(CSLexer lexer);

        public bool IsMatch(CSLexer lexer)
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
