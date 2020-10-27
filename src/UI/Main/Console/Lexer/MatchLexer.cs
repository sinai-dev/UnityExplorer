using System.Collections.Generic;
using ExplorerBeta.Unstrip.ColorUtility;
using UnityEngine;

namespace ExplorerBeta.UI.Main.Console.Lexer
{
    public abstract class MatchLexer
    {
        public abstract Color HighlightColor { get; }

        public string HexColor => htmlColor ?? (htmlColor = "<#" + HighlightColor.ToHex() + ">");
        private string htmlColor = null;

        public virtual IEnumerable<char> StartChars { get { yield break; } }
        public virtual IEnumerable<char> EndChars { get { yield break; } }

        public abstract bool IsImplicitMatch(ILexer lexer);

        public bool IsMatch(ILexer lexer)
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
