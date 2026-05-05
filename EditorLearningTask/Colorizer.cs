using EditorLearningTask;

public class Colorizer
{
    public ConsoleColor GetColor(Token token)
    {
        if (SqlTokenTypes.IsReservedKeyword(token.Value))
            return ConsoleColor.Blue;
        if (SqlTokenTypes.IsKeyword(token.Value))
            return ConsoleColor.DarkBlue;
        if (token.Value == SqlTokenTypes.TOKEN_IDENTIFIER)
            return ConsoleColor.DarkYellow;
        if (token.Value == SqlTokenTypes.TOKEN_COMMENT)
            return ConsoleColor.DarkYellow;
        if (token.Value is SqlTokenTypes.TOKEN_STRING or >= SqlTokenTypes.TOKEN_NUMBER)
            return ConsoleColor.DarkRed;
        return ConsoleColor.White;
    }
}