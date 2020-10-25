using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Explorer.UI.Main.Pages.Console
{
    public class AutoIndent
    {
        // Enum
        public enum IndentMode
        {
            None,
            AutoTab,
            AutoTabContextual,
        }

        // Private
        private static readonly StringBuilder indentBuilder = new StringBuilder();

        private static string indentDecreaseString = null;

        // Public
        public static IndentMode autoIndentMode = IndentMode.AutoTabContextual;

        /// <summary>
        /// Should auto indent be used for this language.
        /// </summary>
        public static bool allowAutoIndent = true;
        /// <summary>
        /// The character that causes the indent level to increase.
        /// </summary>
        public static char indentIncreaseCharacter = '{';
        /// <summary>
        /// The character that causes the indent level to decrease.
        /// </summary>
        public static char indentDecreaseCharacter = '}';

        // Properties
        /// <summary>
        /// Get the string representation of the indent character.
        /// </summary>
        public static string IndentDecreaseString
        {
            get
            {
                if (indentDecreaseString == null)
                {
                    indentDecreaseString = new string(indentDecreaseCharacter, 1);
                }
                return indentDecreaseString;
            }
        }

        // Methods
        public static string GetAutoIndentedFormattedString(string indentSection, int currentIndent, out int caretPosition)
        {
            // Add indent level
            int indent = currentIndent + 1;

            // Append characters
            for (int i = 0; i < indentSection.Length; i++)
            {
                if (indentSection[i] == '\n')
                {
                    indentBuilder.Append('\n');
                    AppendIndentString(indent);
                }
                else if (indentSection[i] == '\t')
                {
                    // We will add tabs manually
                    continue;
                }
                else if (indentSection[i] == indentIncreaseCharacter)
                {
                    indentBuilder.Append(indentIncreaseCharacter);
                    indent++;
                }
                else if (indentSection[i] == indentDecreaseCharacter)
                {
                    indentBuilder.Append(indentDecreaseCharacter);
                    indent--;
                }
                else
                {
                    indentBuilder.Append(indentSection[i]);
                }
            }

            // Build the string
            string formattedSection = indentBuilder.ToString();
            indentBuilder.Length = 0;

            // Default caret position
            caretPosition = formattedSection.Length - 1;

            // Find the caret position
            for (int i = formattedSection.Length - 1; i >= 0; i--)
            {
                if (formattedSection[i] == '\n')
                    continue;

                caretPosition = i;
                break;
            }

            return formattedSection;
        }

        public static int GetAutoIndentLevel(string inputString, int startIndex, int endIndex)
        {
            int indent = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                if (inputString[i] == '\t')
                    indent++;

                // Check for end line or other characters
                if (inputString[i] == '\n' || inputString[i] != ' ')
                    break;
            }

            return indent;
        }

        private static void AppendIndentString(int amount)
        {
            for (int i = 0; i < amount; i++)
                indentBuilder.Append("\t");
        }
    }
}
