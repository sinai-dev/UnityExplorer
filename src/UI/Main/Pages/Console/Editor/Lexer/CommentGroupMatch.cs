using System;
using System.Collections.Generic;
using Explorer.Unstrip.ColorUtility;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{ 
    /// <summary>
    /// Used to match line and block comments.
    /// </summary>
    public sealed class CommentGroupMatch : MatchLexer
    {
        [NonSerialized]
        private string htmlColor = null;

        // Public
        /// <summary>
        /// The string that denotes the start of a line comment.
        /// Leave this value empty if line comments should not be highlighted.
        /// </summary>
        public string lineCommentStart;
        /// <summary>
        /// The string that denotes the start of a block comment.
        /// Leave this value empty if block comments should not be highlighted.
        /// </summary>
        public string blockCommentStart;
        /// <summary>
        /// The string that denotes the end of a block comment.
        /// </summary>
        public string blockCommentEnd;
        /// <summary>
        /// The color that comments will be highlighted with.
        /// </summary>
        public Color highlightColor = Color.black;

        public bool lineCommentHasPresedence = true;

        // Properties
        /// <summary>
        /// Retrusn a value indicating whether any comment highlighting is enabled.
        /// A valid line or block comment start string must be specified in order for comment highlighting to be enabled.
        /// </summary>
        public bool HasCommentHighlighting
        {
            get
            {
                return string.IsNullOrEmpty(lineCommentStart) == false ||
                    string.IsNullOrEmpty(blockCommentStart) == false;
            }
        }

        /// <summary>
        /// Get the html tag color that comments will be highlighted with.
        /// </summary>
        public override string HTMLColor
        {
            get
            {
            // Build html color string
            if (htmlColor == null)
                htmlColor = "<#" + highlightColor.ToHex() + ">";

                return htmlColor;
            }
        }

        /// <summary>
        /// Returns an enumerable collection of characters from this group that can act as delimiter symbols when they appear after a keyword.
        /// </summary>
        public override IEnumerable<char> SpecialStartCharacters
        {
            get
            {
                if (string.IsNullOrEmpty(lineCommentStart) == false)
                    yield return lineCommentStart[0];

                if (string.IsNullOrEmpty(blockCommentEnd) == false)
                    yield return blockCommentEnd[0];
            }
        }

        /// <summary>
        /// Returns an enumerable collection of characters from this group that can act as delimiter symbols when they appear before a keyword.
        /// </summary>
        public override IEnumerable<char> SpecialEndCharacters
        {
            get
            {
                if (string.IsNullOrEmpty(blockCommentEnd) == false)
                    yield return blockCommentEnd[blockCommentEnd.Length - 1];
            }
        }

        // Methods
        /// <summary>
        /// Causes the cached values to be reloaded.
        /// Useful for editor visualisation.
        /// </summary>
        public override void Invalidate()
        {
            this.htmlColor = null;
        }

        /// <summary>
        /// Returns true if the lexer input contains a valid comment format as the next character sequence.
        /// </summary>
        /// <param name="lexer">The input lexer</param>
        /// <returns>True if a comment was found or false if not</returns>
        public override bool IsImplicitMatch(ILexer lexer)
        {
            if (lineCommentHasPresedence == true)
            {
                // Parse line comments then block comments
                if (IsLineCommentMatch(lexer) == true ||
                    IsBlockCommentMatch(lexer) == true)
                    return true;
            }
            else
            {
                // Parse block comments then line coments
                if (IsBlockCommentMatch(lexer) == true ||
                    IsLineCommentMatch(lexer) == true)
                    return true;
            }

            // Not a comment
            return false;
        }

        private bool IsLineCommentMatch(ILexer lexer)
        {
            // Check for line comment
            if (string.IsNullOrEmpty(lineCommentStart) == false)
            {
                lexer.Rollback();

                bool match = true;

                for (int i = 0; i < lineCommentStart.Length; i++)
                {
                    if (lineCommentStart[i] != lexer.ReadNext())
                    {
                        match = false;
                        break;
                    }
                }

                // Check for valid match
                if (match == true)
                {
                    // Read until end
                    while (IsEndLineOrEndFile(lexer, lexer.ReadNext()) == false) ;

                    // Matched a single line comment
                    return true;
                }
            }
            return false;
        }

        private bool IsBlockCommentMatch(ILexer lexer)
        {
            // Check for block comment
            if (string.IsNullOrEmpty(blockCommentStart) == false)
            {
                lexer.Rollback();

                bool match = true;

                for (int i = 0; i < blockCommentStart.Length; i++)
                {
                    if (blockCommentStart[i] != lexer.ReadNext())
                    {
                        match = false;
                        break;
                    }
                }

                // Check for valid match
                if (match == true)
                {
                    // Read until end or closing block
                    while (IsEndLineOrString(lexer, blockCommentEnd) == false) ;

                    // Matched a multi-line block commend
                    return true;
                }
            }
            return false;
        }

        private bool IsEndLineOrEndFile(ILexer lexer, char character)
        {
            if (lexer.EndOfStream == true ||
                character == '\n' ||
                character == '\r')
            {
                // Line end or file end
                return true;
            }
            return false;
        }

        private bool IsEndLineOrString(ILexer lexer, string endString)
        {
            for (int i = 0; i < endString.Length; i++)
            {
                // Check for end of stream
                if (lexer.EndOfStream == true)
                    return true;

                // Check for matching end string
                if (endString[i] != lexer.ReadNext())
                    return false;
            }

            // We matched the end string
            return true;
        }
    }
}
