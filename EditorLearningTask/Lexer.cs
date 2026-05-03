using System.Text;

namespace EditorLearningTask
{
public class Lexer
{
    // Tokenize each line separately, but maintain state for multiline tokens
    public List<List<Token>> Tokenize(string[] lines)
    {
        var tokenLines = new List<List<Token>>();
        int lineEndState = 0;
        int multilineTokenStart = 0;

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var tokens = new List<Token>();
            tokenLines.Add(tokens);
            string line = lines[lineNum];
            int pos = 0;
            int length = line.Length;
            // If we are inside a multiline token, handle it specially
            if (lineEndState != 0)
            {
                if (lineEndState == SqlTokenTypes.TOKEN_COMMENT)
                {
                    int endIdx = line.IndexOf("*/", pos, StringComparison.Ordinal);
                    if (endIdx >= 0)
                    {
                        // End of multiline comment found on this line
                        tokens.Add(new Token(0, endIdx + 2, SqlTokenTypes.TOKEN_COMMENT, line.Substring(0, endIdx + 2)));
                        pos = endIdx + 2;
                        lineEndState = 0;
                        // Continue lexing the rest of the line after the comment
                        while (pos < length)
                        {
                            char c = line[pos];
                            if (char.IsWhiteSpace(c))
                            {
                                HandleWhitespace(line, ref pos, tokens);
                                continue;
                            }
                            if (c == '-' && pos + 1 < length && line[pos + 1] == '-')
                            {
                                HandleSingleLineComment(line, ref pos, tokens);
                                break;
                            }
                            if (c == '/' && pos + 1 < length && line[pos + 1] == '*')
                            {
                                // Start of another multiline comment
                                lineEndState = SqlTokenTypes.TOKEN_COMMENT;
                                tokens.Add(new Token(pos, length - pos, SqlTokenTypes.TOKEN_COMMENT, line.Substring(pos)));
                                pos = length;
                                break;
                            }
                            if (c == '\'')
                            {
                                if (HandleStringLiteral(line, ref pos, ref lineEndState, ref multilineTokenStart, tokens))
                                    break;
                                continue;
                            }
                            if (char.IsDigit(c))
                            {
                                HandleNumber(line, ref pos, tokens);
                                continue;
                            }
                            if (char.IsLetter(c) || c == '_')
                            {
                                HandleIdentifierOrKeyword(line, ref pos, tokens);
                                continue;
                            }
                            if (",;().=*<>!+-/".IndexOf(c) >= 0)
                            {
                                HandleSymbol(line, ref pos, tokens);
                                continue;
                            }
                            HandleUnknown(line, ref pos, tokens);
                        }
                    }
                    else
                    {
                        // Still inside multiline comment, emit the whole line as a comment token
                        tokens.Add(new Token(0, length, SqlTokenTypes.TOKEN_COMMENT, line));
                        // Remain in multiline comment state
                    }
                    continue;
                }
                else if (lineEndState == SqlTokenTypes.TOKEN_STRING)
                {
                    int start = 0;
                    bool closed = false;
                    StringBuilder sb = new();
                    while (start < length)
                    {
                        if (line[start] == '\'')
                        {
                            if (start + 1 < length && line[start + 1] == '\'')
                            {
                                sb.Append("''");
                                start += 2;
                                continue;
                            }
                            sb.Append("'");
                            start++;
                            closed = true;
                            break;
                        }
                        sb.Append(line[start]);
                        start++;
                    }
                    // Emit the string token for this line
                    tokens.Add(new Token(0, closed ? start : length, SqlTokenTypes.TOKEN_STRING, "'" + line.Substring(0, closed ? start : length)));
                    if (closed)
                    {
                        lineEndState = 0;
                        // Continue lexing the rest of the line after the string
                        pos = start;
                        while (pos < length)
                        {
                            char c = line[pos];
                            if (char.IsWhiteSpace(c))
                            {
                                HandleWhitespace(line, ref pos, tokens);
                                continue;
                            }
                            if (c == '-' && pos + 1 < length && line[pos + 1] == '-')
                            {
                                HandleSingleLineComment(line, ref pos, tokens);
                                break;
                            }
                            if (c == '/' && pos + 1 < length && line[pos + 1] == '*')
                            {
                                tokens.Add(new Token(pos, length - pos, SqlTokenTypes.TOKEN_COMMENT, line.Substring(pos)));
                                pos = length;
                                break;
                            }
                            if (c == '\'')
                            {
                                if (HandleStringLiteral(line, ref pos, ref lineEndState, ref multilineTokenStart, tokens))
                                    break;
                                continue;
                            }
                            if (char.IsDigit(c))
                            {
                                HandleNumber(line, ref pos, tokens);
                                continue;
                            }
                            if (char.IsLetter(c) || c == '_')
                            {
                                HandleIdentifierOrKeyword(line, ref pos, tokens);
                                continue;
                            }
                            if (",;().=*<>!+-/".IndexOf(c) >= 0)
                            {
                                HandleSymbol(line, ref pos, tokens);
                                continue;
                            }
                            HandleUnknown(line, ref pos, tokens);
                        }
                    }
                    // Otherwise, remain in multiline string state
                    continue;
                }
            }
            // Not in multiline state, normal lexing
            pos = 0;
            while (pos < length)
            {
                char c = line[pos];
                if (char.IsWhiteSpace(c))
                {
                    HandleWhitespace(line, ref pos, tokens);
                    continue;
                }
                if (c == '-' && pos + 1 < length && line[pos + 1] == '-')
                {
                    HandleSingleLineComment(line, ref pos, tokens);
                    break;
                }
                if (c == '/' && pos + 1 < length && line[pos + 1] == '*')
                {
                    int endIdx = line.IndexOf("*/", pos + 2, StringComparison.Ordinal);
                    if (endIdx >= 0)
                    {
                        tokens.Add(new Token(pos, endIdx + 2 - pos, SqlTokenTypes.TOKEN_COMMENT, line.Substring(pos, endIdx + 2 - pos)));
                        pos = endIdx + 2;
                        continue;
                    }
                    else
                    {
                        // Start of multiline comment
                        lineEndState = SqlTokenTypes.TOKEN_COMMENT;
                        tokens.Add(new Token(pos, length - pos, SqlTokenTypes.TOKEN_COMMENT, line.Substring(pos)));
                        pos = length;
                        break;
                    }
                }
                if (c == '\'')
                {
                    if (HandleStringLiteral(line, ref pos, ref lineEndState, ref multilineTokenStart, tokens))
                        break;
                    continue;
                }
                if (char.IsDigit(c))
                {
                    HandleNumber(line, ref pos, tokens);
                    continue;
                }
                if (char.IsLetter(c) || c == '_')
                {
                    HandleIdentifierOrKeyword(line, ref pos, tokens);
                    continue;
                }
                if (",;().=*<>!+-/".IndexOf(c) >= 0)
                {
                    HandleSymbol(line, ref pos, tokens);
                    continue;
                }
                HandleUnknown(line, ref pos, tokens);
            }
        }
        return tokenLines;
    }

    // No longer needed: HandleMultilineToken

    private void HandleWhitespace(string line, ref int pos, List<Token> tokens)
    {
        int wsStart = pos;
        int length = line.Length;
        while (pos < length && char.IsWhiteSpace(line[pos])) pos++;
        tokens.Add(new Token(wsStart, pos - wsStart, SqlTokenTypes.TOKEN_WHITESPACE, line.Substring(wsStart, pos - wsStart)));
    }

    private void HandleSingleLineComment(string line, ref int pos, List<Token> tokens)
    {
        int length = line.Length;
        tokens.Add(new Token(pos, length - pos, SqlTokenTypes.TOKEN_COMMENT, line.Substring(pos)));
        pos = length;
    }

    // No longer needed: HandleMultilineComment

    // Updated to not use multilineBuffer, and to set state for multiline string
    private bool HandleStringLiteral(string line, ref int pos, ref int lineEndState, ref int multilineTokenStart, List<Token> tokens)
    {
        int strStart = pos;
        int length = line.Length;
        pos++;
        bool closed = false;
        StringBuilder sb = new();
        sb.Append("'");
        while (pos < length)
        {
            if (line[pos] == '\'')
            {
                if (pos + 1 < length && line[pos + 1] == '\'')
                {
                    sb.Append("''");
                    pos += 2;
                    continue;
                }
                sb.Append("'");
                pos++;
                closed = true;
                break;
            }
            sb.Append(line[pos]);
            pos++;
        }
        if (closed)
        {
            tokens.Add(new Token(strStart, sb.Length, SqlTokenTypes.TOKEN_STRING, sb.ToString()));
            return false;
        }
        else
        {
            lineEndState = SqlTokenTypes.TOKEN_STRING;
            multilineTokenStart = strStart;
            // Emit the string so far as a token for this line
            tokens.Add(new Token(strStart, sb.Length, SqlTokenTypes.TOKEN_STRING, sb.ToString()));
            return true;
        }
    }

    private void HandleNumber(string line, ref int pos, List<Token> tokens)
    {
        int numStart = pos;
        int length = line.Length;
        while (pos < length && char.IsDigit(line[pos])) pos++;
        tokens.Add(new Token(numStart, pos - numStart, SqlTokenTypes.TOKEN_NUMBER, line.Substring(numStart, pos - numStart)));
    }

    private void HandleIdentifierOrKeyword(string line, ref int pos, List<Token> tokens)
    {
        int idStart = pos;
        int length = line.Length;
        while (pos < length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_')) pos++;
        string text = line.Substring(idStart, pos - idStart);
        int kwType = SqlTokenTypes.GetKeywordToken(text.ToUpperInvariant());
        if (kwType != -1)
            tokens.Add(new Token(idStart, pos - idStart, kwType, text));
        else
            tokens.Add(new Token(idStart, pos - idStart, SqlTokenTypes.TOKEN_IDENTIFIER, text));
    }

    private void HandleSymbol(string line, ref int pos, List<Token> tokens)
    {
        tokens.Add(new Token(pos, 1, SqlTokenTypes.TOKEN_SYMBOL, line.Substring(pos, 1)));
        pos++;
    }

    private void HandleUnknown(string line, ref int pos, List<Token> tokens)
    {
        tokens.Add(new Token(pos, 1, SqlTokenTypes.TOKEN_UNKNOWN, line.Substring(pos, 1)));
        pos++;
    }
}
}
