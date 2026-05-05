namespace EditorLearningTask
{
public class Lexer
{
    private int _lineEndState;

    public void Reset() => _lineEndState = 0;

    public List<List<Token>> Tokenize(string[] lines)
    {
        Reset();
        var result = new List<List<Token>>(lines.Length);
        foreach (var line in lines)
        {
            var tokens = new List<Token>();
            int pos = 0;
            int start = pos;
            while (NextToken(line, ref pos, out int type))
            {
                tokens.Add(new Token(start, pos - start, type, line.Substring(start, pos - start)));
                start = pos;
            }
            result.Add(tokens);
        }
        return result;
    }

    // Advances pos and returns the next token's type via out parameter.
    // Returns false when the line is exhausted. Multiline string/comment state
    // is carried across calls; call Reset() before starting a new file.
    public bool NextToken(string line, ref int pos, out int token)
    {
        int length = line.Length;

        if (pos == 0 && _lineEndState == SqlTokenTypes.TOKEN_COMMENT)
        {
            int endIdx = line.IndexOf("*/", 0, StringComparison.Ordinal);
            if (endIdx >= 0)
            {
                pos = endIdx + 2;
                _lineEndState = 0;
            }
            else
            {
                pos = length;
            }
            token = SqlTokenTypes.TOKEN_COMMENT;
            return true;
        }
        if (pos == 0 && _lineEndState == SqlTokenTypes.TOKEN_STRING)
        {
            int p = 0;
            bool closed = false;
            while (p < length)
            {
                if (line[p] == '\'')
                {
                    if (p + 1 < length && line[p + 1] == '\'')
                    {
                        p += 2;
                        continue;
                    }
                    p++;
                    closed = true;
                    break;
                }
                p++;
            }
            pos = closed ? p : length;
            if (closed) _lineEndState = 0;
            token = SqlTokenTypes.TOKEN_STRING;
            return true;
        }

        if (pos >= length)
        {
            token = 0;
            return false;
        }

        char c = line[pos];

        if (char.IsWhiteSpace(c))
        {
            while (pos < length && char.IsWhiteSpace(line[pos])) pos++;
            token = SqlTokenTypes.TOKEN_WHITESPACE;
            return true;
        }
        if (c == '-' && pos + 1 < length && line[pos + 1] == '-')
        {
            pos = length;
            token = SqlTokenTypes.TOKEN_COMMENT;
            return true;
        }
        if (c == '/' && pos + 1 < length && line[pos + 1] == '*')
        {
            int endIdx = line.IndexOf("*/", pos + 2, StringComparison.Ordinal);
            if (endIdx >= 0)
            {
                pos = endIdx + 2;
            }
            else
            {
                _lineEndState = SqlTokenTypes.TOKEN_COMMENT;
                pos = length;
            }
            token = SqlTokenTypes.TOKEN_COMMENT;
            return true;
        }
        if (c == '\'')
        {
            pos++;
            bool closed = false;
            while (pos < length)
            {
                if (line[pos] == '\'')
                {
                    if (pos + 1 < length && line[pos + 1] == '\'')
                    {
                        pos += 2;
                        continue;
                    }
                    pos++;
                    closed = true;
                    break;
                }
                pos++;
            }
            if (!closed) _lineEndState = SqlTokenTypes.TOKEN_STRING;
            token = SqlTokenTypes.TOKEN_STRING;
            return true;
        }
        if (char.IsDigit(c))
        {
            while (pos < length && char.IsDigit(line[pos])) pos++;
            token = SqlTokenTypes.TOKEN_NUMBER;
            return true;
        }
        if (char.IsLetter(c) || c == '_')
        {
            int s = pos;
            while (pos < length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_')) pos++;
            int kwType = SqlTokenTypes.GetKeywordToken(line.Substring(s, pos - s).ToUpperInvariant());
            token = kwType != -1 ? kwType : SqlTokenTypes.TOKEN_IDENTIFIER;
            return true;
        }
        if (",;().=*<>!+-/".IndexOf(c) >= 0)
        {
            pos++;
            token = SqlTokenTypes.TOKEN_SYMBOL;
            return true;
        }
        pos++;
        token = SqlTokenTypes.TOKEN_UNKNOWN;
        return true;
    }
}
}
