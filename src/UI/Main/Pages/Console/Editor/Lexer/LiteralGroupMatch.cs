using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Unstrip.ColorUtility;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    /// <summary>
    /// A matcher that checks for quote strings in the lexer stream.
    /// </summary>
    [Serializable]
    public sealed class LiteralGroupMatch : MatchLexer
    {
        // Private
        private string htmlColor = null;

        // Public
        /// <summary>
        /// Should literal be highlighted.
        /// When true, any text surrounded by double quotes will be highlighted.
        /// </summary>
        public bool highlightLiterals = true;
        /// <summary>
        /// The color that any matched literals will be highlighted.
        /// </summary>
        public Color highlightColor = Color.black;

        // Properties
        /// <summary>
        /// Get a value indicating whether literal highlighting is enabled.
        /// </summary>
        public bool HasLiteralHighlighting
        {
            get { return highlightLiterals; }
        }

        /// <summary>
        /// Get the html formatted color tag that any matched literals will be highlighted with.
        /// </summary>
        public override string HTMLColor
        {
            get
            {
                if (htmlColor == null)
                    htmlColor = "<#" + highlightColor.ToHex() + ">";

                return htmlColor;
            }
        }

        /// <summary>
        /// Returns special symbols that can act as delimiters when appearing before a word. 
        /// In this case '"' will be returned.
        /// </summary>
        public override IEnumerable<char> SpecialStartCharacters
        {
            get
            {
                yield return '"';
            }
        }

        /// <summary>
        /// Returns special symbols that can act as delimiters when appearing after a word.
        /// In this case '"' will be returned.
        /// </summary>
        public override IEnumerable<char> SpecialEndCharacters
        {
            get
            {
                yield return '"';
            }
        }

        // Methods
        /// <summary>
        /// Causes any cached data to be reloaded.
        /// </summary>
        public override void Invalidate()
        {
            this.htmlColor = null;
        }

        /// <summary>
        /// Check whether the specified lexer has a valid literal at its current position.
        /// </summary>
        /// <param name="lexer">The input lexer to check</param>
        /// <returns>True if the stream has a literal or false if not</returns>
        public override bool IsImplicitMatch(ILexer lexer)
        {
            // Skip highlighting
            if (highlightLiterals == false)
                return false;

            // Check for quote
            if (lexer.ReadNext() == '"')
            {
                // Read all characters inside the quote
                while (IsClosingQuoteOrEndFile(lexer, lexer.ReadNext()) == false) ;

                // Found a valid literal 
                return true;
            }
            return false;
        }

        private bool IsClosingQuoteOrEndFile(ILexer lexer, char character)
        {
            if (lexer.EndOfStream == true ||
                character == '"')
            {
                // We have found the end of the file or quote
                return true;
            }
            return false;
        }
    }
}
