using System.Text;

class Generator
{
    public static void GenerateSqlFile(int requestedLines)
    {
        string[] sampleBlock = new string[]
        {
            "-- Sample SQL file",
            "CREATE TABLE users (id INT PRIMARY KEY, name VARCHAR(100), email VARCHAR(100));",
            "INSERT INTO users (id, name, email) VALUES (1, 'Alice', 'alice@example.com');",
            "INSERT INTO users (id, name, email) VALUES (2, 'Bob', 'bob@example.com');",
            "UPDATE users SET email = 'alice@newdomain.com' WHERE id = 1;",
            "DELETE FROM users WHERE id = 2;",
            "SELECT * FROM users;",
            "-- Multiline string literal example",
            "INSERT INTO logs (message) VALUES ('This is a log message\nthat spans multiple lines\nand includes special characters: !@#$%^&*()');",
            "-- Complex query",
            "SELECT u.id, u.name, COUNT(o.id) as order_count FROM users u LEFT JOIN orders o ON u.id = o.user_id GROUP BY u.id, u.name;",
            "-- Transaction block",
            "BEGIN TRANSACTION;",
            "UPDATE users SET name = 'Charlie' WHERE id = 1;",
            "COMMIT;",
            "-- Function definition",
            "CREATE FUNCTION get_user_count() RETURNS INT AS $$ BEGIN RETURN (SELECT COUNT(*) FROM users); END; $$ LANGUAGE plpgsql;",
            "-- Multiline comment",
            "/*",
            "This is a multiline comment",
            "spanning several lines",
            "*/",
            // ... (add more varied SQL lines to reach 150 lines)
        };
        var sqlLines = new List<string>(sampleBlock);
        while (sqlLines.Count < 150)
        {
            sqlLines.Add($"-- filler line {sqlLines.Count + 1}");
        }
        var outputLines = new List<string>(requestedLines);
        int fullRepeats = requestedLines / sqlLines.Count;
        int remainder = requestedLines % sqlLines.Count;
        for (int i = 0; i < fullRepeats; i++)
            outputLines.AddRange(sqlLines);
        if (remainder > 0)
            outputLines.AddRange(sqlLines.GetRange(0, remainder));
        var expandedLines = new List<string>();
        foreach (var line in outputLines)
        {
            if (line.Contains("\n"))
                expandedLines.AddRange(line.Split('\n'));
            else
                expandedLines.Add(line);
        }
        if (expandedLines.Count > requestedLines)
            expandedLines = expandedLines.GetRange(0, requestedLines);
        else if (expandedLines.Count < requestedLines)
        {
            int i = 0;
            while (expandedLines.Count < requestedLines)
            {
                expandedLines.Add(sqlLines[i % sqlLines.Count]);
                i++;
            }
        }
        var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.sql");
        File.WriteAllLines(outputPath, expandedLines, Encoding.UTF8);
        Console.WriteLine($"Generated {outputPath} with {requestedLines} lines.");
    }
}