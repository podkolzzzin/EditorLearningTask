namespace EditorLearningTask
{
    public static class SqlTokenTypes
    {
        // Token type constants
        public const int TOKEN_IDENTIFIER = 1;
        public const int TOKEN_STRING = 2;
        public const int TOKEN_COMMENT = 3;
        public const int TOKEN_WHITESPACE = 4;
        public const int TOKEN_SYMBOL = 5;
        public const int TOKEN_NUMBER = 6;
        public const int TOKEN_UNKNOWN = 7;

        // Keyword constants (start from 100)
        public const int TOKEN_SELECT = 100;
        public const int TOKEN_FROM = 101;
        public const int TOKEN_WHERE = 102;
        public const int TOKEN_INSERT = 103;
        public const int TOKEN_INTO = 104;
        public const int TOKEN_VALUES = 105;
        public const int TOKEN_UPDATE = 106;
        public const int TOKEN_SET = 107;
        public const int TOKEN_DELETE = 108;
        public const int TOKEN_CREATE = 109;
        public const int TOKEN_TABLE = 110;
        public const int TOKEN_PRIMARY = 111;
        public const int TOKEN_KEY = 112;
        public const int TOKEN_INT = 113;
        public const int TOKEN_VARCHAR = 114;
        public const int TOKEN_AS = 115;
        public const int TOKEN_BEGIN = 116;
        public const int TOKEN_END = 117;
        public const int TOKEN_COMMIT = 118;
        public const int TOKEN_TRANSACTION = 119;
        public const int TOKEN_FUNCTION = 120;
        public const int TOKEN_RETURNS = 121;
        public const int TOKEN_LANGUAGE = 122;
        public const int TOKEN_GROUP = 123;
        public const int TOKEN_BY = 124;
        public const int TOKEN_LEFT = 125;
        public const int TOKEN_JOIN = 126;
        public const int TOKEN_ON = 127;

        // Non-reserved keyword constants (start from 200)
        public const int TOKEN_ORDER = 200;
        public const int TOKEN_DESC = 201;
        public const int TOKEN_ASC = 202;
        public const int TOKEN_LIMIT = 203;
        public const int TOKEN_OFFSET = 204;
        public const int TOKEN_HAVING = 205;
        public const int TOKEN_DISTINCT = 206;
        public const int TOKEN_UNION = 207;
        public const int TOKEN_ALL = 208;
        public const int TOKEN_INNER = 209;
        public const int TOKEN_OUTER = 210;
        public const int TOKEN_RIGHT = 211;
        public const int TOKEN_CROSS = 212;
        public const int TOKEN_EXISTS = 213;
        public const int TOKEN_LIKE = 214;
        public const int TOKEN_IS = 215;
        public const int TOKEN_NULL = 216;
        public const int TOKEN_TRUE = 217;
        public const int TOKEN_FALSE = 218;
        public const int TOKEN_CASE = 219;
        public const int TOKEN_WHEN = 220;
        public const int TOKEN_THEN = 221;
        public const int TOKEN_ELSE = 222;
        public const int TOKEN_CAST = 223;
        public const int TOKEN_COALESCE = 224;

        private static readonly Dictionary<string, int> KeywordMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "SELECT", TOKEN_SELECT },
            { "FROM", TOKEN_FROM },
            { "WHERE", TOKEN_WHERE },
            { "INSERT", TOKEN_INSERT },
            { "INTO", TOKEN_INTO },
            { "VALUES", TOKEN_VALUES },
            { "UPDATE", TOKEN_UPDATE },
            { "SET", TOKEN_SET },
            { "DELETE", TOKEN_DELETE },
            { "CREATE", TOKEN_CREATE },
            { "TABLE", TOKEN_TABLE },
            { "PRIMARY", TOKEN_PRIMARY },
            { "KEY", TOKEN_KEY },
            { "INT", TOKEN_INT },
            { "VARCHAR", TOKEN_VARCHAR },
            { "AS", TOKEN_AS },
            { "BEGIN", TOKEN_BEGIN },
            { "END", TOKEN_END },
            { "COMMIT", TOKEN_COMMIT },
            { "TRANSACTION", TOKEN_TRANSACTION },
            { "FUNCTION", TOKEN_FUNCTION },
            { "RETURNS", TOKEN_RETURNS },
            { "LANGUAGE", TOKEN_LANGUAGE },
            { "GROUP", TOKEN_GROUP },
            { "BY", TOKEN_BY },
            { "LEFT", TOKEN_LEFT },
            { "JOIN", TOKEN_JOIN },
            { "ON", TOKEN_ON }
        };

        // Static constructor to add non-reserved keywords
        static SqlTokenTypes()
        {
            KeywordMap["ORDER"] = TOKEN_ORDER;
            KeywordMap["DESC"] = TOKEN_DESC;
            KeywordMap["ASC"] = TOKEN_ASC;
            KeywordMap["LIMIT"] = TOKEN_LIMIT;
            KeywordMap["OFFSET"] = TOKEN_OFFSET;
            KeywordMap["HAVING"] = TOKEN_HAVING;
            KeywordMap["DISTINCT"] = TOKEN_DISTINCT;
            KeywordMap["UNION"] = TOKEN_UNION;
            KeywordMap["ALL"] = TOKEN_ALL;
            KeywordMap["INNER"] = TOKEN_INNER;
            KeywordMap["OUTER"] = TOKEN_OUTER;
            KeywordMap["RIGHT"] = TOKEN_RIGHT;
            KeywordMap["CROSS"] = TOKEN_CROSS;
            KeywordMap["EXISTS"] = TOKEN_EXISTS;
            KeywordMap["LIKE"] = TOKEN_LIKE;
            KeywordMap["IS"] = TOKEN_IS;
            KeywordMap["NULL"] = TOKEN_NULL;
            KeywordMap["TRUE"] = TOKEN_TRUE;
            KeywordMap["FALSE"] = TOKEN_FALSE;
            KeywordMap["CASE"] = TOKEN_CASE;
            KeywordMap["WHEN"] = TOKEN_WHEN;
            KeywordMap["THEN"] = TOKEN_THEN;
            KeywordMap["ELSE"] = TOKEN_ELSE;
            KeywordMap["CAST"] = TOKEN_CAST;
            KeywordMap["COALESCE"] = TOKEN_COALESCE;
        }

        public static int GetKeywordToken(string word) => KeywordMap.GetValueOrDefault(word, -1);

        public static bool IsReservedKeyword(int tokenValue) => tokenValue is >= TOKEN_SELECT and <= TOKEN_ON;

        public static bool IsKeyword(int tokenValue) => KeywordMap.ContainsValue(tokenValue);
    }
}