using EditorLearningTask;

public class Colorizer
{
    public ConsoleColor GetColor(int tokenValue)
    {
        if (SqlTokenTypes.IsReservedKeyword(tokenValue))
            return ConsoleColor.Blue;
        if (SqlTokenTypes.IsKeyword(tokenValue))
            return ConsoleColor.DarkBlue;
        if (tokenValue == SqlTokenTypes.TOKEN_IDENTIFIER)
            return ConsoleColor.DarkYellow;
        if (tokenValue == SqlTokenTypes.TOKEN_COMMENT)
            return ConsoleColor.DarkYellow;
        if (tokenValue is SqlTokenTypes.TOKEN_STRING or >= SqlTokenTypes.TOKEN_NUMBER)
            return ConsoleColor.DarkRed;
        return ConsoleColor.White;
    }
}