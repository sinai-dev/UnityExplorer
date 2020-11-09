using System.Collections.Generic;
using UnityExplorer.Unstrip;
using UnityEngine;

namespace UnityExplorer.Console.Lexer
{
    public abstract class Matcher
    {
        public abstract Color HighlightColor { get; }

        public string HexColor => htmlColor ?? (htmlColor = "<#" + HighlightColor.ToHex() + ">");
        private string htmlColor = null;

        public virtual IEnumerable<char> StartChars { get { yield break; } }
        public virtual IEnumerable<char> EndChars { get { yield break; } }

        public abstract bool IsImplicitMatch(InputLexer lexer);

        public bool IsMatch(InputLexer lexer)
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
