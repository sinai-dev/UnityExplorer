﻿namespace ExplorerBeta.UI.Main.Console.Lexer
{
    public enum SpecialCharacterPosition
    {
        Start,
        End,
    };

    public interface ILexer
    {
        bool EndOfStream { get; }
        char Previous { get; }

        char ReadNext();
        void Rollback(int amount = -1);
        void Commit();
        bool IsSpecialSymbol(char character, SpecialCharacterPosition position = SpecialCharacterPosition.Start);
    }
}