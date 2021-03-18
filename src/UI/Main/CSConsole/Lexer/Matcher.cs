using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityExplorer.Core.Unity;

namespace UnityExplorer.UI.CSConsole.Lexer
{
    public abstract class Matcher
    {
        public abstract Color HighlightColor { get; }

        public string HexColor => htmlColor ?? (htmlColor = "<color=#" + HighlightColor.ToHex() + ">");
        private string htmlColor;

        public virtual IEnumerable<char> StartChars => Enumerable.Empty<char>();
        public virtual IEnumerable<char> EndChars => Enumerable.Empty<char>();

        public abstract bool IsImplicitMatch(CSLexerHighlighter lexer);

        public bool IsMatch(CSLexerHighlighter lexer)
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
