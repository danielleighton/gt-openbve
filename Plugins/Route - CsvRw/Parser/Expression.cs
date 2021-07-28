using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class Expression
{
    string m_file;
    public string File { get { return m_file; } set { m_file = value; } }

    private string m_text;
    public string Text { get { return m_text; } set { m_text = value; } }

    private int m_line; 
    public int Line { get { return m_line; } set { m_line = value; } }

    private int m_column; 
    public int Column { get { return m_column; } set { m_column = value; } }
    
    public double m_trackPositionOffset;
    public double TrackPositionOffset { get { return m_trackPositionOffset; } set { m_trackPositionOffset = value; } }

    public Expression(string file, string text, int line, int column, double trackPositionOffset)
    {
        m_file = file;
        m_text = text;
        m_line = line;
        m_column = column;
        m_trackPositionOffset = trackPositionOffset;
    }

    // TODO: Refactor as non-static
    /// <summary>
    /// Parse expressions from input lines
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="fileEncoding"></param>
    /// <param name="isRW"></param>
    /// <param name="lines"></param>
    /// <param name="allowRwRouteDescription"></param>
    /// <param name="trackPositionOffset"></param>
    /// <returns>List of expressions parsed from input lines</returns>
    public static List<Expression> PreprocessSplitIntoExpressions(string fileName, Encoding fileEncoding, bool isRW, string[] lines, bool allowRwRouteDescription, double trackPositionOffset)
    {
        List<Expression> expressionList = new List<Expression>();

        int e = 0;
        // full-line rw comments
        if (isRW)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int Level = 0;
                for (int j = 0; j < lines[i].Length; j++)
                {
                    switch (lines[i][j])
                    {
                        case '(':
                            Level++;
                            break;
                        case ')':
                            Level--;
                            break;
                        case ';':
                            if (Level == 0)
                            {
                                lines[i] = lines[i].Substring(0, j).TrimEnd();
                                j = lines[i].Length;
                            }
                            break;
                        case '=':
                            if (Level == 0)
                            {
                                j = lines[i].Length;
                            }
                            break;
                    }
                }
            }
        }
        
        // parse
        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            if (isRW & allowRwRouteDescription)
            {
                // TODO implement route comments
                // ignore rw route description
                if (
                    lines[lineNum].StartsWith("[", StringComparison.Ordinal) & lines[lineNum].IndexOf("]", StringComparison.Ordinal) > 0 |
                    lines[lineNum].StartsWith("$")
                )
                {
                    allowRwRouteDescription = false;
                    //Game.RouteComment = Game.RouteComment.Trim();
                }
                else
                {
                    //if (Game.RouteComment.Length != 0)
                    //{
                    //    Game.RouteComment += "\n";
                    //}
                    //Game.RouteComment += lines[i];
                    continue;
                }
            }
            {
                // count expressions
                int n = 0;
                int level = 0;
                for (int j = 0; j < lines[lineNum].Length; j++)
                {
                    switch (lines[lineNum][j])
                    {
                        case '(':
                            level++;
                            break;
                        case ')':
                            level--;
                            break;
                        case ',':
                            if (!isRW & level == 0) n++;
                            break;
                        case '@':
                            if (isRW & level == 0) n++;
                            break;
                    }
                }

                // create expressions
                level = 0;
                int a = 0, c = 0;
                for (int j = 0; j < lines[lineNum].Length; j++)
                {
                    switch (lines[lineNum][j])
                    {
                        case '(':
                            level++;
                            break;
                        case ')':
                            level--;
                            break;
                        case ',':
                        case '@':
                            if (level == 0 & !isRW)
                            {
                                string t = lines[lineNum].Substring(a, j - a).Trim();
                                if (t.Length > 0 && !t.StartsWith(";"))
                                {
                                    expressionList.Add(new Expression(fileName, t, lineNum + 1, c + 1, trackPositionOffset));
                                    e++;

                                }
                                a = j + 1;
                                c++;
                            }
                            break;
                        default:
                            continue;  
                    }
                }
                
                if (lines[lineNum].Length - a > 0)
                {
                    string t = lines[lineNum].Substring(a).Trim();
                    if (t.Length > 0 && !t.StartsWith(";"))
                    {
                        expressionList.Add(new Expression(fileName, t, lineNum + 1, c + 1, trackPositionOffset));
                        e++;
                    }
                }
            }
        }

        return expressionList;
    }

    public static void SeparateCommandsAndArguments(Expression expression, out string command, out string argumentSequence, System.Globalization.CultureInfo culture, string fileName, int lineNum, bool raiseErrors)
    {
        bool openingerror = false, closingerror = false;
        int i;
        for (i = 0; i < expression.Text.Length; i++)
        {
            if (expression.Text[i] == '(')
            {
                bool found = false;
                i++;
                while (i < expression.Text.Length)
                {
                    if (expression.Text[i] == '(')
                    {
                        if (raiseErrors & !openingerror)
                        {
                            GD.Print("Invalid opening parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                            openingerror = true;
                        }
                    }
                    else if (expression.Text[i] == ')')
                    {
                        found = true;
                        break;
                    }
                    i++;
                }
                if (!found)
                {
                    if (raiseErrors & !closingerror)
                    {
                        GD.Print("Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                        closingerror = true;
                    }
                    expression.Text += ")";
                }
            }
            else if (expression.Text[i] == ')')
            {
                if (raiseErrors & !closingerror)
                {
                    GD.Print("Invalid closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                    closingerror = true;
                }
            }
            else if (char.IsWhiteSpace(expression.Text[i]))
            {
                if (i >= expression.Text.Length - 1 || !char.IsWhiteSpace(expression.Text[i + 1]))
                {
                    break;
                }
            }
        }
        if (i < expression.Text.Length)
        {
            // white space was found outside of parentheses
            string a = expression.Text.Substring(0, i);
            if (a.IndexOf('(') >= 0 & a.IndexOf(')') >= 0)
            {
                // indices found not separated from the command by spaces
                command = expression.Text.Substring(0, i).TrimEnd();
                argumentSequence = expression.Text.Substring(i + 1).TrimStart();
                if (argumentSequence.StartsWith("(") & argumentSequence.EndsWith(")"))
                {
                    // arguments are enclosed by parentheses
                    argumentSequence = argumentSequence.Substring(1, argumentSequence.Length - 2).Trim();
                }
                else if (argumentSequence.StartsWith("("))
                {
                    // only opening parenthesis found
                    if (raiseErrors & !closingerror)
                    {
                        GD.Print("Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                    }
                    argumentSequence = argumentSequence.Substring(1).TrimStart();
                }
            }
            else
            {
                // no indices found before the space
                if (i < expression.Text.Length - 1 && expression.Text[i + 1] == '(')
                {
                    // opening parenthesis follows the space
                    int j = expression.Text.IndexOf(')', i + 1);
                    if (j > i + 1)
                    {
                        // closing parenthesis found
                        if (j == expression.Text.Length - 1)
                        {
                            // only closing parenthesis found at the end of the expression
                            command = expression.Text.Substring(0, i).TrimEnd();
                            argumentSequence = expression.Text.Substring(i + 2, j - i - 2).Trim();
                        }
                        else
                        {
                            // detect border between indices and arguments
                            bool found = false;
                            command = null; argumentSequence = null;
                            for (int k = j + 1; k < expression.Text.Length; k++)
                            {
                                if (char.IsWhiteSpace(expression.Text[k]))
                                {
                                    command = expression.Text.Substring(0, k).TrimEnd();
                                    argumentSequence = expression.Text.Substring(k + 1).TrimStart();
                                    found = true; break;
                                }
                                else if (expression.Text[k] == '(')
                                {
                                    command = expression.Text.Substring(0, k).TrimEnd();
                                    argumentSequence = expression.Text.Substring(k).TrimStart();
                                    found = true; break;
                                }
                            }
                            if (!found)
                            {
                                if (raiseErrors & !openingerror & !closingerror)
                                {
                                    GD.Print("Invalid syntax encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                                    openingerror = true;
                                    closingerror = true;
                                }
                                command = expression.Text;
                                argumentSequence = "";
                            }
                            if (argumentSequence.StartsWith("(") & argumentSequence.EndsWith(")"))
                            {
                                // arguments are enclosed by parentheses
                                argumentSequence = argumentSequence.Substring(1, argumentSequence.Length - 2).Trim();
                            }
                            else if (argumentSequence.StartsWith("("))
                            {
                                // only opening parenthesis found
                                if (raiseErrors & !closingerror)
                                {
                                    GD.Print("Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                                }
                                argumentSequence = argumentSequence.Substring(1).TrimStart();
                            }
                        }
                    }
                    else
                    {
                        // no closing parenthesis found
                        if (raiseErrors & !closingerror)
                        {
                            GD.Print("Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                        }
                        command = expression.Text.Substring(0, i).TrimEnd();
                        argumentSequence = expression.Text.Substring(i + 2).TrimStart();
                    }
                }
                else
                {
                    // no index possible
                    command = expression.Text.Substring(0, i).TrimEnd();
                    argumentSequence = expression.Text.Substring(i + 1).TrimStart();
                    if (argumentSequence.StartsWith("(") & argumentSequence.EndsWith(")"))
                    {
                        // arguments are enclosed by parentheses
                        argumentSequence = argumentSequence.Substring(1, argumentSequence.Length - 2).Trim();
                    }
                    else if (argumentSequence.StartsWith("("))
                    {
                        // only opening parenthesis found
                        if (raiseErrors & !closingerror)
                        {
                            GD.Print("Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                        }
                        argumentSequence = argumentSequence.Substring(1).TrimStart();
                    }
                }
            }
        }
        else
        {
            // no single space found
            if (expression.Text.EndsWith(")"))
            {
                i = expression.Text.LastIndexOf('(');
                if (i >= 0)
                {
                    command = expression.Text.Substring(0, i).TrimEnd();
                    argumentSequence = expression.Text.Substring(i + 1, expression.Text.Length - i - 2).Trim();
                }
                else
                {
                    command = expression.Text;
                    argumentSequence = "";
                }
            }
            else
            {
                i = expression.Text.IndexOf('(');
                if (i >= 0)
                {
                    if (raiseErrors & !closingerror)
                    {
                        GD.Print("Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                    }
                    command = expression.Text.Substring(0, i).TrimEnd();
                    argumentSequence = expression.Text.Substring(i + 1).TrimStart();
                }
                else
                {
                    if (raiseErrors)
                    {
                        i = expression.Text.IndexOf(')');
                        if (i >= 0 & !closingerror)
                        {
                            GD.Print("Invalid closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                        }
                    }
                    command = expression.Text;
                    argumentSequence = "";
                }
            }
        }
        // invalid trailing characters
        if (command.EndsWith(";"))
        {
            if (raiseErrors)
            {
                GD.Print("Invalid trailing semicolon encountered in " + command + " at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
            }
            while (command.EndsWith(";"))
            {
                command = command.Substring(0, command.Length - 1);
            }
        }
    }
}
