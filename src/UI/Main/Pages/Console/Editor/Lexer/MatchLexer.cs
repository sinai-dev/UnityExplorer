using System;
using System.Collections.Generic;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    public abstract class MatchLexer
    {
        /// <summary>
        /// Get the html formatted color tag that any matched text will be highlighted with.
        /// </summary>
        public abstract string HTMLColor { get; }

        /// <summary>
        /// Get an enumerable collection of special characters that can act as delimiter symbols when they appear before a word.
        /// </summary>
        public virtual IEnumerable<char> SpecialStartCharacters { get { yield break; } }

        /// <summary>
        /// Get an enumerable collection of special characters that can act as delimiter symbols when they appear after a word.
        /// </summary>
        public virtual IEnumerable<char> SpecialEndCharacters { get { yield break; } }

        // Methods
        /// <summary>
        /// Checks the specified lexers current position for a certain sequence of characters as defined by the inheriting matcher.
        /// </summary>
        /// <param name="lexer"></param>
        /// <returns></returns>
        public abstract bool IsImplicitMatch(ILexer lexer);

        /// <summary>
        /// Causes the matcher to invalidate any cached data forcing it to be regenerated or reloaded.
        /// </summary>
        public virtual void Invalidate() { }

        /// <summary>
        /// Attempts to check for a match in the specified lexer.
        /// </summary>
        /// <param name="lexer">The lexer that will be checked</param>
        /// <returns>True if a match was found or false if not</returns>
        public bool IsMatch(ILexer lexer)
        {
            // Check for implicit match
            bool match = IsImplicitMatch(lexer);

            if (match == true)
            {
                // Consume read tokens
                lexer.Commit();
            }
            else
            {
                // Revert lexer state
                lexer.Rollback();
            }

            return match;
        }
    }
}
