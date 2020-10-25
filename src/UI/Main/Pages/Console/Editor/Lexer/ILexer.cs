using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    // Types
    /// <summary>
    /// Represents a keyword position where a special character may appear.
    /// </summary>
    public enum SpecialCharacterPosition
    {
        /// <summary>
        /// The special character may appear before a keyword.
        /// </summary>
        Start,
        /// <summary>
        /// The special character may appear after a keyword.
        /// </summary>
        End,
    };

    /// <summary>
    /// Represents a streamable lexer input which can be examined by matchers.
    /// </summary>
    public interface ILexer
    {
        // Properties
        /// <summary>
        /// Returns true if there are no more characters to read.
        /// </summary>
        bool EndOfStream { get; }

        /// <summary>
        /// Returns the previously read character or '\0' if there is no previous character.
        /// </summary>
        char Previous { get; }

        // Methods
        /// <summary>
        /// Attempt to read the next character.
        /// </summary>
        /// <returns>The next character in the stream or '\0' if the end of stream is reached</returns>
        char ReadNext();

        /// <summary>
        /// Causes the lexer to return its state to a previously commited state.
        /// </summary>
        /// <param name="amount">Use -1 to return to the last commited state or a positive number to represent the number of characters to rollback</param>
        void Rollback(int amount = -1);

        /// <summary>
        /// Causes all read characters to be commited meaning that rollback will return to this lexer state.
        /// </summary>
        void Commit();

        /// <summary>
        /// Determines whether the specified character is considered a special symbol by the lexer meaning that it is able to act as a delimiter.
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <param name="position">The character position to check. This determines whether the character may appear at the start or end of a keyword</param>
        /// <returns>True if the character is a valid delimiter or false if not</returns>
        bool IsSpecialSymbol(char character, SpecialCharacterPosition position = SpecialCharacterPosition.Start);
    }
}
