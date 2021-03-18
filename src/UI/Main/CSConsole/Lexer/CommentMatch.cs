using System.Collections.Generic;
using UnityEngine;

namespace UnityExplorer.UI.Main.CSConsole.Lexer
{
    public class CommentMatch : Matcher
    {
        public string lineCommentStart = @"//";
        public string blockCommentStart = @"/*";
        public string blockCommentEnd = @"*/";

        public override Color HighlightColor => new Color(0.34f, 0.65f, 0.29f, 1.0f);
        public override IEnumerable<char> StartChars => new char[] { lineCommentStart[0], blockCommentStart[0] };
        public override IEnumerable<char> EndChars => new char[] { blockCommentEnd[0] };
        public override bool IsImplicitMatch(CSLexerHighlighter lexer) => IsMatch(lexer, lineCommentStart) || IsMatch(lexer, blockCommentStart);

        private bool IsMatch(CSLexerHighlighter lexer, string commentType)
        {
            if (!string.IsNullOrEmpty(commentType))
            {
                lexer.Rollback();

                bool match = true;
                for (int i = 0; i < commentType.Length; i++)
                {
                    if (commentType[i] != lexer.ReadNext())
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    // Read until end of line or file
                    while (!IsEndLineOrEndFile(lexer, lexer.ReadNext())) { }

                    return true;
                }
            }
            return false;
        }

        private bool IsEndLineOrEndFile(CSLexerHighlighter lexer, char character) => lexer.EndOfStream || character == '\n' || character == '\r';
    }
}
