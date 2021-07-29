using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class Expression
{
    private string m_file;
    public string File { get { return m_file; } set { m_file = value; } }

    private string m_text;
    public string Text { get { return m_text; } set { m_text = value; } }

    private int m_line; 
    public int Line { get { return m_line; } set { m_line = value; } }

    private int m_column; 
    public int Column { get { return m_column; } set { m_column = value; } }
    
    private double m_trackPositionOffset;
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

    /// <summary>Separates an expression into it's consituent command and arguments</summary>
    /// <param name="command">The command</param>
    /// <param name="argumentSequence">The sequence of arguments contained within the expression</param>
    /// <param name="culture">The current culture</param>
    /// <param name="raiseErrors">Whether errors should be raised at this point</param>
    /// <param name="isRw">Whether this is a RW format file</param>
    /// <param name="currentSection">The current section being processed</param>
    public static void SeparateCommandsAndArguments(Expression expression, out string command, out string argumentSequence, System.Globalization.CultureInfo culture, bool raiseErrors, bool isRw, string currentSection)
    {
        bool openingerror = false, closingerror = false;
        int i, firstClosingBracket = 0;
        // if (Plugin.CurrentOptions.EnableBveTsHacks)
        // {
        // 	if (Text.StartsWith("Train. ", StringComparison.InvariantCultureIgnoreCase))
        // 	{
        // 		//HACK: Some Chinese routes seem to have used a space between Train. and the rest of the command
        // 		//e.g. Taipei Metro. BVE4/ 2 accept this......
        // 		Text = "Train." + Text.Substring(7, Text.Length - 7);
        // 	}
        // 	else if (Text.StartsWith("Texture. Background", StringComparison.InvariantCultureIgnoreCase))
        // 	{
        // 		//Same hack as above, found in Minobu route for BVE2
        // 		Text = "Texture.Background" + Text.Substring(19, Text.Length - 19);
        // 	}
        // 	else if (Text.EndsWith(")height(0)", StringComparison.InvariantCultureIgnoreCase))
        // 	{
        // 		//Heavy Coal original RW- Fix starting station
        // 		Text = Text.Substring(0, Text.Length - 9);
        // 	}

        // 	if (IsRw && CurrentSection.ToLowerInvariant() == "track")
        // 	{
        // 		//Removes misplaced track position indicies from the end of a command in the Track section
        // 		int idx = Text.LastIndexOf(')');
        // 		if (idx != -1 && idx != Text.Length)
        // 		{
        // 			// ReSharper disable once NotAccessedVariable
        // 			double d;
        // 			string s = this.Text.Substring(idx + 1, this.Text.Length - idx - 1).Trim();
        // 			if (NumberFormats.TryParseDoubleVb6(s, out d))
        // 			{
        // 				this.Text = this.Text.Substring(0, idx).Trim();
        // 			}
        // 		}
        // 	}

        // 	if (IsRw && this.Text.EndsWith("))"))
        // 	{
        // 		int openingBrackets = Text.Count(x => x == '(');
        // 		int closingBrackets = Text.Count(x => x == ')');
        // 		//Remove obviously wrong double-ending brackets
        // 		if (closingBrackets == openingBrackets + 1 && this.Text.EndsWith("))"))
        // 		{
        // 			this.Text = this.Text.Substring(0, this.Text.Length - 1);
        // 		}
        // 	}

        // 	if (Text.StartsWith("route.comment", StringComparison.InvariantCultureIgnoreCase) && Text.IndexOf("(C)", StringComparison.InvariantCultureIgnoreCase) != -1)
        // 	{
        // 		//Some BVE4 routes use this instead of the copyright symbol
        // 		Text = Text.Replace("(C)", "©");
        // 		Text = Text.Replace("(c)", "©");
        // 	}
        // }

        for (i = 0; i < expression.Text.Length; i++)
        {
            if (expression.Text[i] == '(')
            {
                bool found = false;
                bool stationName = false;
                bool replaced = false;
                i++;
                while (i < expression.Text.Length)
                {
                    if (expression.Text[i] == ',' || expression.Text[i] == ';')
                    {
                        //Only check parenthesis in the station name field- The comma and semi-colon are the argument separators
                        stationName = true;
                    }

                    if (expression.Text[i] == '(')
                    {
                        if (raiseErrors & !openingerror)
                        {
                            if (stationName)
                            {
                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid opening parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " +
                                                                                        expression.Column.ToString(culture) + " in file " + expression.File);
                                openingerror = true;
                            }
                            else
                            {
                                expression.Text = expression.Text.Remove(i, 1).Insert(i, "[");
                                replaced = true;
                            }
                        }
                    }
                    else if (expression.Text[i] == ')')
                    {
                        if (stationName == false && i != expression.Text.Length && replaced)
                        {
                            expression.Text = expression.Text.Remove(i, 1).Insert(i, "]");
                            continue;
                        }

                        found = true;
                        firstClosingBracket = i;
                        break;
                    }

                    i++;
                }

                if (!found)
                {
                    if (raiseErrors & !closingerror)
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
                        closingerror = true;
                    }

                    expression.Text += ")";
                }
            }
            else if (expression.Text[i] == ')')
            {
                if (raiseErrors & !closingerror)
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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

        if (firstClosingBracket != 0 && firstClosingBracket < expression.Text.Length - 1)
        {
            if (!Char.IsWhiteSpace(expression.Text[firstClosingBracket + 1]) && expression.Text[firstClosingBracket + 1] != '.' && expression.Text[firstClosingBracket + 1] != ';')
            {
                expression.Text = expression.Text.Insert(firstClosingBracket + 1, " ");
                i = firstClosingBracket;
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
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                            command = null;
                            argumentSequence = null;
                            for (int k = j + 1; k < expression.Text.Length; k++)
                            {
                                if (char.IsWhiteSpace(expression.Text[k]))
                                {
                                    command = expression.Text.Substring(0, k).TrimEnd();
                                    argumentSequence = expression.Text.Substring(k + 1).TrimStart();
                                    found = true;
                                    break;
                                }

                                if (expression.Text[k] == '(')
                                {
                                    command = expression.Text.Substring(0, k).TrimEnd();
                                    argumentSequence = expression.Text.Substring(k).TrimStart();
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                if (raiseErrors & !openingerror & !closingerror)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid syntax encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Missing closing parenthesis encountered at line " + expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Missing closing parenthesis encountered at line "+ expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Missing closing parenthesis encountered at line "+ expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid closing parenthesis encountered at line "+ expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
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
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid trailing semicolon encountered in " + command + " at line "+ expression.Line.ToString(culture) + ", column " + expression.Column.ToString(culture) + " in file " + expression.File);
            }

            while (command.EndsWith(";"))
            {
                command = command.Substring(0, command.Length - 1);
            }
        }
    }

    /// <summary>Converts a RW formatted expression to CSV format</summary>
    /// <param name="text">The expression</param>
    /// <param name="section">The current section</param>
    /// <param name="sectionAlwaysPrefix">Whether the section prefix should always be applied</param>
    /// <returns>Expression converted to CSV if possible - if not, the original expression is returned</returns>
    public static string ConvertRwToCsv(string text, string section, bool sectionAlwaysPrefix)
    {
        int indexOfEquals = text.IndexOf('=');
        if (indexOfEquals >= 0)
        {
            // handle RW cycle syntax
            string t = text.Substring(0, indexOfEquals);
            if (section.ToLowerInvariant() == "cycle" & sectionAlwaysPrefix)
            {
                double b;
                if (Conversions.TryParseDoubleVb6(t, out b))
                {
                    t = ".Ground(" + b + ")";
                }
            }
            else if (section.ToLowerInvariant() == "signal" & sectionAlwaysPrefix)
            {
                double b;
                if (Conversions.TryParseDoubleVb6(t, out b))
                {
                    t = ".Void(" + b + ")";
                }
            }

            // convert RW style into CSV style
            return t + " " + text.Substring(indexOfEquals + 1);
        }
        else
        {
            return text;
        }

    }

}
