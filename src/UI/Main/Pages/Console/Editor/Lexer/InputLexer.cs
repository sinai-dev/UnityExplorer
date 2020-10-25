using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.UI.Main.Pages.Console.Lexer
{
    internal struct LexerMatchInfo
    {
        public int startIndex;
        public int endIndex;
        public string htmlColor;
    }

    internal class InputLexer : ILexer
    {
        private string inputString = null;
        private MatchLexer[] matchers = null;
        private readonly HashSet<char> specialStartSymbols = new HashSet<char>();
        private readonly HashSet<char> specialEndSymbols = new HashSet<char>();
        private char current = ' ';
        private char previous = ' ';
        private int currentIndex = 0;
        private int currentLookaheadIndex = 0;

        public bool EndOfStream
        {
            get { return currentLookaheadIndex >= inputString.Length; }
        }

        public char Previous
        {
            get { return previous; }
        }

        public void UseMatchers(char[] delimiters, MatchLexer[] matchers)
        {
            this.matchers = matchers;

            specialStartSymbols.Clear();
            specialEndSymbols.Clear();

            if (delimiters != null)
            {
                foreach (char character in delimiters)
                {
                    if (specialStartSymbols.Contains(character) == false)
                        specialStartSymbols.Add(character);

                    if (specialEndSymbols.Contains(character) == false)
                        specialEndSymbols.Add(character);
                }
            }

            if (matchers != null)
            {
                foreach (MatchLexer lexer in matchers)
                {
                    foreach (char special in lexer.StartChars)
                        if (specialStartSymbols.Contains(special) == false)
                            specialStartSymbols.Add(special);

                    foreach (char special in lexer.EndChars)
                        if (specialEndSymbols.Contains(special) == false)
                            specialEndSymbols.Add(special);
                }
            }
        }

        public IEnumerable<LexerMatchInfo> LexInputString(string input)
        {
            if (input == null || matchers == null || matchers.Length == 0)
                yield break;

            this.inputString = input;
            this.current = ' ';
            this.previous = ' ';
            this.currentIndex = 0;
            this.currentLookaheadIndex = 0;

            while (EndOfStream == false)
            {
                bool didMatchLexer = false;

                ReadWhiteSpace();

                foreach (MatchLexer matcher in matchers)
                {
                    int startIndex = currentIndex;

                    bool isMatched = matcher.IsMatch(this);

                    if (isMatched == true)
                    {
                        int endIndex = currentIndex;

                        didMatchLexer = true;

                        yield return new LexerMatchInfo
                        {
                            startIndex = startIndex,
                            endIndex = endIndex,
                            htmlColor = matcher.HexColor,
                        };

                        break;
                    }
                }

                if (didMatchLexer == false)
                {
                    ReadNext();
                    Commit();
                }
            }
        }

        public char ReadNext()
        {
            if (EndOfStream == true)
                return '\0';

            previous = current;

            current = inputString[currentLookaheadIndex];
            currentLookaheadIndex++;

            return current;
        }

        public void Rollback(int amount = -1)
        {
            if (amount == -1)
            {
                currentLookaheadIndex = currentIndex;
            }
            else
            {
                if (currentLookaheadIndex > currentIndex)
                    currentLookaheadIndex -= amount;
            }

            int previousIndex = currentLookaheadIndex - 1;

            if (previousIndex >= inputString.Length)
                previous = inputString[inputString.Length - 1];
            else if (previousIndex >= 0)
                previous = inputString[previousIndex];
            else
                previous = ' ';
        }

        public void Commit()
        {
            currentIndex = currentLookaheadIndex;
        }

        public bool IsSpecialSymbol(char character, SpecialCharacterPosition position = SpecialCharacterPosition.Start)
        {
            if (position == SpecialCharacterPosition.Start)
                return specialStartSymbols.Contains(character);

            return specialEndSymbols.Contains(character);
        }

        private void ReadWhiteSpace()
        {
            while (char.IsWhiteSpace(ReadNext()) == true)
            {
                Commit();
            }

            Rollback();
        }
    }
}