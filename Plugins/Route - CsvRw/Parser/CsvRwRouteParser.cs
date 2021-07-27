using Godot;
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

public static class CsvRwRouteParser
{
    #region Preprocessing (CsvRwRouteParser.Preprocess.cs)

    /// <summary>
    ///
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="fileEncoding"></param>
    /// <param name="isRW"></param>
    /// <param name="lines"></param>
    /// <param name="allowRwRouteDescription"></param>
    /// <param name="trackPositionOffset"></param>
    /// <returns></returns>
    private static List<Expression> PreprocessSplitIntoExpressions(string fileName, Encoding fileEncoding, bool isRW, string[] lines, bool allowRwRouteDescription, double trackPositionOffset)
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
        for (int i = 0; i < lines.Length; i++)
        {
            if (isRW & allowRwRouteDescription)
            {
                // ignore rw route description
                if (
                    lines[i].StartsWith("[", StringComparison.Ordinal) & lines[i].IndexOf("]", StringComparison.Ordinal) > 0 |
                    lines[i].StartsWith("$")
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
                for (int j = 0; j < lines[i].Length; j++)
                {
                    switch (lines[i][j])
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
                for (int j = 0; j < lines[i].Length; j++)
                {
                    switch (lines[i][j])
                    {
                        case '(':
                            level++;
                            break;
                        case ')':
                            level--;
                            break;
                        case ',':
                            if (level == 0 & !isRW)
                            {
                                string t = lines[i].Substring(a, j - a).Trim();
                                if (t.Length > 0 && !t.StartsWith(";"))
                                {
                                    Expression exp = new Expression();

                                    exp.file = fileName;
                                    exp.text = t;
                                    exp.line = i + 1;
                                    exp.column = c + 1;
                                    exp.trackPositionOffset = trackPositionOffset;
                                    expressionList.Add(exp);
                                    e++;

                                }
                                a = j + 1;
                                c++;
                            }
                            break;
                        case '@':
                            if (level == 0 & isRW)
                            {
                                string t = lines[i].Substring(a, j - a).Trim();
                                if (t.Length > 0 && !t.StartsWith(";"))
                                {
                                    Expression exp = new Expression();
                                    exp.file = fileName;
                                    exp.text = t;
                                    exp.line = i + 1;
                                    exp.column = c + 1;
                                    exp.trackPositionOffset = trackPositionOffset;
                                    expressionList.Add(exp);
                                    e++;
                                }
                                a = j + 1;
                                c++;
                            }
                            break;
                    }
                }
                if (lines[i].Length - a > 0)
                {
                    string t = lines[i].Substring(a).Trim();
                    if (t.Length > 0 && !t.StartsWith(";"))
                    {
                        Expression exp = new Expression();
                        exp.file = fileName;
                        exp.text = t;
                        exp.line = i + 1;
                        exp.column = c + 1;
                        exp.trackPositionOffset = trackPositionOffset;
                        expressionList.Add(exp);
                        e++;
                    }
                }
            }
        }

        return expressionList;
    }

    // preprocess chrrndsub
    private static void PreprocessChrRndSub(string fileName, Encoding fileEncoding, bool isRW, ref Expression[] expressions)
    {
        System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
        System.Text.Encoding Encoding = new System.Text.ASCIIEncoding();
        string[] Subs = new string[16];
        int openIfs = 0;
        for (int i = 0; i < expressions.Length; i++)
        {
            string Epilog = " at line " + expressions[i].line.ToString(Culture) + ", column " + expressions[i].column.ToString(Culture) + " in file " + expressions[i].file;
            bool continueWithNextExpression = false;
            for (int j = expressions[i].text.Length - 1; j >= 0; j--)
            {
                if (expressions[i].text[j] == '$')
                {
                    int k;
                    for (k = j + 1; k < expressions[i].text.Length; k++)
                    {
                        if (expressions[i].text[k] == '(')
                        {
                            break;
                        }
                        else if (expressions[i].text[k] == '/' | expressions[i].text[k] == '\\')
                        {
                            k = expressions[i].text.Length + 1;
                            break;
                        }
                    }
                    if (k <= expressions[i].text.Length)
                    {
                        string t = expressions[i].text.Substring(j, k - j).TrimEnd();
                        int l = 1, h;
                        for (h = k + 1; h < expressions[i].text.Length; h++)
                        {
                            switch (expressions[i].text[h])
                            {
                                case '(':
                                    l++;
                                    break;
                                case ')':
                                    l--;
                                    if (l < 0)
                                    {
                                        continueWithNextExpression = true;
                                        GD.Print("Invalid parenthesis structure in " + t + Epilog);
                                    }
                                    break;
                            }
                            if (l <= 0)
                            {
                                break;
                            }
                        }
                        if (continueWithNextExpression)
                        {
                            break;
                        }
                        if (l != 0)
                        {
                            GD.Print("Invalid parenthesis structure in " + t + Epilog);
                            continueWithNextExpression = true;
                            break;
                        }
                        string s = expressions[i].text.Substring(k + 1, h - k - 1).Trim();
                        switch (t.ToLowerInvariant())
                        {
                            case "$if":
                                if (j != 0)
                                {
                                    GD.Print("The $If directive must not appear within another statement" + Epilog);
                                }
                                else
                                {
                                    double num;
                                    if (double.TryParse(s, System.Globalization.NumberStyles.Float, Culture, out num))
                                    {
                                        openIfs++;
                                        expressions[i].text = string.Empty;
                                        if (num == 0.0)
                                        {
                                            // Blank every expression until the matching $Else or $EndIf
                                            i++;
                                            int level = 1;
                                            while (i < expressions.Length)
                                            {
                                                if (expressions[i].text.StartsWith("$if", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    expressions[i].text = string.Empty;
                                                    level++;
                                                }
                                                else if (expressions[i].text.StartsWith("$else", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    expressions[i].text = string.Empty;
                                                    if (level == 1)
                                                    {
                                                        level--;
                                                        break;
                                                    }
                                                }
                                                else if (expressions[i].text.StartsWith("$endif", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    expressions[i].text = string.Empty;
                                                    level--;
                                                    if (level == 0)
                                                    {
                                                        openIfs--;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    expressions[i].text = string.Empty;
                                                }
                                                i++;
                                            }
                                            if (level != 0)
                                            {
                                                GD.Print("$EndIf missing at the end of the file" + Epilog);
                                            }
                                        }
                                        continueWithNextExpression = true;
                                        break;
                                    }
                                    else
                                    {
                                        GD.Print("The $If condition does not evaluate to a number" + Epilog);
                                    }
                                }
                                continueWithNextExpression = true;
                                break;
                            case "$else":

                                // Blank every expression until the matching $EndIf

                                expressions[i].text = string.Empty;
                                if (openIfs != 0)
                                {
                                    i++;
                                    int level = 1;
                                    while (i < expressions.Length)
                                    {
                                        if (expressions[i].text.StartsWith("$if", StringComparison.OrdinalIgnoreCase))
                                        {
                                            expressions[i].text = string.Empty;
                                            level++;
                                        }
                                        else if (expressions[i].text.StartsWith("$else", StringComparison.OrdinalIgnoreCase))
                                        {
                                            expressions[i].text = string.Empty;
                                            if (level == 1)
                                            {
                                                GD.Print("Duplicate $Else encountered" + Epilog);
                                            }
                                        }
                                        else if (expressions[i].text.StartsWith("$endif", StringComparison.OrdinalIgnoreCase))
                                        {
                                            expressions[i].text = string.Empty;
                                            level--;
                                            if (level == 0)
                                            {
                                                openIfs--;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            expressions[i].text = string.Empty;
                                        }
                                        i++;
                                    }
                                    if (level != 0)
                                    {
                                        GD.Print("$EndIf missing at the end of the file" + Epilog);
                                    }
                                }
                                else
                                {
                                    GD.Print("$Else without matching $If encountered" + Epilog);
                                }
                                continueWithNextExpression = true;
                                break;
                            case "$endif":
                                expressions[i].text = string.Empty;
                                if (openIfs != 0)
                                {
                                    openIfs--;
                                }
                                else
                                {
                                    GD.Print("$EndIf without matching $If encountered" + Epilog);
                                }
                                continueWithNextExpression = true;
                                break;
                            case "$include":
                                if (j != 0)
                                {
                                    GD.Print("The $Include directive must not appear within another statement" + Epilog);
                                    continueWithNextExpression = true;
                                    break;
                                }
                                else
                                {
                                    string[] args = s.Split(';');
                                    for (int ia = 0; ia < args.Length; ia++)
                                    {
                                        args[ia] = args[ia].Trim();
                                    }
                                    int count = (args.Length + 1) / 2;
                                    string[] files = new string[count];
                                    double[] weights = new double[count];
                                    double[] offsets = new double[count];
                                    double weightsTotal = 0.0;
                                    for (int ia = 0; ia < count; ia++)
                                    {
                                        string includeFile;
                                        double offset;
                                        int colon = args[2 * ia].IndexOf(':');
                                        if (colon >= 0)
                                        {
                                            includeFile = args[2 * ia].Substring(0, colon).TrimEnd();
                                            string value = args[2 * ia].Substring(colon + 1).TrimStart();
                                            
                                            if (!double.TryParse(value, NumberStyles.Float, Culture, out offset))
                                            {
                                                continueWithNextExpression = true;
                                                GD.Print("The track position offset " + value + " is invalid in " + t + Epilog);
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            includeFile = args[2 * ia];
                                            offset = 0.0;
                                        }

                                        files[ia] = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fileName), includeFile);
                                        offsets[ia] = offset;
                                        if (!System.IO.File.Exists(files[ia]))
                                        {
                                            continueWithNextExpression = true;
                                            GD.Print("The file " + includeFile + " could not be found in " + t + Epilog);
                                            break;
                                        }
                                        else if (2 * ia + 1 < args.Length)
                                        {
                                            if (!Conversions.TryParseDoubleVb6(args[2 * ia + 1], out weights[ia]))
                                            {
                                                continueWithNextExpression = true;
                                                GD.Print("A weight is invalid in " + t + Epilog);
                                                break;
                                            }
                                            else if (weights[ia] <= 0.0)
                                            {
                                                continueWithNextExpression = true;
                                                GD.Print("A weight is not positive in " + t + Epilog);
                                                break;
                                            }
                                            else
                                            {
                                                weightsTotal += weights[ia];
                                            }
                                        }
                                        else
                                        {
                                            weights[ia] = 1.0;
                                            weightsTotal += 1.0;
                                        }
                                    }
                                    if (count == 0)
                                    {
                                        continueWithNextExpression = true;
                                        GD.Print("No file was specified in " + t + Epilog);
                                        break;
                                    }
                                    else if (!continueWithNextExpression)
                                    {
                                        double number = GD.Randf();

                                        double value = 0.0;
                                        int chosenIndex = 0;
                                        for (int ia = 0; ia < count; ia++)
                                        {
                                            value += weights[ia];
                                            if (value > number)
                                            {
                                                chosenIndex = ia;
                                                break;
                                            }
                                        }

                                        string[] lines = System.IO.File.ReadAllLines(files[chosenIndex], Encoding);


                                        Expression[] expr = PreprocessSplitIntoExpressions(files[chosenIndex], fileEncoding, isRW, lines, false, offsets[chosenIndex] + expressions[i].trackPositionOffset).ToArray();

                                        int length = expressions.Length;
                                        if (expr.Length == 0)
                                        {
                                            for (int ia = i; ia < expressions.Length - 1; ia++)
                                            {
                                                expressions[ia] = expressions[ia + 1];
                                            }
                                            Array.Resize<Expression>(ref expressions, length - 1);
                                        }
                                        else
                                        {
                                            Array.Resize<Expression>(ref expressions, length + expr.Length - 1);
                                            for (int ia = expressions.Length - 1; ia >= i + expr.Length; ia--)
                                            {
                                                expressions[ia] = expressions[ia - expr.Length + 1];
                                            }
                                            for (int ia = 0; ia < expr.Length; ia++)
                                            {
                                                expressions[i + ia] = expr[ia];
                                            }
                                        }
                                        i--;
                                        continueWithNextExpression = true;
                                    }
                                }
                                break;
                            case "$chr":
                                {
                                    int x;
                                    if (Conversions.TryParseIntVb6(s, out x))
                                    {
                                        if (x > 0 & x < 128)
                                        {
                                            expressions[i].text = expressions[i].text.Substring(0, j) + new string(Encoding.GetChars(new byte[] { (byte)x })) + expressions[i].text.Substring(h + 1);
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            GD.Print("Index does not correspond to a valid ASCII character in " + t + Epilog);
                                        }
                                    }
                                    else
                                    {
                                        continueWithNextExpression = true;
                                        GD.Print("Index is invalid in " + t + Epilog);
                                    }
                                }
                                break;
                            case "$rnd":
                                {
                                    int m = s.IndexOf(";", StringComparison.Ordinal);
                                    if (m >= 0)
                                    {
                                        string s1 = s.Substring(0, m).TrimEnd();
                                        string s2 = s.Substring(m + 1).TrimStart();
                                        int x; if (Conversions.TryParseIntVb6(s1, out x))
                                        {
                                            int y; if (Conversions.TryParseIntVb6(s2, out y))
                                            {
                                                int z = x + (int)Math.Floor(GD.Randf() * (double)(y - x + 1));
                                                expressions[i].text = expressions[i].text.Substring(0, j) + z.ToString(Culture) + expressions[i].text.Substring(h + 1);
                                            }
                                            else
                                            {
                                                continueWithNextExpression = true;
                                                GD.Print("Index2 is invalid in " + t + Epilog);
                                            }
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            GD.Print("Index1 is invalid in " + t + Epilog);
                                        }
                                    }
                                    else
                                    {
                                        continueWithNextExpression = true;
                                        GD.Print("Two arguments are expected in " + t + Epilog);
                                    }
                                }
                                break;
                            case "$sub":
                                {
                                    l = 0;
                                    bool f = false;
                                    int m;
                                    for (m = h + 1; m < expressions[i].text.Length; m++)
                                    {
                                        switch (expressions[i].text[m])
                                        {
                                            case '(': l++; break;
                                            case ')': l--; break;
                                            case '=':
                                                if (l == 0)
                                                {
                                                    f = true;
                                                }
                                                break;
                                            default:
                                                if (!char.IsWhiteSpace(expressions[i].text[m])) l = -1;
                                                break;
                                        }
                                        if (f | l < 0) break;
                                    }
                                    if (f)
                                    {
                                        l = 0;
                                        int n;
                                        for (n = m + 1; n < expressions[i].text.Length; n++)
                                        {
                                            switch (expressions[i].text[n])
                                            {
                                                case '(': l++; break;
                                                case ')': l--; break;
                                            }
                                            if (l < 0) break;
                                        }
                                        int x;
                                        if (Conversions.TryParseIntVb6(s, out x))
                                        {
                                            if (x >= 0)
                                            {
                                                while (x >= Subs.Length)
                                                {
                                                    Array.Resize<string>(ref Subs, Subs.Length << 1);
                                                }
                                                Subs[x] = expressions[i].text.Substring(m + 1, n - m - 1).Trim();
                                                expressions[i].text = expressions[i].text.Substring(0, j) + expressions[i].text.Substring(n);
                                            }
                                            else
                                            {
                                                continueWithNextExpression = true;
                                                GD.Print("Index is expected to be non-negative in " + t + Epilog);
                                            }
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            GD.Print("Index is invalid in " + t + Epilog);
                                        }
                                    }
                                    else
                                    {
                                        int x;
                                        if (Conversions.TryParseIntVb6(s, out x))
                                        {
                                            if (x >= 0 & x < Subs.Length && Subs[x] != null)
                                            {
                                                expressions[i].text = expressions[i].text.Substring(0, j) + Subs[x] + expressions[i].text.Substring(h + 1);
                                            }
                                            else
                                            {
                                                continueWithNextExpression = true;
                                                GD.Print("Index is out of range in " + t + Epilog);
                                            }
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            GD.Print("Index is invalid in " + t + Epilog);
                                        }
                                    }

                                }
                                break;
                        }
                    }
                }
                if (continueWithNextExpression)
                {
                    break;
                }
            }
        }
        // handle comments introduced via chr, rnd, sub
        {
            int length = expressions.Length;
            for (int i = 0; i < length; i++)
            {
                expressions[i].text = expressions[i].text.Trim();
                if (expressions[i].text.Length != 0)
                {
                    if (expressions[i].text[0] == ';')
                    {
                        for (int j = i; j < length - 1; j++)
                        {
                            expressions[j] = expressions[j + 1];
                        }
                        length--;
                        i--;
                    }
                }
                else
                {
                    for (int j = i; j < length - 1; j++)
                    {
                        expressions[j] = expressions[j + 1];
                    }
                    length--;
                    i--;
                }
            }
            if (length != expressions.Length)
            {
                Array.Resize<Expression>(ref expressions, length);
            }
        }
    }

    // separate commands and arguments
    private static void SeparateCommandsAndArguments(Expression expression, out string command, out string argumentSequence, System.Globalization.CultureInfo culture, string FileName, int LineNumber, bool raiseErrors)
    {
        bool openingerror = false, closingerror = false;
        int i;
        for (i = 0; i < expression.text.Length; i++)
        {
            if (expression.text[i] == '(')
            {
                bool found = false;
                i++;
                while (i < expression.text.Length)
                {
                    if (expression.text[i] == '(')
                    {
                        if (raiseErrors & !openingerror)
                        {
                            GD.Print("Invalid opening parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                            openingerror = true;
                        }
                    }
                    else if (expression.text[i] == ')')
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
                        GD.Print("Missing closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                        closingerror = true;
                    }
                    expression.text += ")";
                }
            }
            else if (expression.text[i] == ')')
            {
                if (raiseErrors & !closingerror)
                {
                    GD.Print("Invalid closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                    closingerror = true;
                }
            }
            else if (char.IsWhiteSpace(expression.text[i]))
            {
                if (i >= expression.text.Length - 1 || !char.IsWhiteSpace(expression.text[i + 1]))
                {
                    break;
                }
            }
        }
        if (i < expression.text.Length)
        {
            // white space was found outside of parentheses
            string a = expression.text.Substring(0, i);
            if (a.IndexOf('(') >= 0 & a.IndexOf(')') >= 0)
            {
                // indices found not separated from the command by spaces
                command = expression.text.Substring(0, i).TrimEnd();
                argumentSequence = expression.text.Substring(i + 1).TrimStart();
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
                        GD.Print("Missing closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                    }
                    argumentSequence = argumentSequence.Substring(1).TrimStart();
                }
            }
            else
            {
                // no indices found before the space
                if (i < expression.text.Length - 1 && expression.text[i + 1] == '(')
                {
                    // opening parenthesis follows the space
                    int j = expression.text.IndexOf(')', i + 1);
                    if (j > i + 1)
                    {
                        // closing parenthesis found
                        if (j == expression.text.Length - 1)
                        {
                            // only closing parenthesis found at the end of the expression
                            command = expression.text.Substring(0, i).TrimEnd();
                            argumentSequence = expression.text.Substring(i + 2, j - i - 2).Trim();
                        }
                        else
                        {
                            // detect border between indices and arguments
                            bool found = false;
                            command = null; argumentSequence = null;
                            for (int k = j + 1; k < expression.text.Length; k++)
                            {
                                if (char.IsWhiteSpace(expression.text[k]))
                                {
                                    command = expression.text.Substring(0, k).TrimEnd();
                                    argumentSequence = expression.text.Substring(k + 1).TrimStart();
                                    found = true; break;
                                }
                                else if (expression.text[k] == '(')
                                {
                                    command = expression.text.Substring(0, k).TrimEnd();
                                    argumentSequence = expression.text.Substring(k).TrimStart();
                                    found = true; break;
                                }
                            }
                            if (!found)
                            {
                                if (raiseErrors & !openingerror & !closingerror)
                                {
                                    GD.Print("Invalid syntax encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                                    openingerror = true;
                                    closingerror = true;
                                }
                                command = expression.text;
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
                                    GD.Print("Missing closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
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
                            GD.Print("Missing closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                        }
                        command = expression.text.Substring(0, i).TrimEnd();
                        argumentSequence = expression.text.Substring(i + 2).TrimStart();
                    }
                }
                else
                {
                    // no index possible
                    command = expression.text.Substring(0, i).TrimEnd();
                    argumentSequence = expression.text.Substring(i + 1).TrimStart();
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
                            GD.Print("Missing closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                        }
                        argumentSequence = argumentSequence.Substring(1).TrimStart();
                    }
                }
            }
        }
        else
        {
            // no single space found
            if (expression.text.EndsWith(")"))
            {
                i = expression.text.LastIndexOf('(');
                if (i >= 0)
                {
                    command = expression.text.Substring(0, i).TrimEnd();
                    argumentSequence = expression.text.Substring(i + 1, expression.text.Length - i - 2).Trim();
                }
                else
                {
                    command = expression.text;
                    argumentSequence = "";
                }
            }
            else
            {
                i = expression.text.IndexOf('(');
                if (i >= 0)
                {
                    if (raiseErrors & !closingerror)
                    {
                        GD.Print("Missing closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                    }
                    command = expression.text.Substring(0, i).TrimEnd();
                    argumentSequence = expression.text.Substring(i + 1).TrimStart();
                }
                else
                {
                    if (raiseErrors)
                    {
                        i = expression.text.IndexOf(')');
                        if (i >= 0 & !closingerror)
                        {
                            GD.Print("Invalid closing parenthesis encountered at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
                        }
                    }
                    command = expression.text;
                    argumentSequence = "";
                }
            }
        }
        // invalid trailing characters
        if (command.EndsWith(";"))
        {
            if (raiseErrors)
            {
                GD.Print("Invalid trailing semicolon encountered in " + command + " at line " + expression.line.ToString(culture) + ", column " + expression.column.ToString(culture) + " in file " + expression.file);
            }
            while (command.EndsWith(";"))
            {
                command = command.Substring(0, command.Length - 1);
            }
        }
    }

    #endregion

    #region Parsing (CsvRwRouteParser.cs)

    internal static void ParseRoute(Node root, string fileName, Encoding fileEncoding, bool isRW, string trainPath, string objectPath, string soundPath, bool previewOnly)
    {
        // initialize data
        string compatibilityFolder = System.IO.Path.Combine(objectPath, "Compatibility");

        RouteData routeData = new RouteData();
        routeData.BlockInterval = 25.0;
        routeData.AccurateObjectDisposal = false;
        routeData.FirstUsedBlock = -1;
        routeData.Blocks = new Block[1];
        routeData.Blocks[0] = new Block();
        routeData.Blocks[0].Rail = new Rail[1];
        routeData.Blocks[0].Rail[0].RailStart = true;
        routeData.Blocks[0].RailType = new int[] { 0 };
        routeData.Blocks[0].Limit = new Limit[] { };
        routeData.Blocks[0].Stop = new Stop[] { };
        routeData.Blocks[0].Station = -1;
        routeData.Blocks[0].StationPassAlarm = false;
        routeData.Blocks[0].Accuracy = 2.0;
        routeData.Blocks[0].AdhesionMultiplier = 1.0;
        //Data.Blocks[0].CurrentTrackState = new TrackManager.TrackElement(0.0);
        if (!previewOnly)
        {
            routeData.Blocks[0].Background = 0;
            routeData.Blocks[0].Brightness = new Brightness[] { };
            //Data.Blocks[0].Fog.Start = Game.NoFogStart;
            //Data.Blocks[0].Fog.End = Game.NoFogEnd;
            //Data.Blocks[0].Fog.Color = new Color24(128, 128, 128);
            routeData.Blocks[0].Cycle = new int[] { -1 };
            routeData.Blocks[0].Height = isRW ? 0.3 : 0.0;
            routeData.Blocks[0].RailFreeObj = new FreeObj[][] { };
            routeData.Blocks[0].GroundFreeObj = new FreeObj[] { };
            routeData.Blocks[0].RailWall = new WallDike[] { };
            routeData.Blocks[0].RailDike = new WallDike[] { };
            routeData.Blocks[0].RailPole = new Pole[] { };
            routeData.Blocks[0].Form = new Form[] { };
            routeData.Blocks[0].Crack = new Crack[] { };
            routeData.Blocks[0].Signal = new Signal[] { };
            routeData.Blocks[0].Section = new Section[] { };
            routeData.Blocks[0].Sound = new Sound[] { };
            routeData.Blocks[0].Transponder = new Transponder[] { };
            routeData.Blocks[0].PointsOfInterest = new PointOfInterest[] { };
            routeData.Markers = new Marker[] { };
            
            // poles
            // Node poleParent = new Node("Poles");
            Node poleParent = new Node ();
            poleParent.Name = "Poles";
            string poleFolder = System.IO.Path.Combine(compatibilityFolder, "Poles");
            routeData.Structure.Poles = new UnifiedObject[][] {
                    new UnifiedObject[] {
                        ObjectManager.Instance.LoadStaticObject(poleParent, System.IO.Path.Combine (poleFolder, "pole_1.csv"), fileEncoding, false, false, false)
                    }, new UnifiedObject[] {
                        ObjectManager.Instance.LoadStaticObject(poleParent, System.IO.Path.Combine (poleFolder, "pole_2.csv"), fileEncoding, false, false, false)
                    }, new UnifiedObject[] {
                        ObjectManager.Instance.LoadStaticObject(poleParent, System.IO.Path.Combine (poleFolder, "pole_3.csv"), fileEncoding, false, false, false)
                    }, new UnifiedObject[] {
                        ObjectManager.Instance.LoadStaticObject(poleParent, System.IO.Path.Combine (poleFolder, "pole_4.csv"), fileEncoding, false, false, false)
                    }
                };


            routeData.Structure.Rail = new UnifiedObject[] { };
            routeData.Structure.Ground = new UnifiedObject[] { };
            routeData.Structure.WallL = new UnifiedObject[] { };
            routeData.Structure.WallR = new UnifiedObject[] { };
            routeData.Structure.DikeL = new UnifiedObject[] { };
            routeData.Structure.DikeR = new UnifiedObject[] { };
            routeData.Structure.FormL = new UnifiedObject[] { };
            routeData.Structure.FormR = new UnifiedObject[] { };
            routeData.Structure.FormCL = new UnifiedObject[] { };
            routeData.Structure.FormCR = new UnifiedObject[] { };
            routeData.Structure.RoofL = new UnifiedObject[] { };
            routeData.Structure.RoofR = new UnifiedObject[] { };
            routeData.Structure.RoofCL = new UnifiedObject[] { };
            routeData.Structure.RoofCR = new UnifiedObject[] { };
            routeData.Structure.CrackL = new UnifiedObject[] { };
            routeData.Structure.CrackR = new UnifiedObject[] { };
            routeData.Structure.FreeObj = new UnifiedObject[] { };
            routeData.Structure.Beacon = new UnifiedObject[] { };
            routeData.Structure.Cycle = new int[][] { };
            routeData.Structure.Run = new int[] { };
            routeData.Structure.Flange = new int[] { };
            routeData.Backgrounds = new ImageTexture[] { };
            //Data.TimetableDaytime = new Textures.Texture[] { null, null, null, null };
            //Data.TimetableNighttime = new Textures.Texture[] { null, null, null, null };

            Node signalParent = new Node();
            signalParent.Name = "Signals";

            // signals
            string signalFolder = System.IO.Path.Combine(compatibilityFolder, @"Signals\Japanese"); //TODO path
            routeData.SignalData = new SignalData[7];
            routeData.SignalData[3] = new CompatibilitySignalData(new int[] { 0, 2, 4 }, new UnifiedObject[] {
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_3_0.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_3_2.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_3_4.csv"), fileEncoding, false, false, false)
                                                                 });
            routeData.SignalData[4] = new CompatibilitySignalData(new int[] { 0, 1, 2, 4 }, new UnifiedObject[] {
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4_0.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4a_2.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4a_1.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4a_4.csv"), fileEncoding, false, false, false)
                                                                 });
            routeData.SignalData[5] = new CompatibilitySignalData(new int[] { 0, 1, 2, 3, 4 }, new UnifiedObject[] {
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_0.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5a_1.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_2.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_3.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_4.csv"), fileEncoding, false, false, false)
                                                                 });
            routeData.SignalData[6] = new CompatibilitySignalData(new int[] { 0, 3, 4 }, new UnifiedObject[] {
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_0.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_3.csv"), fileEncoding, false, false, false),
                                                                     ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_4.csv"), fileEncoding, false, false, false)
                                                                 });
            // compatibility signals
            routeData.CompatibilitySignalData = new CompatibilitySignalData[9];
            routeData.CompatibilitySignalData[0] = new CompatibilitySignalData(new int[] { 0, 2 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2a_2.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[1] = new CompatibilitySignalData(new int[] { 0, 4 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2b_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[2] = new CompatibilitySignalData(new int[] { 0, 2, 4 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_3_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_3_2.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_3_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[3] = new CompatibilitySignalData(new int[] { 0, 1, 2, 4 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4a_1.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4a_2.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4a_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[4] = new CompatibilitySignalData(new int[] { 0, 2, 3, 4 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4b_2.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4b_3.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4b_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[5] = new CompatibilitySignalData(new int[] { 0, 1, 2, 3, 4 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5a_1.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_2.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_3.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[6] = new CompatibilitySignalData(new int[] { 0, 2, 3, 4, 5 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_2.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_3.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_4.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5b_5.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[7] = new CompatibilitySignalData(new int[] { 0, 1, 2, 3, 4, 5 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_1.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_2.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_3.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_4.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_5.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignalData[8] = new CompatibilitySignalData(new int[] { 0, 3, 4 }, new UnifiedObject[] {
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_0.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_3.csv"), fileEncoding, false, false, false),
                                                                                  ObjectManager.Instance.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_4.csv"), fileEncoding, false, false, false)
                                                                              });
            // game data
            //Game.Sections = new Game.Section[1];
            //Game.Sections[0].Aspects = new Game.SectionAspect[] { new Game.SectionAspect(0, 0.0), new Game.SectionAspect(4, double.PositiveInfinity) };
            //Game.Sections[0].CurrentAspect = 0;
            //Game.Sections[0].NextSection = -1;
            //Game.Sections[0].PreviousSection = -1;
            //Game.Sections[0].SignalIndices = new int[] { };
            //Game.Sections[0].StationIndex = -1;
            //Game.Sections[0].TrackPosition = 0;
            //Game.Sections[0].Trains = new TrainManager.Train[] { };
            // continue
            routeData.SignalSpeeds = new double[] { 0.0, 6.94444444444444, 15.2777777777778, 20.8333333333333, double.PositiveInfinity, double.PositiveInfinity };
        }

        // >> Start OpenBVE ParseRouteForData()
        string[] lines = System.IO.File.ReadAllLines(fileName, fileEncoding);
        List<Expression> expressions = PreprocessSplitIntoExpressions(fileName, fileEncoding, isRW, lines, true, 0.0);
        Expression[] expressionArray = expressions.ToArray();
        PreprocessChrRndSub(fileName, fileEncoding, isRW, ref expressionArray);

        double[] unitOfLength = new double[] { 1.0 };
        routeData.UnitOfSpeed = 0.277777777777778;

        /// CswRwRouterParser.Preprocess.cs 
        //PreprocessOptions(fileName, isRW, Encoding, expressions, ref Data, ref UnitOfLength);
        //PreprocessSortByTrackPosition(fileName, isRW, UnitOfLength, ref expressions);
        ParseRouteDetails(fileName, fileEncoding, isRW, expressionArray, trainPath, objectPath, soundPath, unitOfLength, ref routeData, previewOnly);

        //Game.RouteUnitOfLength = UnitOfLength;
        // >> End OpenBVE ParseRouteForData()
        ApplyRouteData(root, fileName, compatibilityFolder, fileEncoding, ref routeData, previewOnly);
        // >> End OpenBVE ParseRoute


    }
        
    private static void ParseRouteDetails(string fileName, Encoding fileEncoding, bool isRW, Expression[] expressions, string trainPath, string objectPath, string soundPath, double[] unitOfLength, ref RouteData routeData, bool previewOnly)
    {
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
        string Section = ""; bool sectionAlwaysPrefix = false;
        int blockIndex = 0;
        int blocksUsed = routeData.Blocks.Length;
        //Game.Stations = new Game.Station[] { };
        int currentStation = -1;
        int currentStop = -1;
        bool departureSignalUsed = false;
        int currentSection = 0;
        bool valueBasedSection = false;
        double progressFactor = expressions.Length == 0 ? 0.3333 : 0.3333 / (double)expressions.Length;

        Node rootRoutePrefabs = new Node();
        rootRoutePrefabs.Name = "Route (Prefabs)";

        // process non-track namespaces
        for (int j = 0; j < expressions.Length; j++)
        {
            //Loading.RouteProgress = (double)j * progressFactor;
            if ((j & 255) == 0)
            {
                //System.Threading.Thread.Sleep(1);
                //if (Loading.Cancel) return;
            }
            if (expressions[j].text.StartsWith("[") & expressions[j].text.EndsWith("]"))
            {
                Section = expressions[j].text.Substring(1, expressions[j].text.Length - 2).Trim();
                if (string.Compare(Section, "object", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Section = "Structure";
                }
                else if (string.Compare(Section, "railway", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Section = "Track";
                }
                sectionAlwaysPrefix = true;
            }
            else
            {
                // find equals
                int equals = expressions[j].text.IndexOf('=');
                if (equals >= 0)
                {
                    // handle RW cycle syntax
                    string t = expressions[j].text.Substring(0, equals);
                    if (Section.ToLowerInvariant() == "cycle" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Ground(" + t + ")";
                        }
                    }
                    else if (Section.ToLowerInvariant() == "signal" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Void(" + t + ")";
                        }
                    }
                    // convert RW style into CSV style
                    expressions[j].text = t + " " + expressions[j].text.Substring(equals + 1);
                }
                // separate command and arguments
                string command, argumentSequence;
                SeparateCommandsAndArguments(expressions[j], out command, out argumentSequence, culture, expressions[j].file, j, false);

                // process command
                double number;
                bool numberCheck = !isRW || string.Compare(Section, "track", StringComparison.OrdinalIgnoreCase) == 0;
                if (numberCheck && Conversions.TryParseDouble(command, unitOfLength, out number))
                {
                    // track position (ignored)
                }
                else
                {
                    // split arguments
                    string[] arguments;
                    {
                        int n = 0;
                        for (int k = 0; k < argumentSequence.Length; k++)
                        {
                            if (isRW & argumentSequence[k] == ',')
                            {
                                n++;
                            }
                            else if (argumentSequence[k] == ';')
                            {
                                n++;
                            }
                        }
                        arguments = new string[n + 1];
                        int a = 0, h = 0;
                        for (int k = 0; k < argumentSequence.Length; k++)
                        {
                            if (isRW & argumentSequence[k] == ',')
                            {
                                arguments[h] = argumentSequence.Substring(a, k - a).Trim();
                                a = k + 1; h++;
                            }
                            else if (argumentSequence[k] == ';')
                            {
                                arguments[h] = argumentSequence.Substring(a, k - a).Trim();
                                a = k + 1; h++;
                            }
                        }
                        if (argumentSequence.Length - a > 0)
                        {
                            arguments[h] = argumentSequence.Substring(a).Trim();
                            h++;
                        }
                        Array.Resize<string>(ref arguments, h);
                    }
                    // preprocess command
                    if (command.ToLowerInvariant() == "with")
                    {
                        if (arguments.Length >= 1)
                        {
                            Section = arguments[0];
                            sectionAlwaysPrefix = false;
                        }
                        else
                        {
                            Section = "";
                            sectionAlwaysPrefix = false;
                        }
                        command = null;
                    }
                    else
                    {
                        if (command.StartsWith("."))
                        {
                            command = Section + command;
                        }
                        else if (sectionAlwaysPrefix)
                        {
                            command = Section + "." + command;
                        }
                        command = command.Replace(".Void", "");
                        if (command.StartsWith("structure", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 5).TrimEnd();
                        }
                        else if (command.StartsWith("texture.background", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 5).TrimEnd();
                        }
                        else if (command.StartsWith("texture.background", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".x", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.x" + command.Substring(18, command.Length - 20).TrimEnd();
                        }
                        else if (command.StartsWith("texture.background", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".aspect", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.aspect" + command.Substring(18, command.Length - 25).TrimEnd();
                        }
                        else if (command.StartsWith("structure.back", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".x", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.x" + command.Substring(14, command.Length - 16).TrimEnd();
                        }
                        else if (command.StartsWith("structure.back", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".aspect", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.aspect" + command.Substring(14, command.Length - 21).TrimEnd();
                        }
                        else if (command.StartsWith("cycle", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".params", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 7).TrimEnd();
                        }
                        else if (command.StartsWith("signal", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 5).TrimEnd();
                        }
                        else if (command.StartsWith("train.run", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 4).TrimEnd();
                        }
                        else if (command.StartsWith("train.flange", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 4).TrimEnd();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".day.load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.day" + command.Substring(15, command.Length - 24).Trim();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".night.load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.night" + command.Substring(15, command.Length - 26).Trim();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".day", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.day" + command.Substring(15, command.Length - 19).Trim();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".night", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.night" + command.Substring(15, command.Length - 21).Trim();
                        }
                        else if (command.StartsWith("route.signal", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 4).TrimEnd();
                        }
                    }
                    // handle indices
                    int commandIndex1 = 0, commandIndex2 = 0;
                    if (command != null && command.EndsWith(")"))
                    {
                        for (int k = command.Length - 2; k >= 0; k--)
                        {
                            if (command[k] == '(')
                            {
                                string Indices = command.Substring(k + 1, command.Length - k - 2).TrimStart();
                                command = command.Substring(0, k).TrimEnd();
                                int h = Indices.IndexOf(";");
                                if (h >= 0)
                                {
                                    string a = Indices.Substring(0, h).TrimEnd();
                                    string b = Indices.Substring(h + 1).TrimStart();
                                    if (a.Length > 0 && !Conversions.TryParseIntVb6(a, out commandIndex1))
                                    {
                                        GD.Print("Invalid first index appeared at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file + ".");
                                        command = null; break;
                                    }
                                    else if (b.Length > 0 && !Conversions.TryParseIntVb6(b, out commandIndex2))
                                    {
                                        GD.Print("Invalid second index appeared at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file + ".");
                                        command = null; break;
                                    }
                                }
                                else
                                {
                                    if (Indices.Length > 0 && !Conversions.TryParseIntVb6(Indices, out commandIndex1))
                                    {
                                        GD.Print("Invalid index appeared at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file + ".");
                                        command = null; break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    // process command
                    if (command != null && command.Length != 0)
                    {
                        switch (command.ToLowerInvariant())
                        {
                            // options
                            case "options.blocklength":
                                {
                                    double length = 25.0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], unitOfLength, out length))
                                    {
                                        GD.Print("Length is invalid in Options.BlockLength at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        length = 25.0;
                                    }
                                    routeData.BlockInterval = length;
                                }
                                break;
                            case "options.unitoflength":
                            case "options.unitofspeed":
                            case "options.objectvisibility":
                                break;
                            case "options.sectionbehavior":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    int a;
                                    if (!Conversions.TryParseIntVb6(arguments[0], out a))
                                    {
                                        GD.Print("Mode is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a != 0 & a != 1)
                                    {
                                        GD.Print("Mode is expected to be either 0 or 1 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        valueBasedSection = a == 1;
                                    }
                                }
                                break;
                            case "options.cantbehavior":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    int a;
                                    if (!Conversions.TryParseIntVb6(arguments[0], out a))
                                    {
                                        GD.Print("Mode is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a != 0 & a != 1)
                                    {
                                        GD.Print("Mode is expected to be either 0 or 1 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        routeData.SignedCant = a == 1;
                                    }
                                }
                                break;
                            case "options.fogbehavior":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    int a;
                                    if (!Conversions.TryParseIntVb6(arguments[0], out a))
                                    {
                                        GD.Print("Mode is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a != 0 & a != 1)
                                    {
                                        GD.Print("Mode is expected to be either 0 or 1 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        routeData.FogTransitionMode = a == 1;
                                    }
                                }
                                break;
                            // route
                            case "route.comment":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    //Game.RouteComment = arguments[0];
                                }
                                break;
                            case "route.image":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    string f = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fileName), arguments[0]);
                                    if (!System.IO.File.Exists(f))
                                    {
                                        GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Game.RouteImage = f;
                                    }
                                }
                                break;
                            case "route.timetable":
                                if (!previewOnly)
                                {
                                    if (arguments.Length < 1)
                                    {
                                        GD.Print("" + command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Timetable.DefaultTimetableDescription = arguments[0];
                                    }
                                }
                                break;
                            case "route.change":
                                if (!previewOnly)
                                {
                                    int change = 0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out change))
                                    {
                                        GD.Print("Mode is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        change = 0;
                                    }
                                    else if (change < -1 | change > 1)
                                    {
                                        GD.Print("Mode is expected to be -1, 0 or 1 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        change = 0;
                                    }
                                    //Game.TrainStart = (Game.TrainStartMode)change;
                                }
                                break;
                            case "route.gauge":
                            case "train.gauge":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        GD.Print("ValueInMillimeters is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a <= 0.0)
                                    {
                                        GD.Print("ValueInMillimeters is expected to be positive in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Game.RouteRailGauge = 0.001 * a;
                                    }
                                }
                                break;
                            case "route.signal":
                                if (!previewOnly)
                                {
                                    if (arguments.Length < 1)
                                    {
                                        GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        double a;
                                        if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                        {
                                            GD.Print("Speed is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (commandIndex1 < 0)
                                            {
                                                GD.Print("AspectIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (a < 0.0)
                                            {
                                                GD.Print("Speed is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.SignalSpeeds.Length)
                                                {
                                                    int n = routeData.SignalSpeeds.Length;
                                                    Array.Resize<double>(ref routeData.SignalSpeeds, commandIndex1 + 1);
                                                    for (int i = n; i < commandIndex1; i++)
                                                    {
                                                        routeData.SignalSpeeds[i] = double.PositiveInfinity;
                                                    }
                                                }
                                                routeData.SignalSpeeds[commandIndex1] = a * routeData.UnitOfSpeed;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "route.runinterval":
                            case "train.interval":
                                {
                                    if (!previewOnly)
                                    {
                                        double[] intervals = new double[arguments.Length];
                                        for (int k = 0; k < arguments.Length; k++)
                                        {
                                            if (!Conversions.TryParseDoubleVb6(arguments[k], out intervals[k]))
                                            {
                                                GD.Print("Interval" + k.ToString(culture) + " is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                        }
                                        Array.Sort<double>(intervals);
                                        //Game.PrecedingTrainTimeDeltas = intervals;
                                    }
                                }
                                break;
                            case "train.velocity":
                                {
                                    if (!previewOnly)
                                    {
                                        double limit = 0.0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out limit))
                                        {
                                            GD.Print("Speed is invalid in Train.Velocity at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            limit = 0.0;
                                        }
                                        //Game.PrecedingTrainSpeedLimit = limit <= 0.0 ? double.PositiveInfinity : data.UnitOfSpeed * limit;
                                    }
                                }
                                break;
                            case "route.accelerationduetogravity":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        GD.Print("Value is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a <= 0.0)
                                    {
                                        GD.Print("Value is expected to be positive in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Game.RouteAccelerationDueToGravity = a;
                                    }
                                }
                                break;
                            case "route.elevation":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], unitOfLength, out a))
                                    {
                                        GD.Print("Height is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Game.RouteInitialElevation = a;
                                    }
                                }
                                break;
                            case "route.temperature":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        GD.Print("ValueInCelsius is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a <= -273.15)
                                    {
                                        GD.Print("ValueInCelsius is expected to be greater than to -273.15 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Game.RouteInitialAirTemperature = a + 273.15;
                                    }
                                }
                                break;
                            case "route.pressure":
                                if (arguments.Length < 1)
                                {
                                    GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        GD.Print("ValueInKPa is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (a <= 0.0)
                                    {
                                        GD.Print("ValueInKPa is expected to be positive in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else
                                    {
                                        //Game.RouteInitialAirPressure = 1000.0 * a;
                                    }
                                }
                                break;
                            case "route.ambientlight":
                                {
                                    int r = 255, g = 255, b = 255;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out r))
                                    {
                                        GD.Print("RedValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (r < 0 | r > 255)
                                    {
                                        GD.Print("RedValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        r = r < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out g))
                                    {
                                        GD.Print("GreenValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (g < 0 | g > 255)
                                    {
                                        GD.Print("GreenValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        g = g < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out b))
                                    {
                                        GD.Print("BlueValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (b < 0 | b > 255)
                                    {
                                        GD.Print("BlueValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        b = b < 0 ? 0 : 255;
                                    }
                                    //Renderer.OptionAmbientColor = new Color24((byte)r, (byte)g, (byte)b);
                                }
                                break;
                            case "route.directionallight":
                                {
                                    int r = 255, g = 255, b = 255;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out r))
                                    {
                                        GD.Print("RedValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (r < 0 | r > 255)
                                    {
                                        GD.Print("RedValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        r = r < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out g))
                                    {
                                        GD.Print("GreenValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (g < 0 | g > 255)
                                    {
                                        GD.Print("GreenValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        g = g < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out b))
                                    {
                                        GD.Print("BlueValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    else if (b < 0 | b > 255)
                                    {
                                        GD.Print("BlueValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        b = b < 0 ? 0 : 255;
                                    }
                                    //Renderer.OptionDiffuseColor = new Color24((byte)r, (byte)g, (byte)b);
                                }
                                break;
                            case "route.lightdirection":
                                {
                                    double theta = 60.0, phi = -26.565051177078;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out theta))
                                    {
                                        GD.Print("Theta is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out phi))
                                    {
                                        GD.Print("Phi is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    }
                                    theta *= 0.0174532925199433;
                                    phi *= 0.0174532925199433;
                                    double dx = Math.Cos(theta) * Math.Sin(phi);
                                    double dy = -Math.Sin(theta);
                                    double dz = Math.Cos(theta) * Math.Cos(phi);
                                    //Renderer.OptionLightPosition = new Vector3f((float)-dx, (float)-dy, (float)-dz);
                                }
                                break;
                            // train
                            case "train.folder":
                            case "train.file":
                                {
                                    if (previewOnly)
                                    {
                                        if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FolderName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                //Game.TrainName = arguments[0];
                                            }
                                        }
                                    }
                                }
                                break;
                            case "train.run":
                            case "train.rail":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RailTypeIndex is out of range in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            int val = 0;
                                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out val))
                                            {
                                                GD.Print("RunSoundIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                val = 0;
                                            }
                                            if (val < 0)
                                            {
                                                GD.Print("RunSoundIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                val = 0;
                                            }
                                            if (commandIndex1 >= routeData.Structure.Run.Length)
                                            {
                                                Array.Resize<int>(ref routeData.Structure.Run, commandIndex1 + 1);
                                            }
                                            routeData.Structure.Run[commandIndex1] = val;
                                        }
                                    }
                                }
                                break;
                            case "train.flange":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RailTypeIndex is out of range in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            int val = 0;
                                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out val))
                                            {
                                                GD.Print("FlangeSoundIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                val = 0;
                                            }
                                            if (val < 0)
                                            {
                                                GD.Print("FlangeSoundIndex expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                val = 0;
                                            }
                                            if (commandIndex1 >= routeData.Structure.Flange.Length)
                                            {
                                                Array.Resize<int>(ref routeData.Structure.Flange, commandIndex1 + 1);
                                            }
                                            routeData.Structure.Flange[commandIndex1] = val;
                                        }
                                    }
                                }
                                break;
                            case "train.timetable.day":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("TimetableIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                //while (commandIndex1 >= data.TimetableDaytime.Length)
                                                //{
                                                //    int n = data.TimetableDaytime.Length;
                                                //    Array.Resize<Textures.texture>(ref data.TimetableDaytime, n << 1);
                                                //    for (int i = n; i < data.TimetableDaytime.Length; i++)
                                                //    {
                                                //        data.TimetableDaytime[i] = null;
                                                //    }
                                                //}
                                                string f = System.IO.Path.Combine(trainPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                }
                                                if (System.IO.File.Exists(f))
                                                {
                                                    //Textures.RegisterTexture(f, out data.TimetableDaytime[commandIndex1]);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "train.timetable.night":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("TimetableIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                //while (commandIndex1 >= data.TimetableNighttime.Length)
                                                //{
                                                //    int n = data.TimetableNighttime.Length;
                                                //    Array.Resize<Textures.texture>(ref data.TimetableNighttime, n << 1);
                                                //    for (int i = n; i < data.TimetableNighttime.Length; i++)
                                                //    {
                                                //        data.TimetableNighttime[i] = null;
                                                //    }
                                                //}
                                                string f = System.IO.Path.Combine(trainPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                }
                                                if (System.IO.File.Exists(f))
                                                {
                                                    //Textures.RegisterTexture(f, out data.TimetableNighttime[commandIndex1]);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            // structure
                            case "structure.rail":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RailStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.Rail.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.Rail, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.Rail[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.beacon":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("BeaconStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.Beacon.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.Beacon, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.Beacon[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.pole":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("AdditionalRailsCovered is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (commandIndex2 < 0)
                                        {
                                            GD.Print("PoleStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.Poles.Length)
                                                {
                                                    Array.Resize<UnifiedObject[]>(ref routeData.Structure.Poles, commandIndex1 + 1);
                                                }
                                                if (routeData.Structure.Poles[commandIndex1] == null)
                                                {
                                                    routeData.Structure.Poles[commandIndex1] = new UnifiedObject[commandIndex2 + 1];
                                                }
                                                else if (commandIndex2 >= routeData.Structure.Poles[commandIndex1].Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.Poles[commandIndex1], commandIndex2 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.Poles[commandIndex1][commandIndex2] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.ground":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("GroundStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.Ground.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.Ground, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.Ground[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.walll":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("WallStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.WallL.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.WallL, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.WallL[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.wallr":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("WallStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.WallR.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.WallR, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.WallR[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.dikel":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("DikeStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.DikeL.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.DikeL, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.DikeL[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.diker":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("DikeStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.DikeR.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.DikeR, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.DikeR[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.forml":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("FormStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.FormL.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.FormL, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.FormL[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.formr":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("FormStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.FormR.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.FormR, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.FormR[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.formcl":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("FormStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.FormCL.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.FormCL, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.FormCL[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, true, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.formcr":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("FormStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.FormCR.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.FormCR, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.FormCR[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, true, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.roofl":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    GD.Print("RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    if (commandIndex1 >= routeData.Structure.RoofL.Length)
                                                    {
                                                        Array.Resize<UnifiedObject>(ref routeData.Structure.RoofL, commandIndex1 + 1);
                                                    }
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else
                                                    {
                                                        routeData.Structure.RoofL[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.roofr":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    GD.Print("RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    if (commandIndex1 >= routeData.Structure.RoofR.Length)
                                                    {
                                                        Array.Resize<UnifiedObject>(ref routeData.Structure.RoofR, commandIndex1 + 1);
                                                    }
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else
                                                    {
                                                        routeData.Structure.RoofR[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.roofcl":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    GD.Print("RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    if (commandIndex1 >= routeData.Structure.RoofCL.Length)
                                                    {
                                                        Array.Resize<UnifiedObject>(ref routeData.Structure.RoofCL, commandIndex1 + 1);
                                                    }
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else
                                                    {
                                                        routeData.Structure.RoofCL[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, true, false, false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.roofcr":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    GD.Print("RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    GD.Print("RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    if (commandIndex1 >= routeData.Structure.RoofCR.Length)
                                                    {
                                                        Array.Resize<UnifiedObject>(ref routeData.Structure.RoofCR, commandIndex1 + 1);
                                                    }
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else
                                                    {
                                                        routeData.Structure.RoofCR[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, true, false, false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.crackl":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("CrackStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.CrackL.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.CrackL, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.CrackL[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, true, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.crackr":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("CrackStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.CrackR.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.CrackR, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.CrackR[commandIndex1] = ObjectManager.Instance.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, true, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "structure.freeobj":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("FreeObjStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Structure.FreeObj.Length)
                                                {
                                                    Array.Resize<UnifiedObject>(ref routeData.Structure.FreeObj, commandIndex1 + 1);
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " could not be found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    routeData.Structure.FreeObj[commandIndex1] = ObjectManager.Instance.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            // signal
                            case "signal":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have between 1 and 2 arguments at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (commandIndex1 >= routeData.SignalData.Length)
                                            {
                                                Array.Resize<SignalData>(ref routeData.SignalData, commandIndex1 + 1);
                                            }
                                            if (arguments[0].EndsWith(".animated", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                                {
                                                    GD.Print("AnimatedObjectFile contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    if (arguments.Length > 1)
                                                    {
                                                        GD.Print(command + " is expected to have exactly 1 argument when using animated objects at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        GD.Print("SignalFileWithoutExtension " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else
                                                    {
                                                        //ObjectManager.UnifiedObject Object = ObjectManager.Instance.LoadObjectAsPrefab(f, fileEncoding, false, false, false);
                                                        //if (Object is ObjectManager.AnimatedObjectCollection)
                                                        //{
                                                        //    AnimatedObjectSignalData Signal = new AnimatedObjectSignalData();
                                                        //    Signal.Objects = (ObjectManager.AnimatedObjectCollection)Object;
                                                        //    data.SignalData[commandIndex1] = Signal;
                                                        //}
                                                        //else
                                                        //{
                                                        //     GD.Print("GlowFileWithoutExtension " + f + " is not a valid animated object in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                        //}
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //if (arguments[0].LastIndexOfAny(Path.GetInvalidPathChars()) >= 0)
                                                //{
                                                //     GD.Print("SignalFileWithoutExtension contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                //}
                                                //else
                                                //{
                                                //    if (arguments.Length > 2)
                                                //    {
                                                //        GD.Print(command + " is expected to have between 1 and 2 arguments at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                //    }
                                                //    string f = System.IO.Path.Combine(ObjectPath, arguments[0]);
                                                //    Bve4SignalData Signal = new Bve4SignalData();
                                                //    Signal.BaseObject = ObjectManager.Instance.LoadStaticObject(f, fileEncoding, false, false, false);
                                                //    Signal.GlowObject = null;
                                                //    string Folder = System.IO.Path.GetDirectoryName(f);
                                                //    if (!System.IO.Directory.Exists(Folder))
                                                //    {
                                                //         GD.Print("The folder " + Folder + " could not be found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                //    }
                                                //    else
                                                //    {
                                                //        Signal.SignalTextures = LoadAllTextures(f, false);
                                                //        Signal.GlowTextures = new Textures.texture[] { };
                                                //        if (arguments.Length >= 2 && arguments[1].Length != 0)
                                                //        {
                                                //            if (arguments[1].LastIndexOfAny(Path.GetInvalidPathChars()) >= 0)
                                                //            {
                                                //                 GD.Print("GlowFileWithoutExtension contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                //            }
                                                //            else
                                                //            {
                                                //                f = System.IO.Path.Combine(objectPath, arguments[1]);
                                                //                Signal.GlowObject = ObjectManager.Instance.LoadStaticObject(f, fileEncoding, false, false, false);
                                                //                if (Signal.GlowObject != null)
                                                //                {
                                                //                    Signal.GlowTextures = LoadAllTextures(f, true);
                                                //                    for (int p = 0; p < Signal.GlowObject.Mesh.Materials.Length; p++)
                                                //                    {
                                                //                        Signal.GlowObject.Mesh.Materials[p].BlendMode = World.MeshMaterialBlendMode.Additive;
                                                //                        Signal.GlowObject.Mesh.Materials[p].GlowAttenuationData = World.GetGlowAttenuationData(200.0, World.GlowAttenuationMode.DivisionExponent4);
                                                //                    }
                                                //                }
                                                //            }
                                                //        }
                                                //        data.SignalData[commandIndex1] = Signal;
                                                //    }
                                                //}
                                            }
                                        }
                                    }
                                }
                                break;

                            // texture
                            case "texture.background":
                            case "structure.back":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("BackgroundTextureIndex is expected to be non-negative at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (commandIndex1 >= routeData.Backgrounds.Length)
                                                {
                                                    int a = routeData.Backgrounds.Length;
                                                    Array.Resize<ImageTexture>(ref routeData.Backgrounds, commandIndex1 + 1);
                                                    for (int k = a; k <= commandIndex1; k++)
                                                    {
                                                        routeData.Backgrounds[k] = new ImageTexture();
                                                    }
                                                }
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName" + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    // TODO: Only BMP support...
                                                    
                                                    ImageTexture background = new ImageTexture(); 
                                                    background.Load(f);
                                                    if (background != null)
                                                        routeData.Backgrounds[commandIndex1] = background;

                                                    //routeData.Backgrounds

                                                    // OLD (works when the textuers are in the asset folder, and have been converted by unity editor) 
                                                    // routeData.Backgrounds[commandIndex1] = Resources.Load(Path.Combine("Objects/gaku/", Path.GetFileNameWithoutExtension(arguments[0]))) as Texture;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "texture.background.x":
                            case "structure.back.x":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("BackgroundTextureIndex is expected to be non-negative at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            //if (commandIndex1 >= data.Backgrounds.Length)
                                            //{
                                            //    int a = data.Backgrounds.Length;
                                            //    Array.Resize<World.Background>(ref data.Backgrounds, commandIndex1 + 1);
                                            //    for (int k = a; k <= commandIndex1; k++)
                                            //    {
                                            //        data.Backgrounds[k] = new World.Background(null, 6, false);
                                            //    }
                                            //}
                                            //int x;
                                            //if (!Conversions.TryParseIntVb6(arguments[0], out x))
                                            //{
                                            //     GD.Print("BackgroundTextureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            //}
                                            //else if (x == 0)
                                            //{
                                            //     GD.Print("RepetitionCount is expected to be non-zero in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            //}
                                            //else
                                            //{
                                            //    data.Backgrounds[commandIndex1].Repetition = x;
                                            //}
                                        }
                                    }
                                }
                                break;
                            case "texture.background.aspect":
                            case "structure.back.aspect":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            GD.Print("BackgroundTextureIndex is expected to be non-negative at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            GD.Print(command + " is expected to have one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            //if (commandIndex1 >= data.Backgrounds.Length)
                                            //{
                                            //    int a = data.Backgrounds.Length;
                                            //    Array.Resize<World.Background>(ref data.Backgrounds, commandIndex1 + 1);
                                            //    for (int k = a; k <= commandIndex1; k++)
                                            //    {
                                            //        data.Backgrounds[k] = new World.Background(null, 6, false);
                                            //    }
                                            //}
                                            //int aspect;
                                            //if (!Conversions.TryParseIntVb6(arguments[0], out aspect))
                                            //{
                                            //     GD.Print("BackgroundTextureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            //}
                                            //else if (aspect != 0 & aspect != 1)
                                            //{
                                            //     GD.Print("Value is expected to be either 0 or 1 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            //}
                                            //else
                                            //{
                                            //    data.Backgrounds[commandIndex1].KeepAspectRatio = aspect == 1;
                                            //}
                                        }
                                    }
                                }
                                break;
                            // cycle
                            case "cycle.ground":
                                if (!previewOnly)
                                {
                                    if (commandIndex1 >= routeData.Structure.Cycle.Length)
                                    {
                                        Array.Resize<int[]>(ref routeData.Structure.Cycle, commandIndex1 + 1);
                                    }
                                    routeData.Structure.Cycle[commandIndex1] = new int[arguments.Length];
                                    for (int k = 0; k < arguments.Length; k++)
                                    {
                                        int ix = 0;
                                        if (arguments[k].Length > 0 && !Conversions.TryParseIntVb6(arguments[k], out ix))
                                        {
                                            GD.Print("GroundStructureIndex" + (k + 1).ToString(culture) + " is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            ix = 0;
                                        }
                                        if (ix < 0 | ix >= routeData.Structure.Ground.Length)
                                        {
                                            GD.Print("GroundStructureIndex" + (k + 1).ToString(culture) + " is out of range in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            ix = 0;
                                        }
                                        routeData.Structure.Cycle[commandIndex1][k] = ix;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        // process track namespace
        for (int j = 0; j < expressions.Length; j++)
        {
            //Loading.RouteProgress = 0.3333 + (double)j * progressFactor;
            if ((j & 255) == 0)
            {
                //System.Threading.Thread.Sleep(1);
                //if (Loading.Cancel) return;
            }
            if (expressions[j].text.StartsWith("[") & expressions[j].text.EndsWith("]"))
            {
                Section = expressions[j].text.Substring(1, expressions[j].text.Length - 2).Trim();
                if (string.Compare(Section, "object", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Section = "Structure";
                }
                else if (string.Compare(Section, "railway", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    Section = "Track";
                }
                sectionAlwaysPrefix = true;
            }
            else
            {
                // find equals
                int equals = expressions[j].text.IndexOf('=');
                if (equals >= 0)
                {
                    // handle RW cycle syntax
                    string t = expressions[j].text.Substring(0, equals);
                    if (Section.ToLowerInvariant() == "cycle" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Ground(" + t + ")";
                        }
                    }
                    else if (Section.ToLowerInvariant() == "signal" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Void(" + t + ")";
                        }
                    }
                    // convert RW style into CSV style
                    expressions[j].text = t + " " + expressions[j].text.Substring(equals + 1);
                }
                // separate command and arguments
                string command, argumentSequence;
                SeparateCommandsAndArguments(expressions[j], out command, out argumentSequence, culture, expressions[j].file, j, false);
                // process command
                double number;
                bool numberCheck = !isRW || string.Compare(Section, "track", StringComparison.OrdinalIgnoreCase) == 0;
                if (numberCheck && Conversions.TryParseDouble(command, unitOfLength, out number))
                {
                    // track position
                    if (argumentSequence.Length != 0)
                    {
                        GD.Print("A track position must not contain any arguments at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                    }
                    else if (number < 0.0)
                    {
                        GD.Print("Negative track position encountered at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                    }
                    else
                    {
                        routeData.TrackPosition = number;
                        blockIndex = (int)Math.Floor(number / routeData.BlockInterval + 0.001);
                        if (routeData.FirstUsedBlock == -1) routeData.FirstUsedBlock = blockIndex;
                        CreateMissingBlocks(ref routeData, ref blocksUsed, blockIndex, previewOnly);
                    }
                }
                else
                {
                    // split arguments
                    string[] arguments;
                    {
                        int n = 0;
                        for (int k = 0; k < argumentSequence.Length; k++)
                        {
                            if (isRW & argumentSequence[k] == ',')
                            {
                                n++;
                            }
                            else if (argumentSequence[k] == ';')
                            {
                                n++;
                            }
                        }
                        arguments = new string[n + 1];
                        int a = 0, h = 0;
                        for (int k = 0; k < argumentSequence.Length; k++)
                        {
                            if (isRW & argumentSequence[k] == ',')
                            {
                                arguments[h] = argumentSequence.Substring(a, k - a).Trim();
                                a = k + 1; h++;
                            }
                            else if (argumentSequence[k] == ';')
                            {
                                arguments[h] = argumentSequence.Substring(a, k - a).Trim();
                                a = k + 1; h++;
                            }
                        }
                        if (argumentSequence.Length - a > 0)
                        {
                            arguments[h] = argumentSequence.Substring(a).Trim();
                            h++;
                        }
                        Array.Resize<string>(ref arguments, h);
                    }
                    // preprocess command
                    if (command.ToLowerInvariant() == "with")
                    {
                        if (arguments.Length >= 1)
                        {
                            Section = arguments[0];
                            sectionAlwaysPrefix = false;
                        }
                        else
                        {
                            Section = "";
                            sectionAlwaysPrefix = false;
                        }
                        command = null;
                    }
                    else
                    {
                        if (command.StartsWith("."))
                        {
                            command = Section + command;
                        }
                        else if (sectionAlwaysPrefix)
                        {
                            command = Section + "." + command;
                        }
                        command = command.Replace(".Void", "");
                        if (command.StartsWith("structure", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 5).TrimEnd();
                        }
                        else if (command.StartsWith("texture.background", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 5).TrimEnd();
                        }
                        else if (command.StartsWith("texture.background", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".x", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.x" + command.Substring(18, command.Length - 20).TrimEnd();
                        }
                        else if (command.StartsWith("texture.background", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".aspect", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.aspect" + command.Substring(18, command.Length - 25).TrimEnd();
                        }
                        else if (command.StartsWith("structure.back", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".x", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.x" + command.Substring(14, command.Length - 16).TrimEnd();
                        }
                        else if (command.StartsWith("structure.back", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".aspect", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "texture.background.aspect" + command.Substring(14, command.Length - 21).TrimEnd();
                        }
                        else if (command.StartsWith("cycle", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".params", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 7).TrimEnd();
                        }
                        else if (command.StartsWith("signal", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 5).TrimEnd();
                        }
                        else if (command.StartsWith("train.run", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 4).TrimEnd();
                        }
                        else if (command.StartsWith("train.flange", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 4).TrimEnd();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".day.load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.day" + command.Substring(15, command.Length - 24).Trim();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".night.load", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.night" + command.Substring(15, command.Length - 26).Trim();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".day", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.day" + command.Substring(15, command.Length - 19).Trim();
                        }
                        else if (command.StartsWith("train.timetable", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".night", StringComparison.OrdinalIgnoreCase))
                        {
                            command = "train.timetable.night" + command.Substring(15, command.Length - 21).Trim();
                        }
                        else if (command.StartsWith("route.signal", StringComparison.OrdinalIgnoreCase) & command.EndsWith(".set", StringComparison.OrdinalIgnoreCase))
                        {
                            command = command.Substring(0, command.Length - 4).TrimEnd();
                        }
                    }
                    // handle indices
                    int CommandIndex1 = 0, CommandIndex2 = 0;
                    if (command != null && command.EndsWith(")"))
                    {
                        for (int k = command.Length - 2; k >= 0; k--)
                        {
                            if (command[k] == '(')
                            {
                                string Indices = command.Substring(k + 1, command.Length - k - 2).TrimStart();
                                command = command.Substring(0, k).TrimEnd();
                                int h = Indices.IndexOf(";");
                                if (h >= 0)
                                {
                                    string a = Indices.Substring(0, h).TrimEnd();
                                    string b = Indices.Substring(h + 1).TrimStart();
                                    if (a.Length > 0 && !Conversions.TryParseIntVb6(a, out CommandIndex1))
                                    {
                                        GD.Print("Invalid first index appeared at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file + ".");
                                        command = null; break;
                                    }
                                    else if (b.Length > 0 && !Conversions.TryParseIntVb6(b, out CommandIndex2))
                                    {
                                        GD.Print("Invalid second index appeared at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file + ".");
                                        command = null; break;
                                    }
                                }
                                else
                                {
                                    if (Indices.Length > 0 && !Conversions.TryParseIntVb6(Indices, out CommandIndex1))
                                    {
                                        GD.Print("Invalid index appeared at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file + ".");
                                        command = null; break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    // process command
                    if (command != null && command.Length != 0)
                    {
                        switch (command.ToLowerInvariant())
                        {
                            // non-track
                            case "options.blocklength":
                            case "options.unitoflength":
                            case "options.unitofspeed":
                            case "options.objectvisibility":
                            case "options.sectionbehavior":
                            case "options.fogbehavior":
                            case "options.cantbehavior":
                            case "route.comment":
                            case "route.image":
                            case "route.timetable":
                            case "route.change":
                            case "route.gauge":
                            case "train.gauge":
                            case "route.signal":
                            case "route.runinterval":
                            case "train.interval":
                            case "route.accelerationduetogravity":
                            case "route.elevation":
                            case "route.temperature":
                            case "route.pressure":
                            case "route.ambientlight":
                            case "route.directionallight":
                            case "route.lightdirection":
                            case "route.developerid":
                            case "train.folder":
                            case "train.file":
                            case "train.run":
                            case "train.rail":
                            case "train.flange":
                            case "train.timetable.day":
                            case "train.timetable.night":
                            case "train.velocity":
                            case "train.acceleration":
                            case "train.station":
                            case "structure.rail":
                            case "structure.beacon":
                            case "structure.pole":
                            case "structure.ground":
                            case "structure.walll":
                            case "structure.wallr":
                            case "structure.dikel":
                            case "structure.diker":
                            case "structure.forml":
                            case "structure.formr":
                            case "structure.formcl":
                            case "structure.formcr":
                            case "structure.roofl":
                            case "structure.roofr":
                            case "structure.roofcl":
                            case "structure.roofcr":
                            case "structure.crackl":
                            case "structure.crackr":
                            case "structure.freeobj":
                            case "signal":
                            case "texture.background":
                            case "structure.back":
                            case "structure.back.x":
                            case "structure.back.aspect":
                            case "texture.background.x":
                            case "texture.background.aspect":
                            case "cycle.ground":
                                break;
                            // track
                            case "track.railstart":
                            case "track.rail":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 1)
                                        {
                                            GD.Print("RailIndex is expected to be positive in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (string.Compare(command, "track.railstart", StringComparison.OrdinalIgnoreCase) == 0)
                                            {
                                                if (idx < routeData.Blocks[blockIndex].Rail.Length && routeData.Blocks[blockIndex].Rail[idx].RailStart)
                                                {
                                                    GD.Print("RailIndex is required to reference a non-existing rail in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                            }
                                            if (routeData.Blocks[blockIndex].Rail.Length <= idx)
                                            {
                                                Array.Resize<Rail>(ref routeData.Blocks[blockIndex].Rail, idx + 1);
                                            }
                                            if (routeData.Blocks[blockIndex].Rail[idx].RailStartRefreshed)
                                            {
                                                routeData.Blocks[blockIndex].Rail[idx].RailEnd = true;
                                            }
                                            {
                                                routeData.Blocks[blockIndex].Rail[idx].RailStart = true;
                                                routeData.Blocks[blockIndex].Rail[idx].RailStartRefreshed = true;
                                                if (arguments.Length >= 2)
                                                {
                                                    if (arguments[1].Length > 0)
                                                    {
                                                        double x;
                                                        if (!Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out x))
                                                        {
                                                            GD.Print("X is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                            x = 0.0;
                                                        }
                                                        routeData.Blocks[blockIndex].Rail[idx].RailStartX = x;
                                                    }
                                                    if (!routeData.Blocks[blockIndex].Rail[idx].RailEnd)
                                                    {
                                                        routeData.Blocks[blockIndex].Rail[idx].RailEndX = routeData.Blocks[blockIndex].Rail[idx].RailStartX;
                                                    }
                                                }
                                                if (arguments.Length >= 3)
                                                {
                                                    if (arguments[2].Length > 0)
                                                    {
                                                        double y;
                                                        if (!Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out y))
                                                        {
                                                            GD.Print("Y is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                            y = 0.0;
                                                        }
                                                        routeData.Blocks[blockIndex].Rail[idx].RailStartY = y;
                                                    }
                                                    if (!routeData.Blocks[blockIndex].Rail[idx].RailEnd)
                                                    {
                                                        routeData.Blocks[blockIndex].Rail[idx].RailEndY = routeData.Blocks[blockIndex].Rail[idx].RailStartY;
                                                    }
                                                }
                                                if (routeData.Blocks[blockIndex].RailType.Length <= idx)
                                                {
                                                    Array.Resize<int>(ref routeData.Blocks[blockIndex].RailType, idx + 1);
                                                }
                                                if (arguments.Length >= 4 && arguments[3].Length != 0)
                                                {
                                                    int sttype;
                                                    if (!Conversions.TryParseIntVb6(arguments[3], out sttype))
                                                    {
                                                        GD.Print("RailStructureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                        sttype = 0;
                                                    }
                                                    if (sttype < 0)
                                                    {
                                                        GD.Print("RailStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else if (sttype >= routeData.Structure.Rail.Length || routeData.Structure.Rail[sttype] == null)
                                                    {
                                                        GD.Print("RailStructureIndex references an object not loaded in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    }
                                                    else
                                                    {
                                                        routeData.Blocks[blockIndex].RailType[idx] = sttype;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.railend":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0 || idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart)
                                        {
                                            GD.Print("RailIndex references a non-existing rail in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (routeData.Blocks[blockIndex].RailType.Length <= idx)
                                            {
                                                Array.Resize<Rail>(ref routeData.Blocks[blockIndex].Rail, idx + 1);
                                            }
                                            routeData.Blocks[blockIndex].Rail[idx].RailStart = false;
                                            routeData.Blocks[blockIndex].Rail[idx].RailStartRefreshed = false;
                                            routeData.Blocks[blockIndex].Rail[idx].RailEnd = true;
                                            if (arguments.Length >= 2 && arguments[1].Length > 0)
                                            {
                                                double x;
                                                if (!Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out x))
                                                {
                                                    GD.Print("X is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    x = 0.0;
                                                }
                                                routeData.Blocks[blockIndex].Rail[idx].RailEndX = x;
                                            }
                                            if (arguments.Length >= 3 && arguments[2].Length > 0)
                                            {
                                                double y;
                                                if (!Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out y))
                                                {
                                                    GD.Print("Y is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    y = 0.0;
                                                }
                                                routeData.Blocks[blockIndex].Rail[idx].RailEndY = y;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.railtype":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out sttype))
                                        {
                                            GD.Print("RailStructureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            GD.Print("RailIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart)
                                            {
                                                GD.Print("RailIndex could be out of range in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            if (sttype < 0)
                                            {
                                                GD.Print("RailStructureIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (sttype >= routeData.Structure.Rail.Length || routeData.Structure.Rail[sttype] == null)
                                            {
                                                GD.Print("RailStructureIndex references an object not loaded in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (routeData.Blocks[blockIndex].RailType.Length <= idx)
                                                {
                                                    Array.Resize<int>(ref routeData.Blocks[blockIndex].RailType, idx + 1);
                                                }
                                                routeData.Blocks[blockIndex].RailType[idx] = sttype;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.accuracy":
                                {
                                    double r = 2.0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out r))
                                    {
                                        GD.Print("Value is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        r = 2.0;
                                    }
                                    if (r < 0.0)
                                    {
                                        r = 0.0;
                                    }
                                    else if (r > 4.0)
                                    {
                                        r = 4.0;
                                    }
                                    routeData.Blocks[blockIndex].Accuracy = r;
                                }
                                break;
                            case "track.pitch":
                                {
                                    double p = 0.0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out p))
                                    {
                                        GD.Print("ValueInPermille is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        p = 0.0;
                                    }
                                    routeData.Blocks[blockIndex].Pitch = 0.001 * p;
                                }
                                break;
                            case "track.curve":
                                {
                                    double radius = 0.0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], unitOfLength, out radius))
                                    {
                                        GD.Print("Radius is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        radius = 0.0;
                                    }
                                    double cant = 0.0;
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out cant))
                                    {
                                        GD.Print("CantInMillimeters is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        cant = 0.0;
                                    }
                                    else
                                    {
                                        cant *= 0.001;
                                    }
                                    if (routeData.SignedCant)
                                    {
                                        if (radius != 0.0)
                                        {
                                            cant *= (double)Math.Sign(radius);
                                        }
                                    }
                                    else
                                    {
                                        cant = Math.Abs(cant) * (double)Math.Sign(radius);
                                    }
                                    routeData.Blocks[blockIndex].CurrentTrackState.CurveRadius = radius;
                                    routeData.Blocks[blockIndex].CurrentTrackState.CurveCant = cant;
                                    routeData.Blocks[blockIndex].CurrentTrackState.CurveCantTangent = 0.0;
                                }
                                break;
                            case "track.turn":
                                {
                                    double s = 0.0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out s))
                                    {
                                        GD.Print("Ratio is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        s = 0.0;
                                    }
                                    routeData.Blocks[blockIndex].Turn = s;
                                }
                                break;
                            case "track.adhesion":
                                {
                                    double a = 100.0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        GD.Print("Value is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        a = 100.0;
                                    }
                                    if (a < 0.0)
                                    {
                                        GD.Print("Value is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        a = 100.0;
                                    }
                                    routeData.Blocks[blockIndex].AdhesionMultiplier = 0.01 * a;
                                }
                                break;
                            case "track.brightness":
                                {
                                    if (!previewOnly)
                                    {
                                        float value = 255.0f;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out value))
                                        {
                                            GD.Print("Value is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            value = 255.0f;
                                        }
                                        value /= 255.0f;
                                        if (value < 0.0f) value = 0.0f;
                                        if (value > 1.0f) value = 1.0f;
                                        int n = routeData.Blocks[blockIndex].Brightness.Length;
                                        Array.Resize<Brightness>(ref routeData.Blocks[blockIndex].Brightness, n + 1);
                                        routeData.Blocks[blockIndex].Brightness[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Brightness[n].Value = value;
                                    }
                                }
                                break;
                            case "track.fog":
                                {
                                    if (!previewOnly)
                                    {
                                        double start = 0.0, end = 0.0;
                                        int r = 128, g = 128, b = 128;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out start))
                                        {
                                            GD.Print("StartingDistance is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            start = 0.0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out end))
                                        {
                                            GD.Print("EndingDistance is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            end = 0.0;
                                        }
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out r))
                                        {
                                            GD.Print("RedValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            r = 128;
                                        }
                                        else if (r < 0 | r > 255)
                                        {
                                            GD.Print("RedValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            r = r < 0 ? 0 : 255;
                                        }
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out g))
                                        {
                                            GD.Print("GreenValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            g = 128;
                                        }
                                        else if (g < 0 | g > 255)
                                        {
                                            GD.Print("GreenValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            g = g < 0 ? 0 : 255;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseIntVb6(arguments[4], out b))
                                        {
                                            GD.Print("BlueValue is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            b = 128;
                                        }
                                        else if (b < 0 | b > 255)
                                        {
                                            GD.Print("BlueValue is required to be within the range from 0 to 255 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            b = b < 0 ? 0 : 255;
                                        }
                                        //if (start < end)
                                        //{
                                        //    data.Blocks[blockIndex].Fog.Start = (float)start;
                                        //    data.Blocks[blockIndex].Fog.End = (float)end;
                                        //}
                                        //else
                                        //{
                                        //    data.Blocks[blockIndex].Fog.Start = Game.NoFogStart;
                                        //    data.Blocks[blockIndex].Fog.End = Game.NoFogEnd;
                                        //}
                                        //data.Blocks[blockIndex].Fog.Color = new Color24((byte)r, (byte)g, (byte)b);
                                        //data.Blocks[blockIndex].FogDefined = true;
                                    }
                                }
                                break;
                            case "track.section":
                            case "track.sections":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length == 0)
                                        {
                                            GD.Print("At least one argument is required in " + command + "at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            int[] aspects = new int[arguments.Length];
                                            for (int i = 0; i < arguments.Length; i++)
                                            {
                                                if (!Conversions.TryParseIntVb6(arguments[i], out aspects[i]))
                                                {
                                                    GD.Print("Aspect" + i.ToString(culture) + " is invalid in " + command + "at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    aspects[i] = -1;
                                                }
                                                else if (aspects[i] < 0)
                                                {
                                                    GD.Print("Aspect" + i.ToString(culture) + " is expected to be non-negative in " + command + "at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    aspects[i] = -1;
                                                }
                                            }
                                            bool valueBased = valueBasedSection | string.Equals(command, "Track.SectionS", StringComparison.OrdinalIgnoreCase);
                                            if (valueBased)
                                            {
                                                Array.Sort<int>(aspects);
                                            }
                                            int n = routeData.Blocks[blockIndex].Section.Length;
                                            Array.Resize<Section>(ref routeData.Blocks[blockIndex].Section, n + 1);
                                            routeData.Blocks[blockIndex].Section[n].TrackPosition = routeData.TrackPosition;
                                            routeData.Blocks[blockIndex].Section[n].Aspects = aspects;
                                            //data.Blocks[blockIndex].Section[n].Type = valueBased ? Game.SectionType.ValueBased : Game.SectionType.IndexBased;
                                            routeData.Blocks[blockIndex].Section[n].DepartureStationIndex = -1;
                                            //if (currentStation >= 0 && Game.Stations[currentStation].ForceStopSignal)
                                            //{
                                            //    if (currentStation >= 0 & currentStop >= 0 & !departureSignalUsed)
                                            //    {
                                            //        data.Blocks[blockIndex].Section[n].DepartureStationIndex = currentStation;
                                            //        departureSignalUsed = true;
                                            //    }
                                            //}
                                            currentSection++;
                                        }
                                    }
                                }
                                break;
                            case "track.sigf":
                                {
                                    if (!previewOnly)
                                    {
                                        int objidx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out objidx))
                                        {
                                            GD.Print("SignalIndex is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            objidx = 0;
                                        }
                                        if (objidx >= 0 & objidx < routeData.SignalData.Length && routeData.SignalData[objidx] != null)
                                        {
                                            int section = 0;
                                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out section))
                                            {
                                                GD.Print("Section is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                section = 0;
                                            }
                                            double x = 0.0, y = 0.0;
                                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out x))
                                            {
                                                GD.Print("X is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                x = 0.0;
                                            }
                                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], unitOfLength, out y))
                                            {
                                                GD.Print("Y is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                y = 0.0;
                                            }
                                            double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                            if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out yaw))
                                            {
                                                GD.Print("Yaw is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                yaw = 0.0;
                                            }
                                            if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out pitch))
                                            {
                                                GD.Print("Pitch is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                pitch = 0.0;
                                            }
                                            if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out roll))
                                            {
                                                GD.Print("Roll is invalid in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                roll = 0.0;
                                            }
                                            int n = routeData.Blocks[blockIndex].Signal.Length;
                                            Array.Resize<Signal>(ref routeData.Blocks[blockIndex].Signal, n + 1);
                                            routeData.Blocks[blockIndex].Signal[n].TrackPosition = routeData.TrackPosition;
                                            routeData.Blocks[blockIndex].Signal[n].Section = currentSection + section;
                                            routeData.Blocks[blockIndex].Signal[n].SignalCompatibilityObjectIndex = -1;
                                            routeData.Blocks[blockIndex].Signal[n].SignalObjectIndex = objidx;
                                            routeData.Blocks[blockIndex].Signal[n].X = x;
                                            routeData.Blocks[blockIndex].Signal[n].Y = y < 0.0 ? 4.8 : y;
                                            routeData.Blocks[blockIndex].Signal[n].Yaw = 0.0174532925199433 * yaw;
                                            routeData.Blocks[blockIndex].Signal[n].Pitch = 0.0174532925199433 * pitch;
                                            routeData.Blocks[blockIndex].Signal[n].Roll = 0.0174532925199433 * roll;
                                            routeData.Blocks[blockIndex].Signal[n].ShowObject = true;
                                            routeData.Blocks[blockIndex].Signal[n].ShowPost = y < 0.0;
                                            routeData.Blocks[blockIndex].Signal[n].GameSignalIndex = -1;
                                        }
                                        else
                                        {
                                            GD.Print("SignalIndex references a signal object not loaded in Track.SigF at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                    }
                                }
                                break;
                            case "track.signal":
                            case "track.sig":
                                {
                                    if (!previewOnly)
                                    {
                                        int num = -2;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out num))
                                        {
                                            GD.Print("Aspects is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            num = -2;
                                        }
                                        if (num != -2 & num != 2 & num != 3 & num != -4 & num != 4 & num != -5 & num != 5 & num != 6)
                                        {
                                            GD.Print("Aspects has an unsupported value in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            num = num == -3 | num == -6 ? -num : -4;
                                        }
                                        double x = 0.0, y = 0.0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out x))
                                        {
                                            GD.Print("X is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            x = 0.0;
                                        }
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], unitOfLength, out y))
                                        {
                                            GD.Print("Y is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            y = 0.0;
                                        }
                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out yaw))
                                        {
                                            GD.Print("Yaw is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out pitch))
                                        {
                                            GD.Print("Pitch is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out roll))
                                        {
                                            GD.Print("Roll is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            roll = 0.0;
                                        }
                                        int[] aspects; int comp;
                                        switch (num)
                                        {
                                            case 2: aspects = new int[] { 0, 2 }; comp = 0; break;
                                            case -2: aspects = new int[] { 0, 4 }; comp = 1; break;
                                            case 3: aspects = new int[] { 0, 2, 4 }; comp = 2; break;
                                            case 4: aspects = new int[] { 0, 1, 2, 4 }; comp = 3; break;
                                            case -4: aspects = new int[] { 0, 2, 3, 4 }; comp = 4; break;
                                            case 5: aspects = new int[] { 0, 1, 2, 3, 4 }; comp = 5; break;
                                            case -5: aspects = new int[] { 0, 2, 3, 4, 5 }; comp = 6; break;
                                            case 6: aspects = new int[] { 0, 1, 2, 3, 4, 5 }; comp = 7; break;
                                            default: aspects = new int[] { 0, 2 }; comp = 0; break;
                                        }
                                        int n = routeData.Blocks[blockIndex].Section.Length;
                                        Array.Resize<Section>(ref routeData.Blocks[blockIndex].Section, n + 1);
                                        routeData.Blocks[blockIndex].Section[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Section[n].Aspects = aspects;
                                        routeData.Blocks[blockIndex].Section[n].DepartureStationIndex = -1;
                                        routeData.Blocks[blockIndex].Section[n].Invisible = x == 0.0;
                                        //data.Blocks[blockIndex].Section[n].Type = Game.SectionType.ValueBased;
                                        //if (currentStation >= 0 && Game.Stations[currentStation].ForceStopSignal)
                                        //{
                                        //    if (currentStation >= 0 & currentStop >= 0 & !departureSignalUsed)
                                        //    {
                                        //        data.Blocks[blockIndex].Section[n].DepartureStationIndex = currentStation;
                                        //        departureSignalUsed = true;
                                        //    }
                                        //}
                                        currentSection++;
                                        n = routeData.Blocks[blockIndex].Signal.Length;
                                        Array.Resize<Signal>(ref routeData.Blocks[blockIndex].Signal, n + 1);
                                        routeData.Blocks[blockIndex].Signal[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Signal[n].Section = currentSection;
                                        routeData.Blocks[blockIndex].Signal[n].SignalCompatibilityObjectIndex = comp;
                                        routeData.Blocks[blockIndex].Signal[n].SignalObjectIndex = -1;
                                        routeData.Blocks[blockIndex].Signal[n].X = x;
                                        routeData.Blocks[blockIndex].Signal[n].Y = y < 0.0 ? 4.8 : y;
                                        routeData.Blocks[blockIndex].Signal[n].Yaw = 0.0174532925199433 * yaw;
                                        routeData.Blocks[blockIndex].Signal[n].Pitch = 0.0174532925199433 * pitch;
                                        routeData.Blocks[blockIndex].Signal[n].Roll = 0.0174532925199433 * roll;
                                        routeData.Blocks[blockIndex].Signal[n].ShowObject = x != 0.0;
                                        routeData.Blocks[blockIndex].Signal[n].ShowPost = x != 0.0 & y < 0.0;
                                        routeData.Blocks[blockIndex].Signal[n].GameSignalIndex = -1;
                                    }
                                }
                                break;
                            case "track.relay":
                                {
                                    if (!previewOnly)
                                    {
                                        double x = 0.0, y = 0.0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], unitOfLength, out x))
                                        {
                                            GD.Print("X is invalid in Track.Relay at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            x = 0.0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out y))
                                        {
                                            GD.Print("Y is invalid in Track.Relay at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            y = 0.0;
                                        }
                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out yaw))
                                        {
                                            GD.Print("Yaw is invalid in Track.Relay at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out pitch))
                                        {
                                            GD.Print("Pitch is invalid in Track.Relay at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out roll))
                                        {
                                            GD.Print("Roll is invalid in Track.Relay at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            roll = 0.0;
                                        }
                                        int n = routeData.Blocks[blockIndex].Signal.Length;
                                        Array.Resize<Signal>(ref routeData.Blocks[blockIndex].Signal, n + 1);
                                        routeData.Blocks[blockIndex].Signal[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Signal[n].Section = currentSection + 1;
                                        routeData.Blocks[blockIndex].Signal[n].SignalCompatibilityObjectIndex = 8;
                                        routeData.Blocks[blockIndex].Signal[n].SignalObjectIndex = -1;
                                        routeData.Blocks[blockIndex].Signal[n].X = x;
                                        routeData.Blocks[blockIndex].Signal[n].Y = y < 0.0 ? 4.8 : y;
                                        routeData.Blocks[blockIndex].Signal[n].Yaw = yaw * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Signal[n].Pitch = pitch * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Signal[n].Roll = roll * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Signal[n].ShowObject = x != 0.0;
                                        routeData.Blocks[blockIndex].Signal[n].ShowPost = x != 0.0 & y < 0.0;
                                    }
                                }
                                break;
                            case "track.beacon":
                                {
                                    if (!previewOnly)
                                    {
                                        int type = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out type))
                                        {
                                            GD.Print("Type is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            type = 0;
                                        }
                                        if (type < 0)
                                        {
                                            GD.Print("Type is expected to be non-positive in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            int structure = 0, section = 0, optional = 0;
                                            if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out structure))
                                            {
                                                GD.Print("BeaconStructureIndex is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                structure = 0;
                                            }
                                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out section))
                                            {
                                                GD.Print("Section is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                section = 0;
                                            }
                                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out optional))
                                            {
                                                GD.Print("Data is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                optional = 0;
                                            }
                                            if (structure < -1)
                                            {
                                                GD.Print("BeaconStructureIndex is expected to be non-negative or -1 in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                structure = -1;
                                            }
                                            else if (structure >= 0 && (structure >= routeData.Structure.Beacon.Length || routeData.Structure.Beacon[structure] == null))
                                            {
                                                GD.Print("BeaconStructureIndex references an object not loaded in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                structure = -1;
                                            }
                                            if (section == -1)
                                            {
                                                //section = (int)TrackManager.TransponderSpecialSection.NextRedSection;
                                            }
                                            else if (section < 0)
                                            {
                                                GD.Print("Section is expected to be non-negative or -1 in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                section = currentSection + 1;
                                            }
                                            else
                                            {
                                                section += currentSection;
                                            }
                                            double x = 0.0, y = 0.0;
                                            double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                            if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], unitOfLength, out x))
                                            {
                                                GD.Print("X is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                x = 0.0;
                                            }
                                            if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], unitOfLength, out y))
                                            {
                                                GD.Print("Y is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                y = 0.0;
                                            }
                                            if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out yaw))
                                            {
                                                GD.Print("Yaw is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                yaw = 0.0;
                                            }
                                            if (arguments.Length >= 8 && arguments[7].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[7], out pitch))
                                            {
                                                GD.Print("Pitch is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                pitch = 0.0;
                                            }
                                            if (arguments.Length >= 9 && arguments[8].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[8], out roll))
                                            {
                                                GD.Print("Roll is invalid in Track.Beacon at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                roll = 0.0;
                                            }
                                            int n = routeData.Blocks[blockIndex].Transponder.Length;
                                            Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponder, n + 1);
                                            routeData.Blocks[blockIndex].Transponder[n].TrackPosition = routeData.TrackPosition;
                                            routeData.Blocks[blockIndex].Transponder[n].Type = type;
                                            routeData.Blocks[blockIndex].Transponder[n].Data = optional;
                                            routeData.Blocks[blockIndex].Transponder[n].BeaconStructureIndex = structure;
                                            routeData.Blocks[blockIndex].Transponder[n].Section = section;
                                            routeData.Blocks[blockIndex].Transponder[n].ShowDefaultObject = false;
                                            routeData.Blocks[blockIndex].Transponder[n].X = x;
                                            routeData.Blocks[blockIndex].Transponder[n].Y = y;
                                            routeData.Blocks[blockIndex].Transponder[n].Yaw = yaw * 0.0174532925199433;
                                            routeData.Blocks[blockIndex].Transponder[n].Pitch = pitch * 0.0174532925199433;
                                            routeData.Blocks[blockIndex].Transponder[n].Roll = roll * 0.0174532925199433;
                                        }
                                    }
                                }
                                break;
                            case "track.transponder":
                            case "track.tr":
                                {
                                    if (!previewOnly)
                                    {
                                        int type = 0, oversig = 0, work = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out type))
                                        {
                                            GD.Print("Type is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            type = 0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out oversig))
                                        {
                                            GD.Print("Signals is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            oversig = 0;
                                        }
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out work))
                                        {
                                            GD.Print("SwitchSystems is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            work = 0;
                                        }
                                        if (oversig < 0)
                                        {
                                            GD.Print("Signals is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            oversig = 0;
                                        }
                                        double x = 0.0, y = 0.0;
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], unitOfLength, out x))
                                        {
                                            GD.Print("X is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            x = 0.0;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], unitOfLength, out y))
                                        {
                                            GD.Print("Y is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            y = 0.0;
                                        }
                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out yaw))
                                        {
                                            GD.Print("Yaw is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out pitch))
                                        {
                                            GD.Print("Pitch is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 8 && arguments[7].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[7], out roll))
                                        {
                                            GD.Print("Roll is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            roll = 0.0;
                                        }
                                        int n = routeData.Blocks[blockIndex].Transponder.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponder, n + 1);
                                        routeData.Blocks[blockIndex].Transponder[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Transponder[n].Type = type;
                                        routeData.Blocks[blockIndex].Transponder[n].Data = work;
                                        routeData.Blocks[blockIndex].Transponder[n].ShowDefaultObject = true;
                                        routeData.Blocks[blockIndex].Transponder[n].BeaconStructureIndex = -1;
                                        routeData.Blocks[blockIndex].Transponder[n].X = x;
                                        routeData.Blocks[blockIndex].Transponder[n].Y = y;
                                        routeData.Blocks[blockIndex].Transponder[n].Yaw = yaw * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Transponder[n].Pitch = pitch * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Transponder[n].Roll = roll * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Transponder[n].Section = currentSection + oversig + 1;
                                        routeData.Blocks[blockIndex].Transponder[n].ClipToFirstRedSection = true;
                                    }
                                }
                                break;
                            case "track.atssn":
                                {
                                    if (!previewOnly)
                                    {
                                        int n = routeData.Blocks[blockIndex].Transponder.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponder, n + 1);
                                        routeData.Blocks[blockIndex].Transponder[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Transponder[n].Type = 0;
                                        routeData.Blocks[blockIndex].Transponder[n].Data = 0;
                                        routeData.Blocks[blockIndex].Transponder[n].ShowDefaultObject = true;
                                        routeData.Blocks[blockIndex].Transponder[n].BeaconStructureIndex = -1;
                                        routeData.Blocks[blockIndex].Transponder[n].Section = currentSection + 1;
                                        routeData.Blocks[blockIndex].Transponder[n].ClipToFirstRedSection = true;
                                    }
                                }
                                break;
                            case "track.atsp":
                                {
                                    if (!previewOnly)
                                    {
                                        int n = routeData.Blocks[blockIndex].Transponder.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponder, n + 1);
                                        routeData.Blocks[blockIndex].Transponder[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Transponder[n].Type = 3;
                                        routeData.Blocks[blockIndex].Transponder[n].Data = 0;
                                        routeData.Blocks[blockIndex].Transponder[n].ShowDefaultObject = true;
                                        routeData.Blocks[blockIndex].Transponder[n].BeaconStructureIndex = -1;
                                        routeData.Blocks[blockIndex].Transponder[n].Section = currentSection + 1;
                                        routeData.Blocks[blockIndex].Transponder[n].ClipToFirstRedSection = true;
                                    }
                                }
                                break;
                            case "track.pattern":
                                {
                                    if (!previewOnly)
                                    {
                                        int type = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out type))
                                        {
                                            GD.Print("Type is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            type = 0;
                                        }
                                        double speed = 0.0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out speed))
                                        {
                                            GD.Print("Speed is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            speed = 0.0;
                                        }
                                        int n = routeData.Blocks[blockIndex].Transponder.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponder, n + 1);
                                        routeData.Blocks[blockIndex].Transponder[n].TrackPosition = routeData.TrackPosition;
                                        //if (type == 0)
                                        //{
                                        //    data.Blocks[blockIndex].Transponder[n].Type = TrackManager.SpecialTransponderTypes.InternalAtsPTemporarySpeedLimit;
                                        //    data.Blocks[blockIndex].Transponder[n].Data = speed == 0.0 ? int.MaxValue : (int)Math.Round(speed * data.UnitOfSpeed * 3.6);
                                        //}
                                        //else
                                        //{
                                        //    data.Blocks[blockIndex].Transponder[n].Type = TrackManager.SpecialTransponderTypes.AtsPPermanentSpeedLimit;
                                        //    data.Blocks[blockIndex].Transponder[n].Data = speed == 0.0 ? int.MaxValue : (int)Math.Round(speed * data.UnitOfSpeed * 3.6);
                                        //}
                                        routeData.Blocks[blockIndex].Transponder[n].Section = -1;
                                        routeData.Blocks[blockIndex].Transponder[n].BeaconStructureIndex = -1;
                                    }
                                }
                                break;
                            case "track.plimit":
                                {
                                    //if (!previewOnly)
                                    //{
                                    //    double speed = 0.0;
                                    //    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out speed))
                                    //    {
                                    //         GD.Print("Speed is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    //        speed = 0.0;
                                    //    }
                                    //    int n = data.Blocks[blockIndex].Transponder.Length;
                                    //    Array.Resize<Transponder>(ref data.Blocks[blockIndex].Transponder, n + 1);
                                    //    data.Blocks[blockIndex].Transponder[n].TrackPosition = data.TrackPosition;
                                    //    data.Blocks[blockIndex].Transponder[n].Type = TrackManager.SpecialTransponderTypes.AtsPPermanentSpeedLimit;
                                    //    data.Blocks[blockIndex].Transponder[n].Data = speed == 0.0 ? int.MaxValue : (int)Math.Round(speed * data.UnitOfSpeed * 3.6);
                                    //    data.Blocks[blockIndex].Transponder[n].Section = -1;
                                    //    data.Blocks[blockIndex].Transponder[n].BeaconStructureIndex = -1;
                                    //}
                                }
                                break;
                            case "track.limit":
                                {
                                    double limit = 0.0;
                                    int direction = 0, cource = 0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], out limit))
                                    {
                                        GD.Print("Speed is invalid in Track.Limit at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        limit = 0.0;
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out direction))
                                    {
                                        GD.Print("Direction is invalid in Track.Limit at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        direction = 0;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out cource))
                                    {
                                        GD.Print("Cource is invalid in Track.Limit at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        cource = 0;
                                    }
                                    int n = routeData.Blocks[blockIndex].Limit.Length;
                                    Array.Resize<Limit>(ref routeData.Blocks[blockIndex].Limit, n + 1);
                                    routeData.Blocks[blockIndex].Limit[n].TrackPosition = routeData.TrackPosition;
                                    routeData.Blocks[blockIndex].Limit[n].Speed = limit <= 0.0 ? double.PositiveInfinity : routeData.UnitOfSpeed * limit;
                                    routeData.Blocks[blockIndex].Limit[n].Direction = direction;
                                    routeData.Blocks[blockIndex].Limit[n].Cource = cource;
                                }
                                break;
                            case "track.stop":
                                if (currentStation == -1)
                                {
                                    GD.Print("A stop without a station is invalid in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                }
                                else
                                {
                                    int dir = 0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out dir))
                                    {
                                        GD.Print("Direction is invalid in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        dir = 0;
                                    }
                                    double backw = 5.0, forw = 5.0;
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out backw))
                                    {
                                        GD.Print("BackwardTolerance is invalid in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        backw = 5.0;
                                    }
                                    else if (backw <= 0.0)
                                    {
                                        GD.Print("BackwardTolerance is expected to be positive in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        backw = 5.0;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out forw))
                                    {
                                        GD.Print("ForwardTolerance is invalid in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        forw = 5.0;
                                    }
                                    else if (forw <= 0.0)
                                    {
                                        GD.Print("ForwardTolerance is expected to be positive in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        forw = 5.0;
                                    }
                                    int cars = 0;
                                    if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out cars))
                                    {
                                        GD.Print("Cars is invalid in Track.Stop at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        cars = 0;
                                    }
                                    int n = routeData.Blocks[blockIndex].Stop.Length;
                                    Array.Resize<Stop>(ref routeData.Blocks[blockIndex].Stop, n + 1);
                                    routeData.Blocks[blockIndex].Stop[n].TrackPosition = routeData.TrackPosition;
                                    routeData.Blocks[blockIndex].Stop[n].Station = currentStation;
                                    routeData.Blocks[blockIndex].Stop[n].Direction = dir;
                                    routeData.Blocks[blockIndex].Stop[n].ForwardTolerance = forw;
                                    routeData.Blocks[blockIndex].Stop[n].BackwardTolerance = backw;
                                    routeData.Blocks[blockIndex].Stop[n].Cars = cars;
                                    currentStop = cars;
                                }
                                break;
                            //case "track.sta":
                            //    {
                            //    currentStation++;
                            //    Array.Resize<Game.Station>(ref Game.Stations, currentStation + 1);
                            //    Game.Stations[currentStation].Name = string.Empty;
                            //    Game.Stations[currentStation].StopMode = Game.StationStopMode.AllStop;
                            //    Game.Stations[currentStation].StationType = Game.StationType.Normal;
                            //    if (arguments.Length >= 1 && arguments[0].Length > 0)
                            //    {
                            //        Game.Stations[currentStation].Name = arguments[0];
                            //    }
                            //    double arr = -1.0, dep = -1.0;
                            //    if (arguments.Length >= 2 && arguments[1].Length > 0)
                            //    {
                            //        if (string.Equals(arguments[1], "P", StringComparison.OrdinalIgnoreCase) | string.Equals(arguments[1], "L", StringComparison.OrdinalIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StopMode = Game.StationStopMode.AllPass;
                            //        }
                            //        else if (string.Equals(arguments[1], "B", StringComparison.OrdinalIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerPass;
                            //        }
                            //        else if (arguments[1].StartsWith("B:", StringComparison.InvariantCultureIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerPass;
                            //            if (!Conversions.TryParseTime(arguments[1].Substring(2).TrimStart(), out arr))
                            //            {
                            //                 GD.Print("ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                arr = -1.0;
                            //            }
                            //        }
                            //        else if (string.Equals(arguments[1], "S", StringComparison.OrdinalIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerStop;
                            //        }
                            //        else if (arguments[1].StartsWith("S:", StringComparison.InvariantCultureIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerStop;
                            //            if (!Conversions.TryParseTime(arguments[1].Substring(2).TrimStart(), out arr))
                            //            {
                            //                 GD.Print("ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                arr = -1.0;
                            //            }
                            //        }
                            //        else if (!Conversions.TryParseTime(arguments[1], out arr))
                            //        {
                            //             GD.Print("ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            arr = -1.0;
                            //        }
                            //    }
                            //    if (arguments.Length >= 3 && arguments[2].Length > 0)
                            //    {
                            //        if (string.Equals(arguments[2], "T", StringComparison.OrdinalIgnoreCase) | string.Equals(arguments[2], "=", StringComparison.OrdinalIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StationType = Game.StationType.Terminal;
                            //        }
                            //        else if (arguments[2].StartsWith("T:", StringComparison.InvariantCultureIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StationType = Game.StationType.Terminal;
                            //            if (!Conversions.TryParseTime(arguments[2].Substring(2).TrimStart(), out dep))
                            //            {
                            //                 GD.Print("DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                dep = -1.0;
                            //            }
                            //        }
                            //        else if (string.Equals(arguments[2], "C", StringComparison.OrdinalIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StationType = Game.StationType.ChangeEnds;
                            //        }
                            //        else if (arguments[2].StartsWith("C:", StringComparison.InvariantCultureIgnoreCase))
                            //        {
                            //            Game.Stations[currentStation].StationType = Game.StationType.ChangeEnds;
                            //            if (!Conversions.TryParseTime(arguments[2].Substring(2).TrimStart(), out dep))
                            //            {
                            //                 GD.Print("DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                dep = -1.0;
                            //            }
                            //        }
                            //        else if (!Conversions.TryParseTime(arguments[2], out dep))
                            //        {
                            //             GD.Print("DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            dep = -1.0;
                            //        }
                            //    }
                            //    int passalarm = 0;
                            //    if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out passalarm))
                            //    {
                            //         GD.Print("PassAlarm is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //        passalarm = 0;
                            //    }
                            //    int door = 0;
                            //    bool doorboth = false;
                            //    if (arguments.Length >= 5 && arguments[4].Length != 0)
                            //    {
                            //        switch (arguments[4].ToUpperInvariant())
                            //        {
                            //            case "L":
                            //                door = -1;
                            //                break;
                            //            case "R":
                            //                door = 1;
                            //                break;
                            //            case "N":
                            //                door = 0;
                            //                break;
                            //            case "B":
                            //                doorboth = true;
                            //                break;
                            //            default:
                            //                if (!Conversions.TryParseIntVb6(arguments[4], out door))
                            //                {
                            //                     GD.Print("Doors is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    door = 0;
                            //                }
                            //                break;
                            //        }
                            //    }
                            //    int stop = 0;
                            //    if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseIntVb6(arguments[5], out stop))
                            //    {
                            //         GD.Print("ForcedRedSignal is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //        stop = 0;
                            //    }
                            //    int device = 0;
                            //    if (arguments.Length >= 7 && arguments[6].Length > 0)
                            //    {
                            //        if (string.Compare(arguments[6], "ats", StringComparison.OrdinalIgnoreCase) == 0)
                            //        {
                            //            device = 0;
                            //        }
                            //        else if (string.Compare(arguments[6], "atc", StringComparison.OrdinalIgnoreCase) == 0)
                            //        {
                            //            device = 1;
                            //        }
                            //        else if (!Conversions.TryParseIntVb6(arguments[6], out device))
                            //        {
                            //             GD.Print("System is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            device = 0;
                            //        }
                            //        if (device != 0 & device != 1)
                            //        {
                            //             GD.Print("System is not supported in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            device = 0;
                            //        }
                            //    }
                            //    Sounds.SoundBuffer arrsnd = null;
                            //    Sounds.SoundBuffer depsnd = null;
                            //    if (!previewOnly)
                            //    {
                            //        if (arguments.Length >= 8 && arguments[7].Length > 0)
                            //        {
                            //            if (arguments[7].LastIndexOfAny(Path.GetInvalidPathChars()) >= 0)
                            //            {
                            //                 GD.Print("ArrivalSound contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            }
                            //            else
                            //            {
                            //                string f = System.IO.Path.Combine(soundPath, arguments[7]);
                            //                if (!File.Exists(f))
                            //                {
                            //                     GD.Print("ArrivalSound " + f + " not found in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                }
                            //                else
                            //                {
                            //                    const double radius = 30.0;
                            //                    arrsnd = Sounds.RegisterBuffer(f, radius);
                            //                }
                            //            }
                            //        }
                            //    }
                            //    double halt = 15.0;
                            //    if (arguments.Length >= 9 && arguments[8].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[8], out halt))
                            //    {
                            //         GD.Print("StopDuration is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //        halt = 15.0;
                            //    }
                            //    else if (halt < 5.0)
                            //    {
                            //        halt = 5.0;
                            //    }
                            //    double jam = 100.0;
                            //    if (!previewOnly)
                            //    {
                            //        if (arguments.Length >= 10 && arguments[9].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[9], out jam))
                            //        {
                            //             GD.Print("PassengerRatio is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            jam = 100.0;
                            //        }
                            //        else if (jam < 0.0)
                            //        {
                            //             GD.Print("PassengerRatio is expected to be non-negative in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            jam = 100.0;
                            //        }
                            //    }
                            //    if (!previewOnly)
                            //    {
                            //        if (arguments.Length >= 11 && arguments[10].Length > 0)
                            //        {
                            //            if (arguments[10].LastIndexOfAny(Path.GetInvalidPathChars()) >= 0)
                            //            {
                            //                 GD.Print("DepartureSound contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            }
                            //            else
                            //            {
                            //                string f = System.IO.Path.Combine(soundPath, arguments[10]);
                            //                if (!File.Exists(f))
                            //                {
                            //                     GD.Print("DepartureSound " + f + " not found in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                }
                            //                else
                            //                {
                            //                    const double radius = 30.0;
                            //                    depsnd = Sounds.RegisterBuffer(f, radius);
                            //                }
                            //            }
                            //        }
                            //    }
                            //    int ttidx = -1;
                            //    Textures.texture tdt = null, tnt = null;
                            //    if (!previewOnly)
                            //    {
                            //        if (arguments.Length >= 12 && arguments[11].Length > 0)
                            //        {
                            //            if (!Conversions.TryParseIntVb6(arguments[11], out ttidx))
                            //            {
                            //                ttidx = -1;
                            //            }
                            //            else
                            //            {
                            //                if (ttidx < 0)
                            //                {
                            //                     GD.Print("TimetableIndex is expected to be non-negative in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    ttidx = -1;
                            //                }
                            //                else if (ttidx >= data.TimetableDaytime.Length & ttidx >= data.TimetableNighttime.Length)
                            //                {
                            //                     GD.Print("TimetableIndex references textures not loaded in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    ttidx = -1;
                            //                }
                            //                tdt = ttidx >= 0 & ttidx < data.TimetableDaytime.Length ? data.TimetableDaytime[ttidx] : null;
                            //                tnt = ttidx >= 0 & ttidx < data.TimetableNighttime.Length ? data.TimetableNighttime[ttidx] : null;
                            //                ttidx = 0;
                            //            }
                            //        }
                            //        else
                            //        {
                            //            ttidx = -1;
                            //        }
                            //        if (ttidx == -1)
                            //        {
                            //            if (currentStation > 0)
                            //            {
                            //                tdt = Game.Stations[currentStation - 1].TimetableDaytimeTexture;
                            //                tnt = Game.Stations[currentStation - 1].TimetableNighttimeTexture;
                            //            }
                            //            else if (data.TimetableDaytime.Length > 0 & data.TimetableNighttime.Length > 0)
                            //            {
                            //                tdt = data.TimetableDaytime[0];
                            //                tnt = data.TimetableNighttime[0];
                            //            }
                            //            else
                            //            {
                            //                tdt = null;
                            //                tnt = null;
                            //            }
                            //        }
                            //    }
                            //    if (Game.Stations[currentStation].Name.Length == 0 & (Game.Stations[currentStation].StopMode == Game.StationStopMode.PlayerStop | Game.Stations[currentStation].StopMode == Game.StationStopMode.AllStop))
                            //    {
                            //        Game.Stations[currentStation].Name = "Station " + (currentStation + 1).ToString(culture) + ")";
                            //    }
                            //    Game.Stations[currentStation].ArrivalTime = arr;
                            //    Game.Stations[currentStation].ArrivalSoundBuffer = arrsnd;
                            //    Game.Stations[currentStation].DepartureTime = dep;
                            //    Game.Stations[currentStation].DepartureSoundBuffer = depsnd;
                            //    Game.Stations[currentStation].StopTime = halt;
                            //    Game.Stations[currentStation].ForceStopSignal = stop == 1;
                            //    Game.Stations[currentStation].OpenLeftDoors = door < 0.0 | doorboth;
                            //    Game.Stations[currentStation].OpenRightDoors = door > 0.0 | doorboth;
                            //    Game.Stations[currentStation].SafetySystem = device == 1 ? Game.SafetySystem.Atc : Game.SafetySystem.Ats;
                            //    Game.Stations[currentStation].Stops = new Game.StationStop[] { };
                            //    Game.Stations[currentStation].PassengerRatio = 0.01 * jam;
                            //    Game.Stations[currentStation].TimetableDaytimeTexture = tdt;
                            //    Game.Stations[currentStation].TimetableNighttimeTexture = tnt;
                            //    Game.Stations[currentStation].DefaultTrackPosition = data.TrackPosition;
                            //    data.Blocks[blockIndex].Station = currentStation;
                            //    data.Blocks[blockIndex].StationPassAlarm = passalarm == 1;
                            //    currentStop = -1;
                            //    departureSignalUsed = false;
                            //}
                            //break;
                            //case "track.station":
                            //    {
                            //        currentStation++;
                            //        Array.Resize<Game.Station>(ref Game.Stations, currentStation + 1);
                            //        Game.Stations[currentStation].Name = string.Empty;
                            //        Game.Stations[currentStation].StopMode = Game.StationStopMode.AllStop;
                            //        Game.Stations[currentStation].StationType = Game.StationType.Normal;
                            //        if (arguments.Length >= 1 && arguments[0].Length > 0)
                            //        {
                            //            Game.Stations[currentStation].Name = arguments[0];
                            //        }
                            //        double arr = -1.0, dep = -1.0;
                            //        if (arguments.Length >= 2 && arguments[1].Length > 0)
                            //        {
                            //            if (string.Equals(arguments[1], "P", StringComparison.OrdinalIgnoreCase) | string.Equals(arguments[1], "L", StringComparison.OrdinalIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StopMode = Game.StationStopMode.AllPass;
                            //            }
                            //            else if (string.Equals(arguments[1], "B", StringComparison.OrdinalIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerPass;
                            //            }
                            //            else if (arguments[1].StartsWith("B:", StringComparison.InvariantCultureIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerPass;
                            //                if (!Conversions.TryParseTime(arguments[1].Substring(2).TrimStart(), out arr))
                            //                {
                            //                     GD.Print("ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    arr = -1.0;
                            //                }
                            //            }
                            //            else if (string.Equals(arguments[1], "S", StringComparison.OrdinalIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerStop;
                            //            }
                            //            else if (arguments[1].StartsWith("S:", StringComparison.InvariantCultureIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StopMode = Game.StationStopMode.PlayerStop;
                            //                if (!Conversions.TryParseTime(arguments[1].Substring(2).TrimStart(), out arr))
                            //                {
                            //                     GD.Print("ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    arr = -1.0;
                            //                }
                            //            }
                            //            else if (!Conversions.TryParseTime(arguments[1], out arr))
                            //            {
                            //                 GD.Print("ArrivalTime is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                arr = -1.0;
                            //            }
                            //        }
                            //        if (arguments.Length >= 3 && arguments[2].Length > 0)
                            //        {
                            //            if (string.Equals(arguments[2], "T", StringComparison.OrdinalIgnoreCase) | string.Equals(arguments[2], "=", StringComparison.OrdinalIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StationType = Game.StationType.Terminal;
                            //            }
                            //            else if (arguments[2].StartsWith("T:", StringComparison.InvariantCultureIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StationType = Game.StationType.Terminal;
                            //                if (!Conversions.TryParseTime(arguments[2].Substring(2).TrimStart(), out dep))
                            //                {
                            //                     GD.Print("DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    dep = -1.0;
                            //                }
                            //            }
                            //            else if (string.Equals(arguments[2], "C", StringComparison.OrdinalIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StationType = Game.StationType.ChangeEnds;
                            //            }
                            //            else if (arguments[2].StartsWith("C:", StringComparison.InvariantCultureIgnoreCase))
                            //            {
                            //                Game.Stations[currentStation].StationType = Game.StationType.ChangeEnds;
                            //                if (!Conversions.TryParseTime(arguments[2].Substring(2).TrimStart(), out dep))
                            //                {
                            //                     GD.Print("DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    dep = -1.0;
                            //                }
                            //            }
                            //            else if (!Conversions.TryParseTime(arguments[2], out dep))
                            //            {
                            //                 GD.Print("DepartureTime is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                dep = -1.0;
                            //            }
                            //        }
                            //        int stop = 0;
                            //        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out stop))
                            //        {
                            //             GD.Print("ForcedRedSignal is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            stop = 0;
                            //        }
                            //        int device = 0;
                            //        if (arguments.Length >= 5 && arguments[4].Length > 0)
                            //        {
                            //            if (string.Compare(arguments[4], "ats", StringComparison.OrdinalIgnoreCase) == 0)
                            //            {
                            //                device = 0;
                            //            }
                            //            else if (string.Compare(arguments[4], "atc", StringComparison.OrdinalIgnoreCase) == 0)
                            //            {
                            //                device = 1;
                            //            }
                            //            else if (!Conversions.TryParseIntVb6(arguments[4], out device))
                            //            {
                            //                 GD.Print("System is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                device = 0;
                            //            }
                            //            else if (device != 0 & device != 1)
                            //            {
                            //                 GD.Print("System is not supported in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                device = 0;
                            //            }
                            //        }
                            //        Sounds.SoundBuffer depsnd = null;
                            //        if (!previewOnly)
                            //        {
                            //            if (arguments.Length >= 6 && arguments[5].Length != 0)
                            //            {
                            //                if (arguments[5].LastIndexOfAny(Path.GetInvalidPathChars()) >= 0)
                            //                {
                            //                     GD.Print("DepartureSound contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                }
                            //                else
                            //                {
                            //                    string f = System.IO.Path.Combine(soundPath, arguments[5]);
                            //                    if (!File.Exists(f))
                            //                    {
                            //                         GD.Print("DepartureSound " + f + " not found in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    }
                            //                    else
                            //                    {
                            //                        const double radius = 30.0;
                            //                        depsnd = Sounds.RegisterBuffer(f, radius);
                            //                    }
                            //                }
                            //            }
                            //        }
                            //        if (Game.Stations[currentStation].Name.Length == 0 & (Game.Stations[currentStation].StopMode == Game.StationStopMode.PlayerStop | Game.Stations[currentStation].StopMode == Game.StationStopMode.AllStop))
                            //        {
                            //            Game.Stations[currentStation].Name = "Station " + (currentStation + 1).ToString(culture) + ")";
                            //        }
                            //        Game.Stations[currentStation].ArrivalTime = arr;
                            //        Game.Stations[currentStation].ArrivalSoundBuffer = null;
                            //        Game.Stations[currentStation].DepartureTime = dep;
                            //        Game.Stations[currentStation].DepartureSoundBuffer = depsnd;
                            //        Game.Stations[currentStation].StopTime = 15.0;
                            //        Game.Stations[currentStation].ForceStopSignal = stop == 1;
                            //        Game.Stations[currentStation].OpenLeftDoors = true;
                            //        Game.Stations[currentStation].OpenRightDoors = true;
                            //        Game.Stations[currentStation].SafetySystem = device == 1 ? Game.SafetySystem.Atc : Game.SafetySystem.Ats;
                            //        Game.Stations[currentStation].Stops = new Game.StationStop[] { };
                            //        Game.Stations[currentStation].PassengerRatio = 1.0;
                            //        Game.Stations[currentStation].TimetableDaytimeTexture = null;
                            //        Game.Stations[currentStation].TimetableNighttimeTexture = null;
                            //        Game.Stations[currentStation].DefaultTrackPosition = data.TrackPosition;
                            //        data.Blocks[blockIndex].Station = currentStation;
                            //        data.Blocks[blockIndex].StationPassAlarm = false;
                            //        currentStop = -1;
                            //        departureSignalUsed = false;
                            //    }
                            //    break;
                            //case "track.buffer":
                            //    {
                            //        if (!previewOnly)
                            //        {
                            //            int n = Game.BufferTrackPositions.Length;
                            //            Array.Resize<double>(ref Game.BufferTrackPositions, n + 1);
                            //            Game.BufferTrackPositions[n] = data.TrackPosition;
                            //        }
                            //    }
                            //    break;
                            case "track.form":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx1 = 0, idx2 = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx1))
                                        {
                                            GD.Print("RailIndex1 is invalid in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx1 = 0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0)
                                        {
                                            if (string.Compare(arguments[1], "L", StringComparison.OrdinalIgnoreCase) == 0)
                                            {
                                                idx2 = Form.SecondaryRailL;
                                            }
                                            else if (string.Compare(arguments[1], "R", StringComparison.OrdinalIgnoreCase) == 0)
                                            {
                                                idx2 = Form.SecondaryRailR;
                                            }
                                            else if (isRW && string.Compare(arguments[1], "9X", StringComparison.OrdinalIgnoreCase) == 0)
                                            {
                                                idx2 = int.MaxValue;
                                            }
                                            else if (!Conversions.TryParseIntVb6(arguments[1], out idx2))
                                            {
                                                GD.Print("RailIndex2 is invalid in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                idx2 = 0;
                                            }
                                        }
                                        if (isRW)
                                        {
                                            if (idx2 == int.MaxValue)
                                            {
                                                idx2 = 9;
                                            }
                                            else if (idx2 == -9)
                                            {
                                                idx2 = Form.SecondaryRailL;
                                            }
                                            else if (idx2 == 9)
                                            {
                                                idx2 = Form.SecondaryRailR;
                                            }
                                        }
                                        if (idx1 < 0)
                                        {
                                            GD.Print("RailIndex1 is expected to be non-negative in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (idx2 < 0 & idx2 != Form.SecondaryRailStub & idx2 != Form.SecondaryRailL & idx2 != Form.SecondaryRailR)
                                        {
                                            GD.Print("RailIndex2 is expected to be greater or equal to -2 in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx1 >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx1].RailStart)
                                            {
                                                GD.Print("RailIndex1 could be out of range in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            if (idx2 != Form.SecondaryRailStub & idx2 != Form.SecondaryRailL & idx2 != Form.SecondaryRailR && (idx2 >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx2].RailStart))
                                            {
                                                GD.Print("RailIndex2 could be out of range in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            int roof = 0, pf = 0;
                                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out roof))
                                            {
                                                GD.Print("RoofStructureIndex is invalid in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                roof = 0;
                                            }
                                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out pf))
                                            {
                                                GD.Print("FormStructureIndex is invalid in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                pf = 0;
                                            }
                                            if (roof != 0 & (roof < 0 || (roof >= routeData.Structure.RoofL.Length || routeData.Structure.RoofL[roof] == null) || (roof >= routeData.Structure.RoofR.Length || routeData.Structure.RoofR[roof] == null)))
                                            {
                                                GD.Print("RoofStructureIndex references an object not loaded in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (pf < 0 | (pf >= routeData.Structure.FormL.Length || routeData.Structure.FormL[pf] == null) & (pf >= routeData.Structure.FormR.Length || routeData.Structure.FormR[pf] == null))
                                                {
                                                    GD.Print("FormStructureIndex references an object not loaded in Track.Form at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                int n = routeData.Blocks[blockIndex].Form.Length;
                                                Array.Resize<Form>(ref routeData.Blocks[blockIndex].Form, n + 1);
                                                routeData.Blocks[blockIndex].Form[n].PrimaryRail = idx1;
                                                routeData.Blocks[blockIndex].Form[n].SecondaryRail = idx2;
                                                routeData.Blocks[blockIndex].Form[n].FormType = pf;
                                                routeData.Blocks[blockIndex].Form[n].RoofType = roof;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.pole":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            GD.Print("RailIndex is expected to be non-negative in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart)
                                            {
                                                GD.Print("RailIndex could be out of range in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            if (idx >= routeData.Blocks[blockIndex].RailPole.Length)
                                            {
                                                Array.Resize<Pole>(ref routeData.Blocks[blockIndex].RailPole, idx + 1);
                                                routeData.Blocks[blockIndex].RailPole[idx].Mode = 0;
                                                routeData.Blocks[blockIndex].RailPole[idx].Location = 0;
                                                routeData.Blocks[blockIndex].RailPole[idx].Interval = 2.0 * routeData.BlockInterval;
                                                routeData.Blocks[blockIndex].RailPole[idx].Type = 0;
                                            }
                                            int typ = routeData.Blocks[blockIndex].RailPole[idx].Mode;
                                            int sttype = routeData.Blocks[blockIndex].RailPole[idx].Type;
                                            if (arguments.Length >= 2 && arguments[1].Length > 0)
                                            {
                                                if (!Conversions.TryParseIntVb6(arguments[1], out typ))
                                                {
                                                    GD.Print("AdditionalRailsCovered is invalid in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    typ = 0;
                                                }
                                            }
                                            if (arguments.Length >= 3 && arguments[2].Length > 0)
                                            {
                                                double loc;
                                                if (!Conversions.TryParseDoubleVb6(arguments[2], out loc))
                                                {
                                                    GD.Print("Location is invalid in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    loc = 0.0;
                                                }
                                                routeData.Blocks[blockIndex].RailPole[idx].Location = loc;
                                            }
                                            if (arguments.Length >= 4 && arguments[3].Length > 0)
                                            {
                                                double dist;
                                                if (!Conversions.TryParseDoubleVb6(arguments[3], unitOfLength, out dist))
                                                {
                                                    GD.Print("Interval is invalid in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    dist = routeData.BlockInterval;
                                                }
                                                routeData.Blocks[blockIndex].RailPole[idx].Interval = dist;
                                            }
                                            if (arguments.Length >= 5 && arguments[4].Length > 0)
                                            {
                                                if (!Conversions.TryParseIntVb6(arguments[4], out sttype))
                                                {
                                                    GD.Print("PoleStructureIndex is invalid in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    sttype = 0;
                                                }
                                            }
                                            if (typ < 0 || typ >= routeData.Structure.Poles.Length || routeData.Structure.Poles[typ] == null)
                                            {
                                                GD.Print("PoleStructureIndex references an object not loaded in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (sttype < 0 || sttype >= routeData.Structure.Poles[typ].Length || routeData.Structure.Poles[typ][sttype] == null)
                                            {
                                                GD.Print("PoleStructureIndex references an object not loaded in Track.Pole at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                routeData.Blocks[blockIndex].RailPole[idx].Mode = typ;
                                                routeData.Blocks[blockIndex].RailPole[idx].Type = sttype;
                                                routeData.Blocks[blockIndex].RailPole[idx].Exists = true;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.poleend":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.PoleEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0 | idx >= routeData.Blocks[blockIndex].RailPole.Length)
                                        {
                                            GD.Print("RailIndex does not reference an existing pole in Track.PoleEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx >= routeData.Blocks[blockIndex].Rail.Length || (!routeData.Blocks[blockIndex].Rail[idx].RailStart & !routeData.Blocks[blockIndex].Rail[idx].RailEnd))
                                            {
                                                GD.Print("RailIndex could be out of range in Track.PoleEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            routeData.Blocks[blockIndex].RailPole[idx].Exists = false;
                                        }
                                    }
                                }
                                break;
                            case "track.wall":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            GD.Print("RailIndex is expected to be a non-negative integer in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        int dir = 0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out dir))
                                        {
                                            GD.Print("Direction is invalid in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            dir = 0;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sttype))
                                        {
                                            GD.Print("WallStructureIndex is invalid in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (sttype < 0)
                                        {
                                            GD.Print("WallStructureIndex is expected to be a non-negative integer in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (dir <= 0 && (sttype >= routeData.Structure.WallL.Length || routeData.Structure.WallL[sttype] == null) ||
                                            dir >= 0 && (sttype >= routeData.Structure.WallR.Length || routeData.Structure.WallR[sttype] == null))
                                        {
                                            GD.Print("WallStructureIndex references an object not loaded in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx < 0)
                                            {
                                                GD.Print("RailIndex is expected to be non-negative in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart)
                                                {
                                                    GD.Print("RailIndex could be out of range in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                if (idx >= routeData.Blocks[blockIndex].RailWall.Length)
                                                {
                                                    Array.Resize<WallDike>(ref routeData.Blocks[blockIndex].RailWall, idx + 1);
                                                }
                                                routeData.Blocks[blockIndex].RailWall[idx].Exists = true;
                                                routeData.Blocks[blockIndex].RailWall[idx].Type = sttype;
                                                routeData.Blocks[blockIndex].RailWall[idx].Direction = dir;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.wallend":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.WallEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0 | idx >= routeData.Blocks[blockIndex].RailWall.Length)
                                        {
                                            GD.Print("RailIndex does not reference an existing wall in Track.WallEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx >= routeData.Blocks[blockIndex].Rail.Length || (!routeData.Blocks[blockIndex].Rail[idx].RailStart & !routeData.Blocks[blockIndex].Rail[idx].RailEnd))
                                            {
                                                GD.Print("RailIndex could be out of range in Track.WallEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            routeData.Blocks[blockIndex].RailWall[idx].Exists = false;
                                        }
                                    }
                                }
                                break;
                            case "track.dike":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            GD.Print("RailIndex is expected to be a non-negative integer in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        int dir = 0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out dir))
                                        {
                                            GD.Print("Direction is invalid in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            dir = 0;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sttype))
                                        {
                                            GD.Print("DikeStructureIndex is invalid in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (sttype < 0)
                                        {
                                            GD.Print("DikeStructureIndex is expected to be a non-negative integer in Track.Wall at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (dir <= 0 && (sttype >= routeData.Structure.DikeL.Length || routeData.Structure.DikeL[sttype] == null) ||
                                            dir >= 0 && (sttype >= routeData.Structure.DikeR.Length || routeData.Structure.DikeR[sttype] == null))
                                        {
                                            GD.Print("DikeStructureIndex references an object not loaded in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx < 0)
                                            {
                                                GD.Print("RailIndex is expected to be non-negative in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart)
                                                {
                                                    GD.Print("RailIndex could be out of range in Track.Dike at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                if (idx >= routeData.Blocks[blockIndex].RailDike.Length)
                                                {
                                                    Array.Resize<WallDike>(ref routeData.Blocks[blockIndex].RailDike, idx + 1);
                                                }
                                                routeData.Blocks[blockIndex].RailDike[idx].Exists = true;
                                                routeData.Blocks[blockIndex].RailDike[idx].Type = sttype;
                                                routeData.Blocks[blockIndex].RailDike[idx].Direction = dir;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.dikeend":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.DikeEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0 | idx >= routeData.Blocks[blockIndex].RailDike.Length)
                                        {
                                            GD.Print("RailIndex does not reference an existing dike in Track.DikeEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx >= routeData.Blocks[blockIndex].Rail.Length || (!routeData.Blocks[blockIndex].Rail[idx].RailStart & !routeData.Blocks[blockIndex].Rail[idx].RailEnd))
                                            {
                                                GD.Print("RailIndex could be out of range in Track.DikeEnd at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            routeData.Blocks[blockIndex].RailDike[idx].Exists = false;
                                        }
                                    }
                                }
                                break;
                            case "track.marker":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length < 1)
                                        {
                                            GD.Print("Track.Marker is expected to have at least one argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                        {
                                            GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                            if (!System.IO.File.Exists(f))
                                            {
                                                GD.Print("FileName " + f + " not found in Track.Marker at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                double dist = routeData.BlockInterval;
                                                if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out dist))
                                                {
                                                    GD.Print("Distance is invalid in Track.Marker at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    dist = routeData.BlockInterval;
                                                }
                                                double start, end;
                                                if (dist < 0.0)
                                                {
                                                    start = routeData.TrackPosition;
                                                    end = routeData.TrackPosition - dist;
                                                }
                                                else
                                                {
                                                    start = routeData.TrackPosition - dist;
                                                    end = routeData.TrackPosition;
                                                }
                                                if (start < 0.0) start = 0.0;
                                                if (end < 0.0) end = 0.0;
                                                if (end <= start) end = start + 0.01;
                                                int n = routeData.Markers.Length;
                                                Array.Resize<Marker>(ref routeData.Markers, n + 1);
                                                routeData.Markers[n].StartingPosition = start;
                                                routeData.Markers[n].EndingPosition = end;
                                                //Textures.RegisterTexture(f, new OpenBveApi.textures.textureParameters(null, new Color24(64, 64, 64)), out data.Markers[n].texture);
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.height":
                                {
                                    if (!previewOnly)
                                    {
                                        double h = 0.0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[0], unitOfLength, out h))
                                        {
                                            GD.Print("Height is invalid in Track.Height at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            h = 0.0;
                                        }
                                        routeData.Blocks[blockIndex].Height = isRW ? h + 0.3 : h;
                                    }
                                }
                                break;
                            case "track.ground":
                                {
                                    if (!previewOnly)
                                    {
                                        int cytype = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out cytype))
                                        {
                                            GD.Print("CycleIndex is invalid in Track.Ground at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            cytype = 0;
                                        }
                                        if (cytype < routeData.Structure.Cycle.Length && routeData.Structure.Cycle[cytype] != null)
                                        {
                                            routeData.Blocks[blockIndex].Cycle = routeData.Structure.Cycle[cytype];
                                        }
                                        else
                                        {
                                            if (cytype >= routeData.Structure.Ground.Length || routeData.Structure.Ground[cytype] == null)
                                            {
                                                GD.Print("CycleIndex references an object not loaded in Track.Ground at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                routeData.Blocks[blockIndex].Cycle = new int[] { cytype };
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.crack":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx1 = 0, idx2 = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx1))
                                        {
                                            GD.Print("RailIndex1 is invalid in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx1 = 0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out idx2))
                                        {
                                            GD.Print("RailIndex2 is invalid in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx2 = 0;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sttype))
                                        {
                                            GD.Print("CrackStructureIndex is invalid in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (sttype < 0 || (sttype >= routeData.Structure.CrackL.Length || routeData.Structure.CrackL[sttype] == null) || (sttype >= routeData.Structure.CrackR.Length || routeData.Structure.CrackR[sttype] == null))
                                        {
                                            GD.Print("CrackStructureIndex references an object not loaded in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx1 < 0)
                                            {
                                                GD.Print("RailIndex1 is expected to be non-negative in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (idx2 < 0)
                                            {
                                                GD.Print("RailIndex2 is expected to be non-negative in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else if (idx1 == idx2)
                                            {
                                                GD.Print("RailIndex1 is expected to be unequal to Index2 in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                if (idx1 >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx1].RailStart)
                                                {
                                                    GD.Print("RailIndex1 could be out of range in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                if (idx2 >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx2].RailStart)
                                                {
                                                    GD.Print("RailIndex2 could be out of range in Track.Crack at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                int n = routeData.Blocks[blockIndex].Crack.Length;
                                                Array.Resize<Crack>(ref routeData.Blocks[blockIndex].Crack, n + 1);
                                                routeData.Blocks[blockIndex].Crack[n].PrimaryRail = idx1;
                                                routeData.Blocks[blockIndex].Crack[n].SecondaryRail = idx2;
                                                routeData.Blocks[blockIndex].Crack[n].Type = sttype;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.freeobj":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0, sttype = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out sttype))
                                        {
                                            GD.Print("FreeObjStructureIndex is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            sttype = 0;
                                        }
                                        if (idx < -1)
                                        {
                                            GD.Print("RailIndex is expected to be non-negative or -1 in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (sttype < 0)
                                        {
                                            GD.Print("FreeObjStructureIndex is expected to be non-negative in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (idx >= 0 && (idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart))
                                            {
                                                GD.Print("RailIndex could be out of range in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            if (sttype >= routeData.Structure.FreeObj.Length || routeData.Structure.FreeObj[sttype] == null || routeData.Structure.FreeObj[sttype] == null)
                                            {
                                                GD.Print("FreeObjStructureIndex references an object not loaded in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                double x = 0.0, y = 0.0;
                                                double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                                if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out x))
                                                {
                                                    GD.Print("X is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    x = 0.0;
                                                }
                                                if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], unitOfLength, out y))
                                                {
                                                    GD.Print("Y is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    y = 0.0;
                                                }
                                                if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out yaw))
                                                {
                                                    GD.Print("Yaw is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    yaw = 0.0;
                                                }
                                                if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out pitch))
                                                {
                                                    GD.Print("Pitch is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    pitch = 0.0;
                                                }
                                                if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out roll))
                                                {
                                                    GD.Print("Roll is invalid in Track.FreeObj at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                    roll = 0.0;
                                                }
                                                if (idx == -1)
                                                {
                                                    int n;
                                                    n = routeData.Blocks[blockIndex].GroundFreeObj.Length;
                                                    Array.Resize<FreeObj>(ref routeData.Blocks[blockIndex].GroundFreeObj, n + 1);
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].TrackPosition = routeData.TrackPosition;
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].Type = sttype;
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].X = x;
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].Y = y;
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].Yaw = yaw * 0.0174532925199433;
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].Pitch = pitch * 0.0174532925199433;
                                                    routeData.Blocks[blockIndex].GroundFreeObj[n].Roll = roll * 0.0174532925199433;
                                                }
                                                else
                                                {
                                                    if (idx >= routeData.Blocks[blockIndex].RailFreeObj.Length)
                                                    {
                                                        Array.Resize<FreeObj[]>(ref routeData.Blocks[blockIndex].RailFreeObj, idx + 1);
                                                    }
                                                    int n;
                                                    if (routeData.Blocks[blockIndex].RailFreeObj[idx] == null)
                                                    {
                                                        routeData.Blocks[blockIndex].RailFreeObj[idx] = new FreeObj[1];
                                                        n = 0;
                                                    }
                                                    else
                                                    {
                                                        n = routeData.Blocks[blockIndex].RailFreeObj[idx].Length;
                                                        Array.Resize<FreeObj>(ref routeData.Blocks[blockIndex].RailFreeObj[idx], n + 1);
                                                    }
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].TrackPosition = routeData.TrackPosition;
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].Type = sttype;
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].X = x;
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].Y = y;
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].Yaw = yaw * 0.0174532925199433;        // TODO degrees to radians 
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].Pitch = pitch * 0.0174532925199433;
                                                    routeData.Blocks[blockIndex].RailFreeObj[idx][n].Roll = roll * 0.0174532925199433;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.back":
                                {
                                    if (!previewOnly)
                                    {
                                        int typ = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out typ))
                                        {
                                            GD.Print("BackgroundTextureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            typ = 0;
                                        }
                                        if (typ < 0 | typ >= routeData.Backgrounds.Length)
                                        {
                                            GD.Print("BackgroundTextureIndex references a texture not loaded in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else if (routeData.Backgrounds[typ] == null)
                                        {
                                            GD.Print("BackgroundTextureIndex has not been loaded via Texture.Background in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            routeData.Blocks[blockIndex].Background = typ;
                                        }
                                    }
                                }
                                break;
                            case "track.announce":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length == 0)
                                        {
                                            GD.Print(command + " is expected to have between 1 and 2 arguments at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(soundPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    double speed = 0.0;
                                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out speed))
                                                    {
                                                        GD.Print("Speed is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                        speed = 0.0;
                                                    }
                                                    int n = routeData.Blocks[blockIndex].Sound.Length;
                                                    Array.Resize<Sound>(ref routeData.Blocks[blockIndex].Sound, n + 1);
                                                    routeData.Blocks[blockIndex].Sound[n].TrackPosition = routeData.TrackPosition;
                                                    const double radius = 15.0;
                                                    //data.Blocks[blockIndex].Sound[n].SoundBuffer = Sounds.RegisterBuffer(f, radius);
                                                    routeData.Blocks[blockIndex].Sound[n].Type = speed == 0.0 ? SoundType.TrainStatic : SoundType.TrainDynamic;
                                                    routeData.Blocks[blockIndex].Sound[n].Speed = speed * routeData.UnitOfSpeed;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.doppler":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length == 0)
                                        {
                                            GD.Print(command + " is expected to have between 1 and 3 arguments at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                GD.Print("FileName contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(soundPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    GD.Print("FileName " + f + " not found in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                }
                                                else
                                                {
                                                    double x = 0.0, y = 0.0;
                                                    if (arguments.Length >= 2 && arguments[1].Length > 0 & !Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out x))
                                                    {
                                                        GD.Print("X is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                        x = 0.0;
                                                    }
                                                    if (arguments.Length >= 3 && arguments[2].Length > 0 & !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out y))
                                                    {
                                                        GD.Print("Y is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                        y = 0.0;
                                                    }
                                                    int n = routeData.Blocks[blockIndex].Sound.Length;
                                                    Array.Resize<Sound>(ref routeData.Blocks[blockIndex].Sound, n + 1);
                                                    routeData.Blocks[blockIndex].Sound[n].TrackPosition = routeData.TrackPosition;
                                                    const double radius = 15.0;
                                                    //data.Blocks[blockIndex].Sound[n].SoundBuffer = Sounds.RegisterBuffer(f, radius);
                                                    routeData.Blocks[blockIndex].Sound[n].Type = SoundType.World;
                                                    routeData.Blocks[blockIndex].Sound[n].X = x;
                                                    routeData.Blocks[blockIndex].Sound[n].Y = y;
                                                    routeData.Blocks[blockIndex].Sound[n].Radius = radius;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.pretrain":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length == 0)
                                        {
                                            GD.Print(command + " is expected to have exactly 1 argument at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        else
                                        {
                                            double time = 0.0;
                                            if (arguments[0].Length > 0 & !Conversions.TryParseTime(arguments[0], out time))
                                            {
                                                GD.Print("Time is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                                time = 0.0;
                                            }
                                            //int n = Game.BogusPretrainInstructions.Length;
                                            //if (n != 0 && Game.BogusPretrainInstructions[n - 1].Time >= time)
                                            //{
                                            //     GD.Print("Time is expected to be in ascending order between successive " + command + " commands at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            //}
                                            //Array.Resize<Game.BogusPretrainInstruction>(ref Game.BogusPretrainInstructions, n + 1);
                                            //Game.BogusPretrainInstructions[n].TrackPosition = data.TrackPosition;
                                            //Game.BogusPretrainInstructions[n].Time = time;
                                        }
                                    }
                                }
                                break;
                            case "track.pointofinterest":
                            case "track.poi":
                                {
                                    if (!previewOnly)
                                    {
                                        int idx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            GD.Print("RailIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            GD.Print("RailIndex is expected to be non-negative in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            idx = 0;
                                        }
                                        if (idx >= 0 && (idx >= routeData.Blocks[blockIndex].Rail.Length || !routeData.Blocks[blockIndex].Rail[idx].RailStart))
                                        {
                                            GD.Print("RailIndex references a non-existing rail in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                        }
                                        double x = 0.0, y = 0.0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out x))
                                        {
                                            GD.Print("X is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            x = 0.0;
                                        }
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out y))
                                        {
                                            GD.Print("Y is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            y = 0.0;
                                        }
                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out yaw))
                                        {
                                            GD.Print("Yaw is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out pitch))
                                        {
                                            GD.Print("Pitch is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out roll))
                                        {
                                            GD.Print("Roll is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            roll = 0.0;
                                        }
                                        string text = null;
                                        if (arguments.Length >= 7 && arguments[6].Length != 0)
                                        {
                                            text = arguments[6];
                                        }
                                        int n = routeData.Blocks[blockIndex].PointsOfInterest.Length;
                                        Array.Resize<PointOfInterest>(ref routeData.Blocks[blockIndex].PointsOfInterest, n + 1);
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].RailIndex = idx;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].X = x;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].Y = y;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].Yaw = 0.0174532925199433 * yaw;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].Pitch = 0.0174532925199433 * pitch;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].Roll = 0.0174532925199433 * roll;
                                        routeData.Blocks[blockIndex].PointsOfInterest[n].Text = text;
                                    }
                                }
                                break;
                            default:
                                GD.Print("The command " + command + " is not supported at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                break;
                        }
                    }
                }
            }
        }
        if (!previewOnly)
        {
            // timetable
            //Timetable.CustomTextures = new Textures.texture[data.TimetableDaytime.Length + data.TimetableNighttime.Length];
            //int n = 0;
            //for (int i = 0; i < data.TimetableDaytime.Length; i++)
            //{
            //    if (data.TimetableDaytime[i] != null)
            //    {
            //        Timetable.CustomTextures[n] = data.TimetableDaytime[i];
            //        n++;
            //    }
            //}
            //for (int i = 0; i < data.TimetableNighttime.Length; i++)
            //{
            //    if (data.TimetableNighttime[i] != null)
            //    {
            //        Timetable.CustomTextures[n] = data.TimetableNighttime[i];
            //        n++;
            //    }
            //}
            //Array.Resize<Textures.texture>(ref Timetable.CustomTextures, n);
        }
        // blocks
        Array.Resize<Block>(ref routeData.Blocks, blocksUsed);
    }

    private static void InterpolateHeight(Block[] blocks)
    {
        int z = 0;
        for (int i = 0; i < blocks.Length; i++)
        {
            if (!double.IsNaN(blocks[i].Height))
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (!double.IsNaN(blocks[j].Height))
                    {
                        double a = blocks[j].Height;
                        double b = blocks[i].Height;
                        double d = (b - a) / (double)(i - j);
                        for (int k = j + 1; k < i; k++)
                        {
                            a += d;
                            blocks[k].Height = a;
                        }
                        break;
                    }
                }
                z = i;
            }
        }
        for (int i = z + 1; i < blocks.Length; i++)
        {
            blocks[i].Height = blocks[z].Height;
        }
    }

    #endregion

    #region Apply Route Data (CsvRwRouteParser.RouteData.cs)

    private static void ApplyRouteData(Node rootForRouteObjects, string fileName, string compatibilityFolder, Encoding fileEncoding, ref RouteData routeData, bool previewOnly)
    {
        string signalPath, limitPath, limitGraphicsPath, transponderPath;
        UnifiedObject signalPost, limitPostStraight, limitPostLeft, limitPostRight, limitPostInfinite;
        UnifiedObject limitOneDigit, limitTwoDigits, limitThreeDigits, stopPost;
        UnifiedObject transponderS, transponderSN, transponderFalseStart, transponderPOrigin, transponderPStop;

        if (!previewOnly)
        {
            // load compatibility objects
            Node rootForCompatObjects = new Node();//("Compatibility");
            rootForCompatObjects.Name = "Compatibility";

            signalPath = System.IO.Path.Combine(compatibilityFolder, @"Signals\Japanese");
            signalPost = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(signalPath, "signal_post.csv"), fileEncoding, false, false, false);
            limitPath = System.IO.Path.Combine(compatibilityFolder, "Limits");
            limitGraphicsPath = System.IO.Path.Combine(limitPath, "Graphics");
            limitPostStraight = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_straight.csv"), fileEncoding, false, false, false);
            limitPostLeft = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_left.csv"), fileEncoding, false, false, false);
            limitPostRight = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_right.csv"), fileEncoding, false, false, false);
            limitPostInfinite = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_infinite.csv"), fileEncoding, false, false, false);
            limitOneDigit = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_1_digit.csv"), fileEncoding, false, false, false);
            limitTwoDigits = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_2_digits.csv"), fileEncoding, false, false, false);
            limitThreeDigits = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(limitPath, "limit_3_digits.csv"), fileEncoding, false, false, false);
            stopPost = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(compatibilityFolder, "stop.csv"), fileEncoding, false, false, false);
            transponderPath = System.IO.Path.Combine(compatibilityFolder, "Transponders");
            transponderS = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "s.csv"), fileEncoding, false, false, false);
            transponderSN = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "sn.csv"), fileEncoding, false, false, false);
            transponderFalseStart = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "falsestart.csv"), fileEncoding, false, false, false);
            transponderPOrigin = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "porigin.csv"), fileEncoding, false, false, false);
            transponderPStop = ObjectManager.Instance.LoadStaticObject(rootForCompatObjects, System.IO.Path.Combine(transponderPath, "pstop.csv"), fileEncoding, false, false, false);
        }
        else
        {
            signalPath = null;
            limitPath = null;
            limitGraphicsPath = null;
            transponderPath = null;
            signalPost = null;
            limitPostStraight = null;
            limitPostLeft = null;
            limitPostRight = null;
            limitPostInfinite = null;
            limitOneDigit = null;
            limitTwoDigits = null;
            limitThreeDigits = null;
            stopPost = null;
            transponderS = null;
            transponderSN = null;
            transponderFalseStart = null;
            transponderPOrigin = null;
            transponderPStop = null;
        }

        // initialize
        System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
        int lastBlock = (int)Math.Floor((routeData.TrackPosition + 600.0) / routeData.BlockInterval + 0.001) + 1;
        int blocksUsed = routeData.Blocks.Length;
        CreateMissingBlocks(ref routeData, ref blocksUsed, lastBlock, previewOnly);
        Array.Resize<Block>(ref routeData.Blocks, blocksUsed);


        if (!previewOnly)
        {
            // interpolate height
            InterpolateHeight(routeData.Blocks);
        }

        // background
        if (!previewOnly)
        {
            if (routeData.Blocks[0].Background >= 0 & routeData.Blocks[0].Background < routeData.Backgrounds.Length)
            {
                //RenderSettings.skybox.mainTexture = routeData.Backgrounds[routeData.Blocks[0].Background];

                // Material material = CreateSkyboxMaterial(routeData.Backgrounds[routeData.Blocks[0].Background]);
                // Node camera = Camera.main.Node;
                // Skybox skybox = camera.GetComponent<Skybox>();
                // if (skybox == null) skybox = camera.AddComponent<Skybox>();
                // skybox.material = material;
            }
            else
            {
                //World.CurrentBackground = new World.Background(null, 6, false);
            }
            //World.TargetBackground = World.CurrentBackground;
        }

        // brightness
        int currentBrightnessElement = -1;
        int currentBrightnessEvent = -1;
        float currentBrightnessValue = 1.0f;
        double currentBrightnessTrackPosition = (double)routeData.FirstUsedBlock * routeData.BlockInterval;
        if (!previewOnly)
        {
            for (int i = routeData.FirstUsedBlock; i < routeData.Blocks.Length; i++)
            {
                if (routeData.Blocks[i].Brightness != null && routeData.Blocks[i].Brightness.Length != 0)
                {
                    currentBrightnessValue = routeData.Blocks[i].Brightness[0].Value;
                    currentBrightnessTrackPosition = routeData.Blocks[i].Brightness[0].Value;
                    break;
                }
            }
        }
     

        // create objects and track
        Vector3 playerRailPos = new Vector3(0.0f, 0.0f, 0.0f);
        Vector2 playerRailDir = new Vector2(0.0f, 1.0f);
        TrackManager.CurrentTrack = new TrackManager.Track();
        TrackManager.CurrentTrack.Elements = new TrackManager.TrackElement[] { };
        double currentSpeedLimit = double.PositiveInfinity;
        int currentRunIndex = 0;
        int currentFlangeIndex = 0;
        if (routeData.FirstUsedBlock < 0) routeData.FirstUsedBlock = 0;
        TrackManager.CurrentTrack.Elements = new TrackManager.TrackElement[256];
        int currentTrackLength = 0;
        int previousFogElement = -1;
        int previousFogEvent = -1;
        //Game.Fog PreviousFog = new Game.Fog(Game.NoFogStart, Game.NoFogEnd, new Color24(128, 128, 128), -Data.BlockInterval);
        //Game.Fog CurrentFog = new Game.Fog(Game.NoFogStart, Game.NoFogEnd, new Color24(128, 128, 128), 0.0);

        // process blocks
        double progressFactor = routeData.Blocks.Length - routeData.FirstUsedBlock == 0 ? 0.5 : 0.5 / (double)(routeData.Blocks.Length - routeData.FirstUsedBlock);
        for (int blockIdx = routeData.FirstUsedBlock; blockIdx < routeData.Blocks.Length; blockIdx++)
        {
            double startingDistance = (double)blockIdx * routeData.BlockInterval;
            double endingDistance = startingDistance + routeData.BlockInterval;
            
            playerRailDir = playerRailDir.Normalized();

            // track
            if (!previewOnly)
            {
                if (routeData.Blocks[blockIdx].Cycle.Length == 1 && routeData.Blocks[blockIdx].Cycle[0] == -1)
                {
                     if (routeData.Structure.Cycle.Length == 0 || routeData.Structure.Cycle[0] == null)
                    {
                        routeData.Blocks[blockIdx].Cycle = new int[] { 0 };
                    }
                    else
                    {
                        routeData.Blocks[blockIdx].Cycle = routeData.Structure.Cycle[0];
                    }
                }
            }

            TrackManager.TrackElement worldTrackElement = routeData.Blocks[blockIdx].CurrentTrackState;
            int n = currentTrackLength;
            if (n >= TrackManager.CurrentTrack.Elements.Length)
            {
                Array.Resize<TrackManager.TrackElement>(ref TrackManager.CurrentTrack.Elements, TrackManager.CurrentTrack.Elements.Length << 1);
            }

            currentTrackLength++;
            TrackManager.CurrentTrack.Elements[n] = worldTrackElement;
            TrackManager.CurrentTrack.Elements[n].WorldPosition = playerRailPos;
            TrackManager.CurrentTrack.Elements[n].WorldDirection = Calc.GetNormalizedVector3(playerRailDir, routeData.Blocks[blockIdx].Pitch);
            TrackManager.CurrentTrack.Elements[n].WorldSide = new Vector3(playerRailDir.y, 0.0f, -playerRailDir.x);
            // World.Cross(TrackManager.CurrentTrack.Elements[n].WorldDirection.X, 
            //             TrackManager.CurrentTrack.Elements[n].WorldDirection.Y, 
            //             TrackManager.CurrentTrack.Elements[n].WorldDirection.Z, 
            //             TrackManager.CurrentTrack.Elements[n].WorldSide.X, 
            //             TrackManager.CurrentTrack.Elements[n].WorldSide.Y, 
            //             TrackManager.CurrentTrack.Elements[n].WorldSide.Z, 
            //             out TrackManager.CurrentTrack.Elements[n].WorldUp.X, 
            //             out TrackManager.CurrentTrack.Elements[n].WorldUp.Y, 
            //             out TrackManager.CurrentTrack.Elements[n].WorldUp.Z);
            TrackManager.CurrentTrack.Elements[n].StartingTrackPosition = startingDistance;
            TrackManager.CurrentTrack.Elements[n].Events = new TrackManager.GeneralEvent[] { };
            TrackManager.CurrentTrack.Elements[n].AdhesionMultiplier = routeData.Blocks[blockIdx].AdhesionMultiplier;
            TrackManager.CurrentTrack.Elements[n].CsvRwAccuracyLevel = routeData.Blocks[blockIdx].Accuracy;

            // background
            //if (!PreviewOnly)
            //{
            //    if (Data.Blocks[i].Background >= 0)
            //    {
            //        int typ;
            //        if (i == Data.FirstUsedBlock)
            //        {
            //            typ = Data.Blocks[i].Background;
            //        }
            //        else
            //        {
            //            typ = Data.Backgrounds.Length > 0 ? 0 : -1;
            //            for (int j = i - 1; j >= Data.FirstUsedBlock; j--)
            //            {
            //                if (Data.Blocks[j].Background >= 0)
            //                {
            //                    typ = Data.Blocks[j].Background;
            //                    break;
            //                }
            //            }
            //        }
            //        if (typ >= 0 & typ < Data.Backgrounds.Length)
            //        {
            //            int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //            TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.BackgroundChangeEvent(0.0, Data.Backgrounds[typ], Data.Backgrounds[Data.Blocks[i].Background]);
            //        }
            //    }
            //}

            // brightness
            //if (!PreviewOnly)
            //{
            //    for (int j = 0; j < Data.Blocks[i].Brightness.Length; j++)
            //    {
            //        int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //        double d = Data.Blocks[i].Brightness[j].TrackPosition - StartingDistance;
            //        TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.BrightnessChangeEvent(d, Data.Blocks[i].Brightness[j].Value, CurrentBrightnessValue, Data.Blocks[i].Brightness[j].TrackPosition - CurrentBrightnessTrackPosition, Data.Blocks[i].Brightness[j].Value, 0.0);
            //        if (CurrentBrightnessElement >= 0 & CurrentBrightnessEvent >= 0)
            //        {
            //            TrackManager.BrightnessChangeEvent bce = (TrackManager.BrightnessChangeEvent)TrackManager.CurrentTrack.Elements[CurrentBrightnessElement].Events[CurrentBrightnessEvent];
            //            bce.NextBrightness = Data.Blocks[i].Brightness[j].Value;
            //            bce.NextDistance = Data.Blocks[i].Brightness[j].TrackPosition - CurrentBrightnessTrackPosition;
            //        }
            //        CurrentBrightnessElement = n;
            //        CurrentBrightnessEvent = m;
            //        CurrentBrightnessValue = Data.Blocks[i].Brightness[j].Value;
            //        CurrentBrightnessTrackPosition = Data.Blocks[i].Brightness[j].TrackPosition;
            //    }
            //}

            // fog
            //if (!PreviewOnly)
            //{
            //    if (Data.FogTransitionMode)
            //    {
            //        if (Data.Blocks[i].FogDefined)
            //        {
            //            Data.Blocks[i].Fog.TrackPosition = StartingDistance;
            //            int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //            TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.FogChangeEvent(0.0, PreviousFog, Data.Blocks[i].Fog, Data.Blocks[i].Fog);
            //            if (PreviousFogElement >= 0 & PreviousFogEvent >= 0)
            //            {
            //                TrackManager.FogChangeEvent e = (TrackManager.FogChangeEvent)TrackManager.CurrentTrack.Elements[PreviousFogElement].Events[PreviousFogEvent];
            //                e.NextFog = Data.Blocks[i].Fog;
            //            }
            //            else
            //            {
            //                Game.PreviousFog = PreviousFog;
            //                Game.CurrentFog = PreviousFog;
            //                Game.NextFog = Data.Blocks[i].Fog;
            //            }
            //            PreviousFog = Data.Blocks[i].Fog;
            //            PreviousFogElement = n;
            //            PreviousFogEvent = m;
            //        }
            //    }
            //    else
            //    {
            //        Data.Blocks[i].Fog.TrackPosition = StartingDistance + Data.BlockInterval;
            //        int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //        TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.FogChangeEvent(0.0, PreviousFog, CurrentFog, Data.Blocks[i].Fog);
            //        PreviousFog = CurrentFog;
            //        CurrentFog = Data.Blocks[i].Fog;
            //    }
            //}

            // rail sounds
            //if (!PreviewOnly)
            //{
            //    int j = Data.Blocks[i].RailType[0];
            //    int r = j < Data.Structure.Run.Length ? Data.Structure.Run[j] : 0;
            //    int f = j < Data.Structure.Flange.Length ? Data.Structure.Flange[j] : 0;
            //    int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //    TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.RailSoundsChangeEvent(0.0, CurrentRunIndex, CurrentFlangeIndex, r, f);
            //    CurrentRunIndex = r;
            //    CurrentFlangeIndex = f;
            //}

            // point sound
            //if (!PreviewOnly)
            //{
            //    if (i < Data.Blocks.Length - 1)
            //    {
            //        bool q = false;
            //        for (int j = 0; j < Data.Blocks[i].Rail.Length; j++)
            //        {
            //            if (Data.Blocks[i].Rail[j].RailStart & Data.Blocks[i + 1].Rail.Length > j)
            //            {
            //                bool qx = Math.Sign(Data.Blocks[i].Rail[j].RailStartX) != Math.Sign(Data.Blocks[i + 1].Rail[j].RailEndX);
            //                bool qy = Data.Blocks[i].Rail[j].RailStartY * Data.Blocks[i + 1].Rail[j].RailEndY <= 0.0;
            //                if (qx & qy)
            //                {
            //                    q = true;
            //                    break;
            //                }
            //            }
            //        }
            //        if (q)
            //        {
            //            int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //            TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.SoundEvent(0.0, null, false, false, true, new Vector3D(0.0, 0.0, 0.0), 12.5);
            //        }
            //    }
            //}

            // station
            //if (Data.Blocks[i].Station >= 0)
            //{
            //    // station
            //    int s = Data.Blocks[i].Station;
            //    int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //    TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.StationStartEvent(0.0, s);
            //    double dx, dy = 3.0;
            //    if (Game.Stations[s].OpenLeftDoors & !Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = -5.0;
            //    }
            //    else if (!Game.Stations[s].OpenLeftDoors & Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = 5.0;
            //    }
            //    else
            //    {
            //        dx = 0.0;
            //    }
            //    Game.Stations[s].SoundOrigin.X = Position.X + dx * TrackManager.CurrentTrack.Elements[n].WorldSide.X + dy * TrackManager.CurrentTrack.Elements[n].WorldUp.X;
            //    Game.Stations[s].SoundOrigin.Y = Position.Y + dx * TrackManager.CurrentTrack.Elements[n].WorldSide.Y + dy * TrackManager.CurrentTrack.Elements[n].WorldUp.Y;
            //    Game.Stations[s].SoundOrigin.Z = Position.Z + dx * TrackManager.CurrentTrack.Elements[n].WorldSide.Z + dy * TrackManager.CurrentTrack.Elements[n].WorldUp.Z;
            //    // passalarm
            //    if (!PreviewOnly)
            //    {
            //        if (Data.Blocks[i].StationPassAlarm)
            //        {
            //            int b = i - 6;
            //            if (b >= 0)
            //            {
            //                int j = b - Data.FirstUsedBlock;
            //                if (j >= 0)
            //                {
            //                    m = TrackManager.CurrentTrack.Elements[j].Events.Length;
            //                    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[j].Events, m + 1);
            //                    TrackManager.CurrentTrack.Elements[j].Events[m] = new TrackManager.StationPassAlarmEvent(0.0);
            //                }
            //            }
            //        }
            //    }
            //}

            // stop
            //for (int j = 0; j < Data.Blocks[i].Stop.Length; j++)
            //{
            //    int s = Data.Blocks[i].Stop[j].Station;
            //    int t = Game.Stations[s].Stops.Length;
            //    Array.Resize<Game.StationStop>(ref Game.Stations[s].Stops, t + 1);
            //    Game.Stations[s].Stops[t].TrackPosition = Data.Blocks[i].Stop[j].TrackPosition;
            //    Game.Stations[s].Stops[t].ForwardTolerance = Data.Blocks[i].Stop[j].ForwardTolerance;
            //    Game.Stations[s].Stops[t].BackwardTolerance = Data.Blocks[i].Stop[j].BackwardTolerance;
            //    Game.Stations[s].Stops[t].Cars = Data.Blocks[i].Stop[j].Cars;
            //    double dx, dy = 2.0;
            //    if (Game.Stations[s].OpenLeftDoors & !Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = -5.0;
            //    }
            //    else if (!Game.Stations[s].OpenLeftDoors & Game.Stations[s].OpenRightDoors)
            //    {
            //        dx = 5.0;
            //    }
            //    else
            //    {
            //        dx = 0.0;
            //    }
            //    Game.Stations[s].SoundOrigin.X = Position.X + dx * TrackManager.CurrentTrack.Elements[n].WorldSide.X + dy * TrackManager.CurrentTrack.Elements[n].WorldUp.X;
            //    Game.Stations[s].SoundOrigin.Y = Position.Y + dx * TrackManager.CurrentTrack.Elements[n].WorldSide.Y + dy * TrackManager.CurrentTrack.Elements[n].WorldUp.Y;
            //    Game.Stations[s].SoundOrigin.Z = Position.Z + dx * TrackManager.CurrentTrack.Elements[n].WorldSide.Z + dy * TrackManager.CurrentTrack.Elements[n].WorldUp.Z;
            //}

            // limit
            //for (int j = 0; j < Data.Blocks[i].Limit.Length; j++)
            //{
            //    int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //    double d = Data.Blocks[i].Limit[j].TrackPosition - StartingDistance;
            //    TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.LimitChangeEvent(d, CurrentSpeedLimit, Data.Blocks[i].Limit[j].Speed);
            //    CurrentSpeedLimit = Data.Blocks[i].Limit[j].Speed;
            //}

            // marker
            //if (!PreviewOnly)
            //{
            //    for (int j = 0; j < Data.Markers.Length; j++)
            //    {
            //        if (Data.Markers[j].StartingPosition >= StartingDistance & Data.Markers[j].StartingPosition < EndingDistance)
            //        {
            //            int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //            double d = Data.Markers[j].StartingPosition - StartingDistance;
            //            TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.MarkerStartEvent(d, Data.Markers[j].Texture);
            //        }
            //        if (Data.Markers[j].EndingPosition >= StartingDistance & Data.Markers[j].EndingPosition < EndingDistance)
            //        {
            //            int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //            double d = Data.Markers[j].EndingPosition - StartingDistance;
            //            TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.MarkerEndEvent(d, Data.Markers[j].Texture);
            //        }
            //    }
            //}

            // sound
            //if (!PreviewOnly)
            //{
            //    for (int j = 0; j < Data.Blocks[i].Sound.Length; j++)
            //    {
            //        if (Data.Blocks[i].Sound[j].Type == SoundType.TrainStatic | Data.Blocks[i].Sound[j].Type == SoundType.TrainDynamic)
            //        {
            //            int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
            //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
            //            double d = Data.Blocks[i].Sound[j].TrackPosition - StartingDistance;
            //            switch (Data.Blocks[i].Sound[j].Type)
            //            {
            //                case SoundType.TrainStatic:
            //                    TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.SoundEvent(d, Data.Blocks[i].Sound[j].SoundBuffer, true, true, false, new Vector3D(0.0, 0.0, 0.0), 0.0);
            //                    break;
            //                case SoundType.TrainDynamic:
            //                    TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.SoundEvent(d, Data.Blocks[i].Sound[j].SoundBuffer, false, false, true, new Vector3D(0.0, 0.0, 0.0), Data.Blocks[i].Sound[j].Speed);
            //                    break;
            //            }
            //        }
            //    }
            //}

            // Turn
            if (routeData.Blocks[blockIdx].Turn != 0.0)
            {
                float ag = (float)Math.Atan(routeData.Blocks[blockIdx].Turn);
                playerRailDir = playerRailDir.Rotated(-ag);

                // World.RotatePlane(ref TrackManager.CurrentTrack.Elements[n].WorldDirection, cosag, sinag);
                // World.RotatePlane(ref TrackManager.CurrentTrack.Elements[n].WorldSide, cosag, sinag);
                // World.Cross(TrackManager.CurrentTrack.Elements[n].WorldDirection.X, TrackManager.CurrentTrack.Elements[n].WorldDirection.Y, TrackManager.CurrentTrack.Elements[n].WorldDirection.Z, TrackManager.CurrentTrack.Elements[n].WorldSide.X, TrackManager.CurrentTrack.Elements[n].WorldSide.Y, TrackManager.CurrentTrack.Elements[n].WorldSide.Z, out TrackManager.CurrentTrack.Elements[n].WorldUp.X, out TrackManager.CurrentTrack.Elements[n].WorldUp.Y, out TrackManager.CurrentTrack.Elements[n].WorldUp.Z);
                TrackManager.CurrentTrack.Elements[n].WorldDirection = TrackManager.CurrentTrack.Elements[n].WorldDirection.Rotated(Vector3.Left, -ag);
                TrackManager.CurrentTrack.Elements[n].WorldSide = TrackManager.CurrentTrack.Elements[n].WorldSide.Rotated(Vector3.Left, -ag);
                TrackManager.CurrentTrack.Elements[n].WorldUp = TrackManager.CurrentTrack.Elements[n].WorldDirection.Cross(TrackManager.CurrentTrack.Elements[n].WorldSide);
            }

            // Pitch
            if (routeData.Blocks[blockIdx].Pitch != 0.0)
            {
                TrackManager.CurrentTrack.Elements[n].Pitch = routeData.Blocks[blockIdx].Pitch;
            }
            else
            {
                TrackManager.CurrentTrack.Elements[n].Pitch = 0.0;
            }

            // Curves
            double a = 0.0;
            double c = routeData.BlockInterval;
            double h = 0.0;
            if (worldTrackElement.CurveRadius != 0.0 & routeData.Blocks[blockIdx].Pitch != 0.0)
            {
                double d = routeData.BlockInterval;
                double p = routeData.Blocks[blockIdx].Pitch;
                double r = worldTrackElement.CurveRadius;
                double s = d / Math.Sqrt(1.0 + p * p);
                h = s * p;
                double b = s / Math.Abs(r);
                c = Math.Sqrt(2.0 * r * r * (1.0 - Math.Cos(b)));
                a = 0.5 * (double)Math.Sign(r) * b;
                playerRailDir = playerRailDir.Rotated((float)-a);
            }
            else if (worldTrackElement.CurveRadius != 0.0)
            {
                double d = routeData.BlockInterval;
                double r = worldTrackElement.CurveRadius;
                double b = d / Math.Abs(r);
                c = Math.Sqrt(2.0 * r * r * (1.0 - Math.Cos(b)));
                a = 0.5 * (double)Math.Sign(r) * b;
                playerRailDir = playerRailDir.Rotated((float)-a);
            }
            else if (routeData.Blocks[blockIdx].Pitch != 0.0)
            {
                double p = routeData.Blocks[blockIdx].Pitch;
                double d = routeData.BlockInterval;
                c = d / Math.Sqrt(1.0 + p * p);
                h = c * p;
            }

            float trackYaw = (float)Math.Atan2(playerRailDir.x, playerRailDir.y);
            float trackPitch = (float)Math.Atan(routeData.Blocks[blockIdx].Pitch);

            Transform groundTransformation = new Transform(Basis.Identity,  Vector3.Zero);
            groundTransformation = groundTransformation.Rotated(Vector3.Up, -(float)trackYaw);

            Transform trackTransformation = new Transform(Basis.Identity,  Vector3.Zero);
            trackTransformation = trackTransformation.Rotated(Vector3.Up, -(float)trackYaw);
            trackTransformation = trackTransformation.Rotated(Vector3.Right, (float)trackPitch);

            Transform nullTransformation = new Transform(Basis.Identity, Vector3.Zero);

            // Ground
            if (!previewOnly)
            {
                int cb = (int)Math.Floor((double)blockIdx + 0.001);
                int ci = (cb % routeData.Blocks[blockIdx].Cycle.Length + routeData.Blocks[blockIdx].Cycle.Length) % routeData.Blocks[blockIdx].Cycle.Length;
                int gi = routeData.Blocks[blockIdx].Cycle[ci];
                if (gi >= 0 & gi < routeData.Structure.Ground.Length)
                {
                    if (routeData.Structure.Ground[gi] != null)
                    {
                        //ObjectManager.CreateObject(Data.Structure.Ground[Data.Blocks[i].Cycle[ci]], Position + new Vector3D(0.0, -Data.Blocks[i].Height, 0.0), GroundTransformation, NullTransformation, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, StartingDistance);
                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.Ground[routeData.Blocks[blockIdx].Cycle[ci]],
                                                                    playerRailPos + new Vector3(0.0f, (float)-routeData.Blocks[blockIdx].Height, 0.0f), 
                                                                    groundTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, 
                                                                    routeData.BlockInterval, startingDistance);
                    }
                }
            }


            // ground-aligned free objects
            if (!previewOnly)
            {
                for (int j = 0; j < routeData.Blocks[blockIdx].GroundFreeObj.Length; j++)
                {
                    int sttype = routeData.Blocks[blockIdx].GroundFreeObj[j].Type;
                    double d = routeData.Blocks[blockIdx].GroundFreeObj[j].TrackPosition - startingDistance;
                    double dx = routeData.Blocks[blockIdx].GroundFreeObj[j].X;
                    double dy = routeData.Blocks[blockIdx].GroundFreeObj[j].Y;
                    
                    Vector3 wpos = playerRailPos + new Vector3( (float)(playerRailDir.x * d + playerRailDir.y * dx), 
                                                                (float)(dy - routeData.Blocks[blockIdx].Height), 
                                                                (float)(playerRailDir.y * d - playerRailDir.x * dx));
                    double tpos = routeData.Blocks[blockIdx].GroundFreeObj[j].TrackPosition;

                    Transform gafTran = new Transform(Basis.Identity, Vector3.Zero);
                    gafTran = gafTran.Rotated(Vector3.Up, -(float)routeData.Blocks[blockIdx].GroundFreeObj[j].Yaw);
                    gafTran = gafTran.Rotated(Vector3.Right, (float)routeData.Blocks[blockIdx].GroundFreeObj[j].Pitch);
                    gafTran = gafTran.Rotated(Vector3.Forward, (float)routeData.Blocks[blockIdx].GroundFreeObj[j].Roll);

                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FreeObj[sttype], wpos, groundTransformation, gafTran, 
                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, tpos);
                }
            }

            // Rail-aligned objects
            if (!previewOnly)
            {
                for (int railIdx = 0; railIdx < routeData.Blocks[blockIdx].Rail.Length; railIdx++)
                {
                    if (railIdx > 0 && !routeData.Blocks[blockIdx].Rail[railIdx].RailStart) continue;

                    // Rail
                    // RailIdx == 0 - player rail
                    // RailIdx > 0  - auxiliary rails
                    Vector3 railPosition;
                    Transform railTransformation;// = new Transform(Basis.Identity, Vector3.Zero);
                    double planar, updown;
                    if (railIdx == 0)
                    {
                        // Rail 0 (player rail)
                        planar = 0.0;               
                        updown = 0.0;
                        railTransformation = new Transform(trackTransformation.basis, Vector3.Zero);
                        railTransformation = railTransformation.Rotated(Vector3.Up, -(float)planar);            // TODO: seems a waste, always rotating by 0.0 planar / updown ???
                        railTransformation = railTransformation.Rotated(Vector3.Right, -(float)updown);
                        railPosition = playerRailPos;
                    }
                    else
                    {
                        // Rails 1-infinity (auxiliary rails)
                        double x = routeData.Blocks[blockIdx].Rail[railIdx].RailStartX;
                        double y = routeData.Blocks[blockIdx].Rail[railIdx].RailStartY;
                        Vector3 offset = new Vector3((float)(playerRailDir.y * x), (float)y, (float)(playerRailDir.x * x));
                        railPosition = playerRailPos + offset;
                        double dh;
                        if (blockIdx < routeData.Blocks.Length - 1 && routeData.Blocks[blockIdx + 1].Rail.Length > railIdx)
                        {
                            // take orientation of upcoming block into account
                            Vector2 direction2 = playerRailDir;
                            Vector3 position2 = playerRailPos;
                            position2.x += playerRailDir.x * (float)c;
                            position2.y += (float)h;
                            position2.z -= playerRailDir.y * (float)c;
                            if (a != 0.0)
                            {
                                direction2 = direction2.Rotated(-(float)a);
                            }
                            if (routeData.Blocks[blockIdx + 1].Turn != 0.0)
                            {
                                double ag = Math.Atan(routeData.Blocks[blockIdx + 1].Turn);
                                direction2 = direction2.Rotated(-(float)ag);
                            }
                            double a2 = 0.0;
                            double c2 = routeData.BlockInterval;
                            double h2 = 0.0;
                            if (routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius != 0.0 & routeData.Blocks[blockIdx + 1].Pitch != 0.0)
                            {
                                double d2 = routeData.BlockInterval;
                                double p2 = routeData.Blocks[blockIdx + 1].Pitch;
                                double r2 = routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius;
                                double s2 = d2 / Math.Sqrt(1.0 + p2 * p2);
                                h2 = s2 * p2;
                                double b2 = s2 / Math.Abs(r2);
                                c2 = Math.Sqrt(2.0 * r2 * r2 * (1.0 - Math.Cos(b2)));
                                a2 = 0.5 * (double)Math.Sign(r2) * b2;
                                direction2 = direction2.Rotated(-(float)a2);
                            }
                            else if (routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius != 0.0)
                            {
                                double d2 = routeData.BlockInterval;
                                double r2 = routeData.Blocks[blockIdx + 1].CurrentTrackState.CurveRadius;
                                double b2 = d2 / Math.Abs(r2);
                                c2 = Math.Sqrt(2.0 * r2 * r2 * (1.0 - Math.Cos(b2)));
                                a2 = 0.5 * (double)Math.Sign(r2) * b2;
                                direction2 = direction2.Rotated(-(float)a2);
                            }
                            else if (routeData.Blocks[blockIdx + 1].Pitch != 0.0)
                            {
                                double p2 = routeData.Blocks[blockIdx + 1].Pitch;
                                double d2 = routeData.BlockInterval;
                                c2 = d2 / Math.Sqrt(1.0 + p2 * p2);
                                h2 = c2 * p2;
                            }

                            //These generate a compiler warning, as secondary tracks do not generate yaw, as they have no
                            //concept of a curve, but rather are a straight line between two points
                            //TODO: Revist the handling of secondary tracks ==> !!BACKWARDS INCOMPATIBLE!!
                            /*
                            double TrackYaw2 = Math.Atan2(Direction2.X, Direction2.Y);
                            double TrackPitch2 = Math.Atan(Data.Blocks[i + 1].Pitch);
                            World.Transformation GroundTransformation2 = new World.Transformation(TrackYaw2, 0.0, 0.0);
                            World.Transformation TrackTransformation2 = new World.Transformation(TrackYaw2, TrackPitch2, 0.0);
                             */
                            // double x2 = routeData.Blocks[i + 1].Rail[j].RailEndX;
                            // double y2 = routeData.Blocks[i + 1].Rail[j].RailEndY;
                            // Vector3 offset2 = new Vector3((float)(direction2.y * x2), (float)y2, -(float)(direction2.x * x2));
                            // Vector3 pos2 = position2 + offset2;
                            // float rx = pos2.x - pos.x;
                            // float ry = pos2.y - pos.y;
                            // float rz = pos2.z - pos.z;
                            // // World.Normalize(ref rx, ref ry, ref rz);
                            // railTransformation.basis.z = new Vector3(rx, ry, rz).Normalized();
                            // railTransformation.basis.x = new Vector3(rz, 0.0f, -rx);
                            // //World.Normalize(ref RailTransformation.X.X, ref RailTransformation.X.Z);
                            // railTransformation.basis.x = railTransformation.basis.x.Normalized();

                            // railTransformation.basis.y = railTransformation.basis.z.Cross(railTransformation.basis.x);
                            //railTransformation = new Transform(trackTransformation.basis, Vector3.Zero);
                            double dx = routeData.Blocks[blockIdx + 1].Rail[railIdx].RailEndX - routeData.Blocks[blockIdx].Rail[railIdx].RailStartX;
                            double dy = routeData.Blocks[blockIdx + 1].Rail[railIdx].RailEndY - routeData.Blocks[blockIdx].Rail[railIdx].RailStartY;
                            
                            planar = Math.Atan(dx / c);
                            dh = dy / c;
                            updown = Math.Atan(dh);

                            railTransformation = trackTransformation.Rotated(Vector3.Up, -(float)planar);
                            railTransformation = railTransformation.Rotated(Vector3.Right, (float)updown);
                        }
                        else
                        {
                            planar = 0.0;
                            dh = 0.0;
                            updown = 0.0;
                            railTransformation = new Transform(trackTransformation.basis, Vector3.Zero);
                        }
                    }

                    // Place rail (either player rail or auxiliary rail)
                    if (routeData.Blocks[blockIdx].RailType[railIdx] < routeData.Structure.Rail.Length)
                    {
                        if (routeData.Structure.Rail[routeData.Blocks[blockIdx].RailType[railIdx]] != null)
                        {
                            // ObjectManager.CreateObject(routeData.Structure.Rail[routeData.Blocks[i].RailType[j]], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.Rail[routeData.Blocks[blockIdx].RailType[railIdx]], 
                                                                        railPosition, railTransformation, nullTransformation, 
                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                        }
                    }

                    // points of interest
                    //for (int k = 0; k < Data.Blocks[i].PointsOfInterest.Length; k++)
                    //{
                    //    if (Data.Blocks[i].PointsOfInterest[k].RailIndex == j)
                    //    {
                    //        double d = Data.Blocks[i].PointsOfInterest[k].TrackPosition - StartingDistance;
                    //        double x = Data.Blocks[i].PointsOfInterest[k].X;
                    //        double y = Data.Blocks[i].PointsOfInterest[k].Y;
                    //        int m = Game.PointsOfInterest.Length;
                    //        Array.Resize<Game.PointOfInterest>(ref Game.PointsOfInterest, m + 1);
                    //        Game.PointsOfInterest[m].TrackPosition = Data.Blocks[i].PointsOfInterest[k].TrackPosition;
                    //        if (i < Data.Blocks.Length - 1 && Data.Blocks[i + 1].Rail.Length > j)
                    //        {
                    //            double dx = Data.Blocks[i + 1].Rail[j].RailEndX - Data.Blocks[i].Rail[j].RailStartX;
                    //            double dy = Data.Blocks[i + 1].Rail[j].RailEndY - Data.Blocks[i].Rail[j].RailStartY;
                    //            dx = Data.Blocks[i].Rail[j].RailStartX + d / Data.BlockInterval * dx;
                    //            dy = Data.Blocks[i].Rail[j].RailStartY + d / Data.BlockInterval * dy;
                    //            Game.PointsOfInterest[m].TrackOffset = new Vector3D(x + dx, y + dy, 0.0);
                    //        }
                    //        else
                    //        {
                    //            double dx = Data.Blocks[i].Rail[j].RailStartX;
                    //            double dy = Data.Blocks[i].Rail[j].RailStartY;
                    //            Game.PointsOfInterest[m].TrackOffset = new Vector3D(x + dx, y + dy, 0.0);
                    //        }
                    //        Game.PointsOfInterest[m].TrackYaw = Data.Blocks[i].PointsOfInterest[k].Yaw + planar;
                    //        Game.PointsOfInterest[m].TrackPitch = Data.Blocks[i].PointsOfInterest[k].Pitch + updown;
                    //        Game.PointsOfInterest[m].TrackRoll = Data.Blocks[i].PointsOfInterest[k].Roll;
                    //        Game.PointsOfInterest[m].Text = Data.Blocks[i].PointsOfInterest[k].Text;
                    //    }
                    //}

                    // poles
                    if (routeData.Blocks[blockIdx].RailPole.Length > railIdx && routeData.Blocks[blockIdx].RailPole[railIdx].Exists)
                    {
                        double dz = startingDistance / routeData.Blocks[blockIdx].RailPole[railIdx].Interval;
                        dz -= Math.Floor(dz + 0.5);
                        if (dz >= -0.01 & dz <= 0.01)
                        {
                            if (routeData.Blocks[blockIdx].RailPole[railIdx].Mode == 0)
                            {
                                UnifiedObject poleObj = ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.Poles[0][routeData.Blocks[blockIdx].RailPole[railIdx].Type], railPosition, railTransformation, nullTransformation, 
                                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                if (routeData.Blocks[blockIdx].RailPole[railIdx].Location > 0)
                                    poleObj.Mirror();
                            }
                            else
                            {
                                int m = routeData.Blocks[blockIdx].RailPole[railIdx].Mode;
                                double dx = -routeData.Blocks[blockIdx].RailPole[railIdx].Location * 3.8;
                                double wa = Math.Atan2(playerRailDir.y, playerRailDir.x) - planar;
                                double wx = Math.Cos(wa);
                                double wy = Math.Tan(updown);
                                double wz = Math.Sin(wa);
                                Calc.Normalize(ref wx, ref wy, ref wz);
                                double sx = playerRailDir.y;
                                double sy = 0.0;
                                double sz = -playerRailDir.x;
                                Vector3 wpos = railPosition + new Vector3((float)(sx * dx + wx * dz), (float)(sy * dx + wy * dz), (float)(sz * dx + wz * dz));
                                int type = routeData.Blocks[blockIdx].RailPole[railIdx].Type;
                                //ObjectManager.CreateObject(routeData.Structure.Poles[m][type], wpos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.Poles[m][type], railPosition, railTransformation, nullTransformation, 
                                                                            routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                            }
                        }
                    }

                    // walls
                    if (routeData.Blocks[blockIdx].RailWall.Length > railIdx && routeData.Blocks[blockIdx].RailWall[railIdx].Exists)
                    {
                        if (routeData.Blocks[blockIdx].RailWall[railIdx].Direction <= 0)
                        {                
                            //ObjectManager.CreateObject(routeData.Structure.WallL[routeData.Blocks[i].RailWall[j].Type], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.WallL[routeData.Blocks[blockIdx].RailWall[railIdx].Type], railPosition, railTransformation, nullTransformation, 
                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                        }
                        if (routeData.Blocks[blockIdx].RailWall[railIdx].Direction >= 0)
                        {                            
                            //ObjectManager.CreateObject(routeData.Structure.WallR[routeData.Blocks[i].RailWall[j].Type], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.WallR[routeData.Blocks[blockIdx].RailWall[railIdx].Type], railPosition, railTransformation, nullTransformation, 
                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                        }
                    }

                    // dikes
                    if (routeData.Blocks[blockIdx].RailDike.Length > railIdx && routeData.Blocks[blockIdx].RailDike[railIdx].Exists)
                    {
                        if (routeData.Blocks[blockIdx].RailDike[railIdx].Direction <= 0)
                        {
                            //ObjectManager.CreateObject(routeData.Structure.DikeL[routeData.Blocks[i].RailDike[j].Type], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.blockInterval, startingDistance);
                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.DikeL[routeData.Blocks[blockIdx].RailDike[railIdx].Type], railPosition, railTransformation, nullTransformation,
                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                        }
                        if (routeData.Blocks[blockIdx].RailDike[railIdx].Direction >= 0)
                        {
                            //ObjectManager.CreateObject(routeData.Structure.DikeR[routeData.Blocks[i].RailDike[j].Type], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.blockInterval, startingDistance);
                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.DikeR[routeData.Blocks[blockIdx].RailDike[railIdx].Type], railPosition, railTransformation, nullTransformation,
                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                        }
                    }

                    // sounds
                    //if (j == 0)
                    //{
                    //    for (int k = 0; k < Data.Blocks[i].Sound.Length; k++)
                    //    {
                    //        if (Data.Blocks[i].Sound[k].Type == SoundType.World)
                    //        {
                    //            if (Data.Blocks[i].Sound[k].SoundBuffer != null)
                    //            {
                    //                double d = Data.Blocks[i].Sound[k].TrackPosition - StartingDistance;
                    //                double dx = Data.Blocks[i].Sound[k].X;
                    //                double dy = Data.Blocks[i].Sound[k].Y;
                    //                double wa = Math.Atan2(Direction.Y, Direction.X) - planar;
                    //                double wx = Math.Cos(wa);
                    //                double wy = Math.Tan(updown);
                    //                double wz = Math.Sin(wa);
                    //                World.Calc.Normalize(ref wx, ref wy, ref wz);
                    //                double sx = Direction.Y;
                    //                double sy = 0.0;
                    //                double sz = -Direction.X;
                    //                double ux, uy, uz;
                    //                World.Cross(wx, wy, wz, sx, sy, sz, out ux, out uy, out uz);
                    //                Vector3D wpos = pos + new Vector3D(sx * dx + ux * dy + wx * d, sy * dx + uy * dy + wy * d, sz * dx + uz * dy + wz * d);
                    //                Sounds.PlaySound(Data.Blocks[i].Sound[k].SoundBuffer, 1.0, 1.0, wpos, true);
                    //            }
                    //        }
                    //    }
                    //}

                    // forms
                    for (int k = 0; k < routeData.Blocks[blockIdx].Form.Length; k++)
                    {
                        // primary rail
                        if (routeData.Blocks[blockIdx].Form[k].PrimaryRail == railIdx)
                        {
                            if (routeData.Blocks[blockIdx].Form[k].SecondaryRail == Form.SecondaryRailStub)
                            {
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormL.Length || routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateObject(routeData.Structure.FormL[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(rootForRouteObjects, routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                    {
                                        if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofL.Length || routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                        {
                                            GD.Print("RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                        }
                                        else
                                        {
                                            //ObjectManager.CreateObject(routeData.Structure.RoofL[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        }
                                    }
                                }
                            }
                            else if (routeData.Blocks[blockIdx].Form[k].SecondaryRail == Form.SecondaryRailL)
                            {
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormL.Length || routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateObject(routeData.Structure.FormL[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                }
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormCL.Length || routeData.Structure.FormCL[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormCL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateStaticObject(routeData.Structure.FormCL[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormCL[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                }

                                if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                {
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofL.Length || routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                    {
                                        GD.Print("RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.CreateObject(routeData.Structure.RoofL[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    }
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofCL.Length || routeData.Structure.RoofCL[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                    {
                                        GD.Print("RoofStructureIndex references a RoofCL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.CreateStaticObject(routeData.Structure.RoofCL[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofCL[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    }
                                }
                            }
                            else if (routeData.Blocks[blockIdx].Form[k].SecondaryRail == Form.SecondaryRailR)
                            {
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormR.Length || routeData.Structure.FormR[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateObject(routeData.Structure.FormR[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormR[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                }
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormCR.Length || routeData.Structure.FormCR[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormCR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateStaticObject(routeData.Structure.FormCR[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormCR[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                }
                                if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                {
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofR.Length || routeData.Structure.RoofR[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                    {
                                        GD.Print("RoofStructureIndex references a RoofR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.CreateObject(routeData.Structure.RoofR[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofR[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    }
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofCR.Length || routeData.Structure.RoofCR[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                    {
                                        GD.Print("RoofStructureIndex references a RoofCR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.CreateStaticObject(routeData.Structure.RoofCR[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofCR[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    }
                                }
                            }
                            else if (routeData.Blocks[blockIdx].Form[k].SecondaryRail > 0)
                            {
                                int p = routeData.Blocks[blockIdx].Form[k].PrimaryRail;
                                double px0 = p > 0 ? routeData.Blocks[blockIdx].Rail[p].RailStartX : 0.0;
                                double px1 = p > 0 ? routeData.Blocks[blockIdx + 1].Rail[p].RailEndX : 0.0;
                                int s = routeData.Blocks[blockIdx].Form[k].SecondaryRail;
                                if (s < 0 || s >= routeData.Blocks[blockIdx].Rail.Length || !routeData.Blocks[blockIdx].Rail[s].RailStart)
                                {
                                    GD.Print("RailIndex2 is out of range in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName);
                                }
                                else
                                {
                                    double sx0 = routeData.Blocks[blockIdx].Rail[s].RailStartX;
                                    double sx1 = routeData.Blocks[blockIdx + 1].Rail[s].RailEndX;
                                    double d0 = sx0 - px0;
                                    double d1 = sx1 - px1;
                                    if (d0 < 0.0)
                                    {
                                        if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormL.Length || routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                        {
                                            GD.Print("FormStructureIndex references a FormL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                        }
                                        else
                                        {
                                            //ObjectManager.CreateObject(routeData.Structure.FormL[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation,
                                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        }

                                        if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormCL.Length || routeData.Structure.FormCL[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                        {
                                            GD.Print("FormStructureIndex references a FormCL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                        }
                                        else
                                        {
                                            // TODO instantiate object
                                            //ObjectManager.StaticObject FormC = GetTransformedStaticObject(routeData.Structure.FormCL[routeData.Blocks[i].Form[k].FormType], d0, d1);
                                            //ObjectManager.CreateStaticObject(FormC, pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        }
                                        if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                        {
                                            if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofL.Length || routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                            {
                                                GD.Print("RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                            }
                                            else
                                            {
                                                //ObjectManager.CreateObject(routeData.Structure.RoofL[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                                ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                            routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            }
                                            if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofCL.Length || routeData.Structure.RoofCL[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                            {
                                                GD.Print("RoofStructureIndex references a RoofCL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                            }
                                            else
                                            {
                                                // TODO instantiate object
                                                //ObjectManager.StaticObject RoofC = GetTransformedStaticObject(routeData.Structure.RoofCL[routeData.Blocks[i].Form[k].RoofType], d0, d1);
                                                //ObjectManager.CreateStaticObject(RoofC, pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            }
                                        }
                                    }
                                    else if (d0 > 0.0)
                                    {
                                        if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormR.Length || routeData.Structure.FormR[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                        {
                                            GD.Print("FormStructureIndex references a FormR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                        }
                                        else
                                        {
                                            //ObjectManager.CreateObject(routeData.Structure.FormR[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormR[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                        routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        }
                                        if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormCR.Length || routeData.Structure.FormCR[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                        {
                                            GD.Print("FormStructureIndex references a FormCR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                        }
                                        else
                                        {
                                            // TODO Instantiate object
                                            //ObjectManager.StaticObject FormC = GetTransformedStaticObject(routeData.Structure.FormCR[routeData.Blocks[i].Form[k].FormType], d0, d1);
                                            //ObjectManager.CreateStaticObject(FormC, pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        }
                                        if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                        {
                                            if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofR.Length || routeData.Structure.RoofR[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                            {
                                                GD.Print("RoofStructureIndex references a RoofR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                            }
                                            else
                                            {
                                                //ObjectManager.CreateObject(routeData.Structure.RoofR[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                                ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofR[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                            routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            }
                                            if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofCR.Length || routeData.Structure.RoofCR[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                            {
                                                GD.Print("RoofStructureIndex references a RoofCR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                            }
                                            else
                                            {
                                                //ObjectManager.StaticObject RoofC = GetTransformedStaticObject(routeData.Structure.RoofCR[routeData.Blocks[i].Form[k].RoofType], d0, d1);
                                                //ObjectManager.CreateStaticObject(RoofC, pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // secondary rail
                        if (routeData.Blocks[blockIdx].Form[k].SecondaryRail == railIdx)
                        {
                            int p = routeData.Blocks[blockIdx].Form[k].PrimaryRail;
                            double px = p > 0 ? routeData.Blocks[blockIdx].Rail[p].RailStartX : 0.0;
                            int s = routeData.Blocks[blockIdx].Form[k].SecondaryRail;
                            double sx = routeData.Blocks[blockIdx].Rail[s].RailStartX;
                            double d = px - sx;
                            if (d < 0.0)
                            {
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormL.Length || routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateObject(routeData.Structure.FormL[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormL[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                }
                                if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                {
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofL.Length || routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                    {
                                        GD.Print("RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.CreateObject(routeData.Structure.RoofL[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofL[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    }
                                }
                            }
                            else
                            {
                                if (routeData.Blocks[blockIdx].Form[k].FormType >= routeData.Structure.FormR.Length || routeData.Structure.FormR[routeData.Blocks[blockIdx].Form[k].FormType] == null)
                                {
                                    GD.Print("FormStructureIndex references a FormR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                }
                                else
                                {
                                    //ObjectManager.CreateObject(routeData.Structure.FormR[routeData.Blocks[i].Form[k].FormType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FormR[routeData.Blocks[blockIdx].Form[k].FormType], railPosition, railTransformation, nullTransformation, 
                                                                                routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                }
                                if (routeData.Blocks[blockIdx].Form[k].RoofType > 0)
                                {
                                    if (routeData.Blocks[blockIdx].Form[k].RoofType >= routeData.Structure.RoofR.Length || routeData.Structure.RoofR[routeData.Blocks[blockIdx].Form[k].RoofType] == null)
                                    {
                                        GD.Print("RoofStructureIndex references a RoofR not loaded in Track.Form at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.CreateObject(routeData.Structure.RoofR[routeData.Blocks[i].Form[k].RoofType], pos, railTransformation, nullTransformation, routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                        ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.RoofR[routeData.Blocks[blockIdx].Form[k].RoofType], railPosition, railTransformation, nullTransformation, 
                                                                                    routeData.AccurateObjectDisposal, startingDistance, endingDistance, routeData.BlockInterval, startingDistance);
                                    }
                                }
                            }
                        }
                    }

                    // cracks
                    for (int k = 0; k < routeData.Blocks[blockIdx].Crack.Length; k++)
                    {
                        if (routeData.Blocks[blockIdx].Crack[k].PrimaryRail == railIdx)
                        {
                            int p = routeData.Blocks[blockIdx].Crack[k].PrimaryRail;
                            double px0 = p > 0 ? routeData.Blocks[blockIdx].Rail[p].RailStartX : 0.0;
                            double px1 = p > 0 ? routeData.Blocks[blockIdx + 1].Rail[p].RailEndX : 0.0;
                            int s = routeData.Blocks[blockIdx].Crack[k].SecondaryRail;
                            if (s < 0 || s >= routeData.Blocks[blockIdx].Rail.Length || !routeData.Blocks[blockIdx].Rail[s].RailStart)
                            {
                                GD.Print("RailIndex2 is out of range in Track.Crack at track position " + startingDistance.ToString(Culture) + " in file " + fileName);
                            }
                            else
                            {
                                double sx0 = routeData.Blocks[blockIdx].Rail[s].RailStartX;
                                double sx1 = routeData.Blocks[blockIdx + 1].Rail[s].RailEndX;
                                double d0 = sx0 - px0;
                                double d1 = sx1 - px1;
                                if (d0 < 0.0)
                                {
                                    if (routeData.Blocks[blockIdx].Crack[k].Type >= routeData.Structure.CrackL.Length || routeData.Structure.CrackL[routeData.Blocks[blockIdx].Crack[k].Type] == null)
                                    {
                                        GD.Print("CrackStructureIndex references a CrackL not loaded in Track.Crack at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.StaticObject Crack = GetTransformedStaticObject(routeData.Structure.CrackL[routeData.Blocks[i].Crack[k].Type], d0, d1);
                                        //ObjectManager.CreateStaticObject(Crack, pos, RailTransformation, NullTransformation, routeData.AccurateObjectDisposal, StartingDistance, EndingDistance, routeData.BlockInterval, StartingDistance);
                                    }
                                }
                                else if (d0 > 0.0)
                                {
                                    if (routeData.Blocks[blockIdx].Crack[k].Type >= routeData.Structure.CrackR.Length || routeData.Structure.CrackR[routeData.Blocks[blockIdx].Crack[k].Type] == null)
                                    {
                                        GD.Print("CrackStructureIndex references a CrackR not loaded in Track.Crack at track position " + startingDistance.ToString(Culture) + " in file " + fileName + ".");
                                    }
                                    else
                                    {
                                        //ObjectManager.StaticObject Crack = GetTransformedStaticObject(routeData.Structure.CrackR[routeData.Blocks[i].Crack[k].Type], d0, d1);
                                        //ObjectManager.CreateStaticObject(Crack, pos, RailTransformation, NullTransformation, routeData.AccurateObjectDisposal, StartingDistance, EndingDistance, routeData.BlockInterval, StartingDistance);
                                    }
                                }
                            }
                        }
                    }

                    // free objects
                    if (routeData.Blocks[blockIdx].RailFreeObj.Length > railIdx && routeData.Blocks[blockIdx].RailFreeObj[railIdx] != null)
                    {
                        for (int k = 0; k < routeData.Blocks[blockIdx].RailFreeObj[railIdx].Length; k++)
                        {
                            int sttype = routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].Type;
                            double dx = routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].X;
                            double dy = routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].Y;
                            double dz = routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].TrackPosition - startingDistance;
                            Vector3 wpos = railPosition;
                            wpos.x += (float)(dx * railTransformation.basis.x.x + dy * railTransformation.basis.y.x + dz * railTransformation.basis.z.x);
                            wpos.y += (float)(dx * railTransformation.basis.x.y + dy * railTransformation.basis.y.y + dz * railTransformation.basis.z.y);
                            wpos.z += (float)(dx * railTransformation.basis.x.z + dy * railTransformation.basis.y.z + dz * railTransformation.basis.z.z);
                            double tpos = routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].TrackPosition;
                            //ObjectManager.CreateObject(Data.Structure.FreeObj[sttype], wpos, RailTransformation, new World.Transformation(Data.Blocks[i].RailFreeObj[j][k].Yaw, Data.Blocks[i].RailFreeObj[j][k].Pitch, Data.Blocks[i].RailFreeObj[j][k].Roll), -1, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, tpos, 1.0, false);
                            
                            // new Transformation((float)routeData.Blocks[i].RailFreeObj[j][k].Yaw, (float)routeData.Blocks[i].RailFreeObj[j][k].Pitch, (float)routeData.Blocks[i].RailFreeObj[j][k].Roll)
                            Transform foTran = new Transform(Basis.Identity, new Vector3(0, 0, 0));
                            foTran = foTran.Rotated(Vector3.Up, -(float)routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].Yaw);
                            foTran = foTran.Rotated(Vector3.Right, (float)routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].Pitch);
                            foTran = foTran.Rotated(Vector3.Forward, (float)routeData.Blocks[blockIdx].RailFreeObj[railIdx][k].Roll);

                            ObjectManager.Instance.InstantiateObject(   rootForRouteObjects, routeData.Structure.FreeObj[sttype], wpos, railTransformation, foTran, false, 
                                                                        startingDistance, endingDistance, routeData.BlockInterval, tpos);
                        }
                    }

                    // transponder objects
                    //if (j == 0)
                    //{
                    //    for (int k = 0; k < Data.Blocks[i].Transponder.Length; k++)
                    //    {
                    //        ObjectManager.UnifiedObject obj = null;
                    //        if (Data.Blocks[i].Transponder[k].ShowDefaultObject)
                    //        {
                    //            switch (Data.Blocks[i].Transponder[k].Type)
                    //            {
                    //                case 0: obj = TransponderS; break;
                    //                case 1: obj = TransponderSN; break;
                    //                case 2: obj = TransponderFalseStart; break;
                    //                case 3: obj = TransponderPOrigin; break;
                    //                case 4: obj = TransponderPStop; break;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            int b = Data.Blocks[i].Transponder[k].BeaconStructureIndex;
                    //            if (b >= 0 & b < Data.Structure.Beacon.Length)
                    //            {
                    //                obj = Data.Structure.Beacon[b];
                    //            }
                    //        }
                    //        if (obj != null)
                    //        {
                    //            double dx = Data.Blocks[i].Transponder[k].X;
                    //            double dy = Data.Blocks[i].Transponder[k].Y;
                    //            double dz = Data.Blocks[i].Transponder[k].TrackPosition - StartingDistance;
                    //            Vector3D wpos = pos;
                    //            wpos.X += dx * RailTransformation.X.X + dy * RailTransformation.Y.X + dz * RailTransformation.Z.X;
                    //            wpos.Y += dx * RailTransformation.X.Y + dy * RailTransformation.Y.Y + dz * RailTransformation.Z.Y;
                    //            wpos.Z += dx * RailTransformation.X.Z + dy * RailTransformation.Y.Z + dz * RailTransformation.Z.Z;
                    //            double tpos = Data.Blocks[i].Transponder[k].TrackPosition;
                    //            if (Data.Blocks[i].Transponder[k].ShowDefaultObject)
                    //            {
                    //                double b = 0.25 + 0.75 * GetBrightness(ref Data, tpos);
                    //                ObjectManager.CreateObject(obj, wpos, RailTransformation, new World.Transformation(Data.Blocks[i].Transponder[k].Yaw, Data.Blocks[i].Transponder[k].Pitch, Data.Blocks[i].Transponder[k].Roll), -1, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //            }
                    //            else
                    //            {
                    //                ObjectManager.CreateObject(obj, wpos, RailTransformation, new World.Transformation(Data.Blocks[i].Transponder[k].Yaw, Data.Blocks[i].Transponder[k].Pitch, Data.Blocks[i].Transponder[k].Roll), Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, tpos);
                    //            }
                    //        }
                    //    }
                    //}

                    // sections/signals/transponders
                    //if (j == 0)
                    //{
                    //    // signals
                    //    for (int k = 0; k < Data.Blocks[i].Signal.Length; k++)
                    //    {
                    //        SignalData sd;
                    //        if (Data.Blocks[i].Signal[k].SignalCompatibilityObjectIndex >= 0)
                    //        {
                    //            sd = Data.CompatibilitySignalData[Data.Blocks[i].Signal[k].SignalCompatibilityObjectIndex];
                    //        }
                    //        else
                    //        {
                    //            sd = Data.SignalData[Data.Blocks[i].Signal[k].SignalObjectIndex];
                    //        }
                    //        // objects
                    //        double dz = Data.Blocks[i].Signal[k].TrackPosition - StartingDistance;
                    //        if (Data.Blocks[i].Signal[k].ShowPost)
                    //        {
                    //            // post
                    //            double dx = Data.Blocks[i].Signal[k].X;
                    //            Vector3D wpos = pos;
                    //            wpos.X += dx * RailTransformation.X.X + dz * RailTransformation.Z.X;
                    //            wpos.Y += dx * RailTransformation.X.Y + dz * RailTransformation.Z.Y;
                    //            wpos.Z += dx * RailTransformation.X.Z + dz * RailTransformation.Z.Z;
                    //            double tpos = Data.Blocks[i].Signal[k].TrackPosition;
                    //            double b = 0.25 + 0.75 * GetBrightness(ref Data, tpos);
                    //            ObjectManager.CreateStaticObject(SignalPost, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //        }
                    //        if (Data.Blocks[i].Signal[k].ShowObject)
                    //        {
                    //            // signal object
                    //            double dx = Data.Blocks[i].Signal[k].X;
                    //            double dy = Data.Blocks[i].Signal[k].Y;
                    //            Vector3D wpos = pos;
                    //            wpos.X += dx * RailTransformation.X.X + dy * RailTransformation.Y.X + dz * RailTransformation.Z.X;
                    //            wpos.Y += dx * RailTransformation.X.Y + dy * RailTransformation.Y.Y + dz * RailTransformation.Z.Y;
                    //            wpos.Z += dx * RailTransformation.X.Z + dy * RailTransformation.Y.Z + dz * RailTransformation.Z.Z;
                    //            double tpos = Data.Blocks[i].Signal[k].TrackPosition;
                    //            if (sd is AnimatedObjectSignalData)
                    //            {
                    //                AnimatedObjectSignalData aosd = (AnimatedObjectSignalData)sd;
                    //                ObjectManager.CreateObject(aosd.Objects, wpos, RailTransformation, new World.Transformation(Data.Blocks[i].Signal[k].Yaw, Data.Blocks[i].Signal[k].Pitch, Data.Blocks[i].Signal[k].Roll), Data.Blocks[i].Signal[k].Section, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, tpos, 1.0, false);
                    //            }
                    //            else if (sd is CompatibilitySignalData)
                    //            {
                    //                CompatibilitySignalData csd = (CompatibilitySignalData)sd;
                    //                if (csd.Numbers.Length != 0)
                    //                {
                    //                    double brightness = 0.25 + 0.75 * GetBrightness(ref Data, tpos);
                    //                    ObjectManager.AnimatedObjectCollection aoc = new ObjectManager.AnimatedObjectCollection();
                    //                    aoc.Objects = new ObjectManager.AnimatedObject[1];
                    //                    aoc.Objects[0] = new ObjectManager.AnimatedObject();
                    //                    aoc.Objects[0].States = new ObjectManager.AnimatedObjectState[csd.Numbers.Length];
                    //                    for (int l = 0; l < csd.Numbers.Length; l++)
                    //                    {
                    //                        aoc.Objects[0].States[l].Object = ObjectManager.CloneObject(csd.Objects[l]);
                    //                    }
                    //                    string expr = "";
                    //                    for (int l = 0; l < csd.Numbers.Length - 1; l++)
                    //                    {
                    //                        expr += "section " + csd.Numbers[l].ToString(Culture) + " <= " + l.ToString(Culture) + " ";
                    //                    }
                    //                    expr += (csd.Numbers.Length - 1).ToString(Culture);
                    //                    for (int l = 0; l < csd.Numbers.Length - 1; l++)
                    //                    {
                    //                        expr += " ?";
                    //                    }
                    //                    aoc.Objects[0].StateFunction = FunctionScripts.GetFunctionScriptFromPostfixNotation(expr);
                    //                    aoc.Objects[0].RefreshRate = 1.0 + 0.01 * Program.RandomNumberGenerator.NextDouble();
                    //                    ObjectManager.CreateObject(aoc, wpos, RailTransformation, new World.Transformation(Data.Blocks[i].Signal[k].Yaw, Data.Blocks[i].Signal[k].Pitch, Data.Blocks[i].Signal[k].Roll), Data.Blocks[i].Signal[k].Section, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, tpos, brightness, false);
                    //                }
                    //            }
                    //            else if (sd is Bve4SignalData)
                    //            {
                    //                Bve4SignalData b4sd = (Bve4SignalData)sd;
                    //                if (b4sd.SignalTextures.Length != 0)
                    //                {
                    //                    int m = Math.Max(b4sd.SignalTextures.Length, b4sd.GlowTextures.Length);
                    //                    int zn = 0;
                    //                    for (int l = 0; l < m; l++)
                    //                    {
                    //                        if (l < b4sd.SignalTextures.Length && b4sd.SignalTextures[l] != null || l < b4sd.GlowTextures.Length && b4sd.GlowTextures[l] != null)
                    //                        {
                    //                            zn++;
                    //                        }
                    //                    }
                    //                    ObjectManager.AnimatedObjectCollection aoc = new ObjectManager.AnimatedObjectCollection();
                    //                    aoc.Objects = new ObjectManager.AnimatedObject[1];
                    //                    aoc.Objects[0] = new ObjectManager.AnimatedObject();
                    //                    aoc.Objects[0].States = new ObjectManager.AnimatedObjectState[zn];
                    //                    int zi = 0;
                    //                    string expr = "";
                    //                    for (int l = 0; l < m; l++)
                    //                    {
                    //                        bool qs = l < b4sd.SignalTextures.Length && b4sd.SignalTextures[l] != null;
                    //                        bool qg = l < b4sd.GlowTextures.Length && b4sd.GlowTextures[l] != null;
                    //                        if (qs & qg)
                    //                        {
                    //                            ObjectManager.StaticObject so = ObjectManager.CloneObject(b4sd.BaseObject, b4sd.SignalTextures[l], null);
                    //                            ObjectManager.StaticObject go = ObjectManager.CloneObject(b4sd.GlowObject, b4sd.GlowTextures[l], null);
                    //                            ObjectManager.JoinObjects(ref so, go);
                    //                            aoc.Objects[0].States[zi].Object = so;
                    //                        }
                    //                        else if (qs)
                    //                        {
                    //                            ObjectManager.StaticObject so = ObjectManager.CloneObject(b4sd.BaseObject, b4sd.SignalTextures[l], null);
                    //                            aoc.Objects[0].States[zi].Object = so;
                    //                        }
                    //                        else if (qg)
                    //                        {
                    //                            ObjectManager.StaticObject go = ObjectManager.CloneObject(b4sd.GlowObject, b4sd.GlowTextures[l], null);
                    //                            aoc.Objects[0].States[zi].Object = go;
                    //                        }
                    //                        if (qs | qg)
                    //                        {
                    //                            if (zi < zn - 1)
                    //                            {
                    //                                expr += "section " + l.ToString(Culture) + " <= " + zi.ToString(Culture) + " ";
                    //                            }
                    //                            else
                    //                            {
                    //                                expr += zi.ToString(Culture);
                    //                            }
                    //                            zi++;
                    //                        }
                    //                    }
                    //                    for (int l = 0; l < zn - 1; l++)
                    //                    {
                    //                        expr += " ?";
                    //                    }
                    //                    aoc.Objects[0].StateFunction = FunctionScripts.GetFunctionScriptFromPostfixNotation(expr);
                    //                    aoc.Objects[0].RefreshRate = 1.0 + 0.01 * Program.RandomNumberGenerator.NextDouble();
                    //                    ObjectManager.CreateObject(aoc, wpos, RailTransformation, new World.Transformation(Data.Blocks[i].Signal[k].Yaw, Data.Blocks[i].Signal[k].Pitch, Data.Blocks[i].Signal[k].Roll), Data.Blocks[i].Signal[k].Section, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, Data.BlockInterval, tpos, 1.0, false);
                    //                }
                    //            }
                    //        }
                    //    }

                    //    // sections
                    //    for (int k = 0; k < Data.Blocks[i].Section.Length; k++)
                    //    {
                    //        int m = Game.Sections.Length;
                    //        Array.Resize<Game.Section>(ref Game.Sections, m + 1);
                    //        Game.Sections[m].SignalIndices = new int[] { };
                    //        // create associated transponders
                    //        for (int g = 0; g <= i; g++)
                    //        {
                    //            for (int l = 0; l < Data.Blocks[g].Transponder.Length; l++)
                    //            {
                    //                if (Data.Blocks[g].Transponder[l].Type != -1 & Data.Blocks[g].Transponder[l].Section == m)
                    //                {
                    //                    int o = TrackManager.CurrentTrack.Elements[n - i + g].Events.Length;
                    //                    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n - i + g].Events, o + 1);
                    //                    double dt = Data.Blocks[g].Transponder[l].TrackPosition - StartingDistance + (double)(i - g) * Data.BlockInterval;
                    //                    TrackManager.CurrentTrack.Elements[n - i + g].Events[o] = new TrackManager.TransponderEvent(dt, Data.Blocks[g].Transponder[l].Type, Data.Blocks[g].Transponder[l].Data, m, Data.Blocks[g].Transponder[l].ClipToFirstRedSection);
                    //                    Data.Blocks[g].Transponder[l].Type = -1;
                    //                }
                    //            }
                    //        }
                    //        // create section
                    //        Game.Sections[m].TrackPosition = Data.Blocks[i].Section[k].TrackPosition;
                    //        Game.Sections[m].Aspects = new Game.SectionAspect[Data.Blocks[i].Section[k].Aspects.Length];
                    //        for (int l = 0; l < Data.Blocks[i].Section[k].Aspects.Length; l++)
                    //        {
                    //            Game.Sections[m].Aspects[l].Number = Data.Blocks[i].Section[k].Aspects[l];
                    //            if (Data.Blocks[i].Section[k].Aspects[l] >= 0 & Data.Blocks[i].Section[k].Aspects[l] < Data.SignalSpeeds.Length)
                    //            {
                    //                Game.Sections[m].Aspects[l].Speed = Data.SignalSpeeds[Data.Blocks[i].Section[k].Aspects[l]];
                    //            }
                    //            else
                    //            {
                    //                Game.Sections[m].Aspects[l].Speed = double.PositiveInfinity;
                    //            }
                    //        }
                    //        Game.Sections[m].Type = Data.Blocks[i].Section[k].Type;
                    //        Game.Sections[m].CurrentAspect = -1;
                    //        if (m > 0)
                    //        {
                    //            Game.Sections[m].PreviousSection = m - 1;
                    //            Game.Sections[m - 1].NextSection = m;
                    //        }
                    //        else
                    //        {
                    //            Game.Sections[m].PreviousSection = -1;
                    //        }
                    //        Game.Sections[m].NextSection = -1;
                    //        Game.Sections[m].StationIndex = Data.Blocks[i].Section[k].DepartureStationIndex;
                    //        Game.Sections[m].Invisible = Data.Blocks[i].Section[k].Invisible;
                    //        Game.Sections[m].Trains = new TrainManager.Train[] { };
                    //        // create section change event
                    //        double d = Data.Blocks[i].Section[k].TrackPosition - StartingDistance;
                    //        int p = TrackManager.CurrentTrack.Elements[n].Events.Length;
                    //        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, p + 1);
                    //        TrackManager.CurrentTrack.Elements[n].Events[p] = new TrackManager.SectionChangeEvent(d, m - 1, m);
                    //    }
                    //    // transponders introduced after corresponding sections
                    //    for (int l = 0; l < Data.Blocks[i].Transponder.Length; l++)
                    //    {
                    //        if (Data.Blocks[i].Transponder[l].Type != -1)
                    //        {
                    //            int t = Data.Blocks[i].Transponder[l].Section;
                    //            if (t >= 0 & t < Game.Sections.Length)
                    //            {
                    //                int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
                    //                Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
                    //                double dt = Data.Blocks[i].Transponder[l].TrackPosition - StartingDistance;
                    //                TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.TransponderEvent(dt, Data.Blocks[i].Transponder[l].Type, Data.Blocks[i].Transponder[l].Data, t, Data.Blocks[i].Transponder[l].ClipToFirstRedSection);
                    //                Data.Blocks[i].Transponder[l].Type = -1;
                    //            }
                    //        }
                    //    }
                    //}

                    // limit
                    //if (j == 0)
                    //{
                    //    for (int k = 0; k < Data.Blocks[i].Limit.Length; k++)
                    //    {
                    //        if (Data.Blocks[i].Limit[k].Direction != 0)
                    //        {
                    //            double dx = 2.2 * (double)Data.Blocks[i].Limit[k].Direction;
                    //            double dz = Data.Blocks[i].Limit[k].TrackPosition - StartingDistance;
                    //            Vector3D wpos = pos;
                    //            wpos.X += dx * RailTransformation.X.X + dz * RailTransformation.Z.X;
                    //            wpos.Y += dx * RailTransformation.X.Y + dz * RailTransformation.Z.Y;
                    //            wpos.Z += dx * RailTransformation.X.Z + dz * RailTransformation.Z.Z;
                    //            double tpos = Data.Blocks[i].Limit[k].TrackPosition;
                    //            double b = 0.25 + 0.75 * GetBrightness(ref Data, tpos);
                    //            if (Data.Blocks[i].Limit[k].Speed <= 0.0 | Data.Blocks[i].Limit[k].Speed >= 1000.0)
                    //            {
                    //                ObjectManager.CreateStaticObject(LimitPostInfinite, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //            }
                    //            else
                    //            {
                    //                if (Data.Blocks[i].Limit[k].Cource < 0)
                    //                {
                    //                    ObjectManager.CreateStaticObject(LimitPostLeft, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //                }
                    //                else if (Data.Blocks[i].Limit[k].Cource > 0)
                    //                {
                    //                    ObjectManager.CreateStaticObject(LimitPostRight, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //                }
                    //                else
                    //                {
                    //                    ObjectManager.CreateStaticObject(LimitPostStraight, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //                }
                    //                double lim = Data.Blocks[i].Limit[k].Speed / Data.UnitOfSpeed;
                    //                if (lim < 10.0)
                    //                {
                    //                    int d0 = (int)Math.Round(lim);
                    //                    int o = ObjectManager.CreateStaticObject(LimitOneDigit, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, true);
                    //                    if (ObjectManager.Objects[o].Mesh.Materials.Length >= 1)
                    //                    {
                    //                        Textures.RegisterTexture(Path.Combine(LimitGraphicsPath, "limit_" + d0 + ".png"), out ObjectManager.Objects[o].Mesh.Materials[0].DaytimeTexture);
                    //                    }
                    //                }
                    //                else if (lim < 100.0)
                    //                {
                    //                    int d1 = (int)Math.Round(lim);
                    //                    int d0 = d1 % 10;
                    //                    d1 /= 10;
                    //                    int o = ObjectManager.CreateStaticObject(LimitTwoDigits, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, true);
                    //                    if (ObjectManager.Objects[o].Mesh.Materials.Length >= 1)
                    //                    {
                    //                        Textures.RegisterTexture(Path.Combine(LimitGraphicsPath, "limit_" + d1 + ".png"), out ObjectManager.Objects[o].Mesh.Materials[0].DaytimeTexture);
                    //                    }
                    //                    if (ObjectManager.Objects[o].Mesh.Materials.Length >= 2)
                    //                    {
                    //                        Textures.RegisterTexture(Path.Combine(LimitGraphicsPath, "limit_" + d0 + ".png"), out ObjectManager.Objects[o].Mesh.Materials[1].DaytimeTexture);
                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    int d2 = (int)Math.Round(lim);
                    //                    int d0 = d2 % 10;
                    //                    int d1 = (d2 / 10) % 10;
                    //                    d2 /= 100;
                    //                    int o = ObjectManager.CreateStaticObject(LimitThreeDigits, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, true);
                    //                    if (ObjectManager.Objects[o].Mesh.Materials.Length >= 1)
                    //                    {
                    //                        Textures.RegisterTexture(Path.Combine(LimitGraphicsPath, "limit_" + d2 + ".png"), out ObjectManager.Objects[o].Mesh.Materials[0].DaytimeTexture);
                    //                    }
                    //                    if (ObjectManager.Objects[o].Mesh.Materials.Length >= 2)
                    //                    {
                    //                        Textures.RegisterTexture(Path.Combine(LimitGraphicsPath, "limit_" + d1 + ".png"), out ObjectManager.Objects[o].Mesh.Materials[1].DaytimeTexture);
                    //                    }
                    //                    if (ObjectManager.Objects[o].Mesh.Materials.Length >= 3)
                    //                    {
                    //                        Textures.RegisterTexture(Path.Combine(LimitGraphicsPath, "limit_" + d0 + ".png"), out ObjectManager.Objects[o].Mesh.Materials[2].DaytimeTexture);
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    // stop
                    //if (j == 0)
                    //{
                    //    for (int k = 0; k < Data.Blocks[i].Stop.Length; k++)
                    //    {
                    //        if (Data.Blocks[i].Stop[k].Direction != 0)
                    //        {
                    //            double dx = 1.8 * (double)Data.Blocks[i].Stop[k].Direction;
                    //            double dz = Data.Blocks[i].Stop[k].TrackPosition - StartingDistance;
                    //            Vector3D wpos = pos;
                    //            wpos.X += dx * RailTransformation.X.X + dz * RailTransformation.Z.X;
                    //            wpos.Y += dx * RailTransformation.X.Y + dz * RailTransformation.Z.Y;
                    //            wpos.Z += dx * RailTransformation.X.Z + dz * RailTransformation.Z.Z;
                    //            double tpos = Data.Blocks[i].Stop[k].TrackPosition;
                    //            double b = 0.25 + 0.75 * GetBrightness(ref Data, tpos);
                    //            ObjectManager.CreateStaticObject(StopPost, wpos, RailTransformation, NullTransformation, Data.AccurateObjectDisposal, 0.0, StartingDistance, EndingDistance, Data.BlockInterval, tpos, b, false);
                    //        }
                    //    }
                    //}
                }
            }

            // finalize block
            playerRailPos.x += (float)(playerRailDir.x * c);
            playerRailPos.y += (float)h;
            playerRailPos.z -= (float)(playerRailDir.y * c);
            if (a != 0.0)
            {
                // Calc.Rotate(ref direction, Math.Cos(-a), Math.Sin(-a));
                playerRailDir = playerRailDir.Rotated(-(float)a);
            }
        }

        // orphaned transponders
        //if (!PreviewOnly)
        //{
        //    for (int i = Data.FirstUsedBlock; i < Data.Blocks.Length; i++)
        //    {
        //        for (int j = 0; j < Data.Blocks[i].Transponder.Length; j++)
        //        {
        //            if (Data.Blocks[i].Transponder[j].Type != -1)
        //            {
        //                int n = i - Data.FirstUsedBlock;
        //                int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
        //                Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
        //                double d = Data.Blocks[i].Transponder[j].TrackPosition - TrackManager.CurrentTrack.Elements[n].StartingTrackPosition;
        //                int s = Data.Blocks[i].Transponder[j].Section;
        //                if (s >= 0) s = -1;
        //                TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.TransponderEvent(d, Data.Blocks[i].Transponder[j].Type, Data.Blocks[i].Transponder[j].Data, s, Data.Blocks[i].Transponder[j].ClipToFirstRedSection);
        //                Data.Blocks[i].Transponder[j].Type = -1;
        //            }
        //        }
        //    }
        //}

        // insert station end events
        //for (int i = 0; i < Game.Stations.Length; i++)
        //{
        //    int j = Game.Stations[i].Stops.Length - 1;
        //    if (j >= 0)
        //    {
        //        double p = Game.Stations[i].Stops[j].TrackPosition + Game.Stations[i].Stops[j].ForwardTolerance + Data.BlockInterval;
        //        int k = (int)Math.Floor(p / (double)Data.BlockInterval) - Data.FirstUsedBlock;
        //        if (k >= 0 & k < Data.Blocks.Length)
        //        {
        //            double d = p - (double)(k + Data.FirstUsedBlock) * (double)Data.BlockInterval;
        //            int m = TrackManager.CurrentTrack.Elements[k].Events.Length;
        //            Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[k].Events, m + 1);
        //            TrackManager.CurrentTrack.Elements[k].Events[m] = new TrackManager.StationEndEvent(d, i);
        //        }
        //    }
        //}

        // create default point of interests
        //if (Game.PointsOfInterest.Length == 0)
        //{
        //    Game.PointsOfInterest = new OpenBve.Game.PointOfInterest[Game.Stations.Length];
        //    int n = 0;
        //    for (int i = 0; i < Game.Stations.Length; i++)
        //    {
        //        if (Game.Stations[i].Stops.Length != 0)
        //        {
        //            Game.PointsOfInterest[n].Text = Game.Stations[i].Name;
        //            Game.PointsOfInterest[n].TrackPosition = Game.Stations[i].Stops[0].TrackPosition;
        //            Game.PointsOfInterest[n].TrackOffset = new Vector3D(0.0, 2.8, 0.0);
        //            if (Game.Stations[i].OpenLeftDoors & !Game.Stations[i].OpenRightDoors)
        //            {
        //                Game.PointsOfInterest[n].TrackOffset.X = -2.5;
        //            }
        //            else if (!Game.Stations[i].OpenLeftDoors & Game.Stations[i].OpenRightDoors)
        //            {
        //                Game.PointsOfInterest[n].TrackOffset.X = 2.5;
        //            }
        //            n++;
        //        }
        //    }
        //    Array.Resize<Game.PointOfInterest>(ref Game.PointsOfInterest, n);
        //}

        // convert block-based cant into point-based cant
        //for (int i = CurrentTrackLength - 1; i >= 1; i--)
        //{
        //    if (TrackManager.CurrentTrack.Elements[i].CurveCant == 0.0)
        //    {
        //        TrackManager.CurrentTrack.Elements[i].CurveCant = TrackManager.CurrentTrack.Elements[i - 1].CurveCant;
        //    }
        //    else if (TrackManager.CurrentTrack.Elements[i - 1].CurveCant != 0.0)
        //    {
        //        if (Math.Sign(TrackManager.CurrentTrack.Elements[i - 1].CurveCant) == Math.Sign(TrackManager.CurrentTrack.Elements[i].CurveCant))
        //        {
        //            if (Math.Abs(TrackManager.CurrentTrack.Elements[i - 1].CurveCant) > Math.Abs(TrackManager.CurrentTrack.Elements[i].CurveCant))
        //            {
        //                TrackManager.CurrentTrack.Elements[i].CurveCant = TrackManager.CurrentTrack.Elements[i - 1].CurveCant;
        //            }
        //        }
        //        else
        //        {
        //            TrackManager.CurrentTrack.Elements[i].CurveCant = 0.5 * (TrackManager.CurrentTrack.Elements[i].CurveCant + TrackManager.CurrentTrack.Elements[i - 1].CurveCant);
        //        }
        //    }
        //}

        // finalize
        //Array.Resize<TrackManager.TrackElement>(ref TrackManager.CurrentTrack.Elements, CurrentTrackLength);
        //for (int i = 0; i < Game.Stations.Length; i++)
        //{
        //    if (Game.Stations[i].Stops.Length == 0 & Game.Stations[i].StopMode != Game.StationStopMode.AllPass)
        //    {
        //        GD.Print("Station " + Game.Stations[i].Name + " expects trains to stop but does not define stop points at track position " + Game.Stations[i].DefaultTrackPosition.ToString(Culture) + " in file " + FileName);
        //        Game.Stations[i].StopMode = Game.StationStopMode.AllPass;
        //    }
        //    if (Game.Stations[i].StationType == Game.StationType.ChangeEnds)
        //    {
        //        if (i < Game.Stations.Length - 1)
        //        {
        //            if (Game.Stations[i + 1].StopMode != Game.StationStopMode.AllStop)
        //            {
        //                GD.Print("Station " + Game.Stations[i].Name + " is marked as \"change ends\" but the subsequent station does not expect all trains to stop in file " + FileName);
        //                Game.Stations[i + 1].StopMode = Game.StationStopMode.AllStop;
        //            }
        //        }
        //        else
        //        {
        //            GD.Print("Station " + Game.Stations[i].Name + " is marked as \"change ends\" but there is no subsequent station defined in file " + FileName);
        //            Game.Stations[i].StationType = Game.StationType.Terminal;
        //        }
        //    }
        //}
        //if (Game.Stations.Length != 0)
        //{
        //    Game.Stations[Game.Stations.Length - 1].StationType = Game.StationType.Terminal;
        //}
        //if (TrackManager.CurrentTrack.Elements.Length != 0)
        //{
        //    int n = TrackManager.CurrentTrack.Elements.Length - 1;
        //    int m = TrackManager.CurrentTrack.Elements[n].Events.Length;
        //    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[n].Events, m + 1);
        //    TrackManager.CurrentTrack.Elements[n].Events[m] = new TrackManager.TrackEndEvent(Data.BlockInterval);
        //}

        // insert compatibility beacons
        //if (!PreviewOnly)
        //{
        //    List<TrackManager.TransponderEvent> transponders = new List<TrackManager.TransponderEvent>();
        //    bool atc = false;
        //    for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length; i++)
        //    {
        //        for (int j = 0; j < TrackManager.CurrentTrack.Elements[i].Events.Length; j++)
        //        {
        //            if (!atc)
        //            {
        //                if (TrackManager.CurrentTrack.Elements[i].Events[j] is TrackManager.StationStartEvent)
        //                {
        //                    TrackManager.StationStartEvent station = (TrackManager.StationStartEvent)TrackManager.CurrentTrack.Elements[i].Events[j];
        //                    if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Atc)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[i].Events, TrackManager.CurrentTrack.Elements[i].Events.Length + 2);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 0, 0, false);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 1, 0, false);
        //                        atc = true;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                if (TrackManager.CurrentTrack.Elements[i].Events[j] is TrackManager.StationStartEvent)
        //                {
        //                    TrackManager.StationStartEvent station = (TrackManager.StationStartEvent)TrackManager.CurrentTrack.Elements[i].Events[j];
        //                    if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Ats)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[i].Events, TrackManager.CurrentTrack.Elements[i].Events.Length + 2);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 2, 0, false);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 3, 0, false);
        //                    }
        //                }
        //                else if (TrackManager.CurrentTrack.Elements[i].Events[j] is TrackManager.StationEndEvent)
        //                {
        //                    TrackManager.StationEndEvent station = (TrackManager.StationEndEvent)TrackManager.CurrentTrack.Elements[i].Events[j];
        //                    if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Atc)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[i].Events, TrackManager.CurrentTrack.Elements[i].Events.Length + 2);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 1, 0, false);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 2, 0, false);
        //                    }
        //                    else if (Game.Stations[station.StationIndex].SafetySystem == Game.SafetySystem.Ats)
        //                    {
        //                        Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[i].Events, TrackManager.CurrentTrack.Elements[i].Events.Length + 2);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 2] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 3, 0, false);
        //                        TrackManager.CurrentTrack.Elements[i].Events[TrackManager.CurrentTrack.Elements[i].Events.Length - 1] = new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcTrackStatus, 0, 0, false);
        //                        atc = false;
        //                    }
        //                }
        //                else if (TrackManager.CurrentTrack.Elements[i].Events[j] is TrackManager.LimitChangeEvent)
        //                {
        //                    TrackManager.LimitChangeEvent limit = (TrackManager.LimitChangeEvent)TrackManager.CurrentTrack.Elements[i].Events[j];
        //                    int speed = (int)Math.Round(Math.Min(4095.0, 3.6 * limit.NextSpeedLimit));
        //                    int distance = Math.Min(1048575, (int)Math.Round(TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + limit.TrackPositionDelta));
        //                    unchecked
        //                    {
        //                        int value = (int)((uint)speed | ((uint)distance << 12));
        //                        transponders.Add(new TrackManager.TransponderEvent(0.0, TrackManager.SpecialTransponderTypes.AtcSpeedLimit, value, 0, false));
        //                    }
        //                }
        //            }
        //            if (TrackManager.CurrentTrack.Elements[i].Events[j] is TrackManager.TransponderEvent)
        //            {
        //                TrackManager.TransponderEvent transponder = TrackManager.CurrentTrack.Elements[i].Events[j] as TrackManager.TransponderEvent;
        //                if (transponder.Type == TrackManager.SpecialTransponderTypes.InternalAtsPTemporarySpeedLimit)
        //                {
        //                    int speed = Math.Min(4095, transponder.Data);
        //                    int distance = Math.Min(1048575, (int)Math.Round(TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + transponder.TrackPositionDelta));
        //                    unchecked
        //                    {
        //                        int value = (int)((uint)speed | ((uint)distance << 12));
        //                        transponder.DontTriggerAnymore = true;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    int n = TrackManager.CurrentTrack.Elements[0].Events.Length;
        //    Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[0].Events, n + transponders.Count);
        //    for (int i = 0; i < transponders.Count; i++)
        //    {
        //        TrackManager.CurrentTrack.Elements[0].Events[n + i] = transponders[i];
        //    }
        //}

        // cant
        if (!previewOnly)
        {
           ComputeCantTangents();
           int subdivisions = (int)Math.Floor(routeData.BlockInterval / 5.0);
           if (subdivisions >= 2)
           {
               SmoothenOutTurns(subdivisions);
               ComputeCantTangents();
           }
        }
    }

    #endregion
  

    private static void ComputeCantTangents() {
			if (TrackManager.CurrentTrack.Elements.Length == 1) {
				TrackManager.CurrentTrack.Elements[0].CurveCantTangent = 0.0;
			} else if (TrackManager.CurrentTrack.Elements.Length != 0) {
				double[] deltas = new double[TrackManager.CurrentTrack.Elements.Length - 1];
				for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length - 1; i++) {
					deltas[i] = TrackManager.CurrentTrack.Elements[i + 1].CurveCant - TrackManager.CurrentTrack.Elements[i].CurveCant;
				}
				double[] tangents = new double[TrackManager.CurrentTrack.Elements.Length];
				tangents[0] = deltas[0];
				tangents[TrackManager.CurrentTrack.Elements.Length - 1] = deltas[TrackManager.CurrentTrack.Elements.Length - 2];
				for (int i = 1; i < TrackManager.CurrentTrack.Elements.Length - 1; i++) {
					tangents[i] = 0.5 * (deltas[i - 1] + deltas[i]);
				}
				for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length - 1; i++) {
					if (deltas[i] == 0.0) {
						tangents[i] = 0.0;
						tangents[i + 1] = 0.0;
					} else {
						double a = tangents[i] / deltas[i];
						double b = tangents[i + 1] / deltas[i];
						if (a * a + b * b > 9.0) {
							double t = 3.0 / Math.Sqrt(a * a + b * b);
							tangents[i] = t * a * deltas[i];
							tangents[i + 1] = t * b * deltas[i];
						}
					}
				}
				for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length; i++) {
					TrackManager.CurrentTrack.Elements[i].CurveCantTangent = tangents[i];
				}
			}
		}

    private static void SmoothenOutTurns(int subdivisions)
    {
        if (subdivisions < 2)
        {
            throw new InvalidOperationException();
        }

        // subdivide track
        int length = TrackManager.CurrentTrack.Elements.Length;
        int newLength = (length - 1) * subdivisions + 1;
        double[] midpointsTrackPositions = new double[newLength];
        Vector3[] midpointsWorldPositions = new Vector3[newLength];
        Vector3[] midpointsWorldDirections = new Vector3[newLength];
        Vector3[] midpointsWorldUps = new Vector3[newLength];
        Vector3[] midpointsWorldSides = new Vector3[newLength];
        double[] midpointsCant = new double[newLength];
        for (int i = 0; i < newLength; i++)
        {
            int m = i % subdivisions;
            if (m != 0)
            {
                int q = i / subdivisions;
                TrackManager.TrackFollower follower = new TrackManager.TrackFollower();
                double r = (double)m / (double)subdivisions;
                double p = (1.0 - r) * TrackManager.CurrentTrack.Elements[q].StartingTrackPosition + r * TrackManager.CurrentTrack.Elements[q + 1].StartingTrackPosition;
                TrackManager.UpdateTrackFollower(ref follower, -1.0, true, false);
                TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                midpointsTrackPositions[i] = p;
                midpointsWorldPositions[i] = follower.WorldPosition;
                midpointsWorldDirections[i] = follower.WorldDirection;
                midpointsWorldUps[i] = follower.WorldUp;
                midpointsWorldSides[i] = follower.WorldSide;
                midpointsCant[i] = follower.CurveCant;
            }
        }
        Array.Resize<TrackManager.TrackElement>(ref TrackManager.CurrentTrack.Elements, newLength);
        for (int i = length - 1; i >= 1; i--)
        {
            TrackManager.CurrentTrack.Elements[subdivisions * i] = TrackManager.CurrentTrack.Elements[i];
        }
        for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length; i++)
        {
            int m = i % subdivisions;
            if (m != 0)
            {
                int q = i / subdivisions;
                int j = q * subdivisions;
                TrackManager.CurrentTrack.Elements[i] = TrackManager.CurrentTrack.Elements[j];
                TrackManager.CurrentTrack.Elements[i].Events = new TrackManager.GeneralEvent[] { };
                TrackManager.CurrentTrack.Elements[i].StartingTrackPosition = midpointsTrackPositions[i];
                TrackManager.CurrentTrack.Elements[i].WorldPosition = midpointsWorldPositions[i];
                TrackManager.CurrentTrack.Elements[i].WorldDirection = midpointsWorldDirections[i];
                TrackManager.CurrentTrack.Elements[i].WorldUp = midpointsWorldUps[i];
                TrackManager.CurrentTrack.Elements[i].WorldSide = midpointsWorldSides[i];
                TrackManager.CurrentTrack.Elements[i].CurveCant = midpointsCant[i];
                TrackManager.CurrentTrack.Elements[i].CurveCantTangent = 0.0;
            }
        }
        // find turns
        bool[] isTurn = new bool[TrackManager.CurrentTrack.Elements.Length];
        {
            TrackManager.TrackFollower follower = new TrackManager.TrackFollower();
            for (int i = 1; i < TrackManager.CurrentTrack.Elements.Length - 1; i++)
            {
                int m = i % subdivisions;
                if (m == 0)
                {
                    double p = 0.00000001 * TrackManager.CurrentTrack.Elements[i - 1].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition;
                    TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                    Vector3 d1 = TrackManager.CurrentTrack.Elements[i].WorldDirection;
                    Vector3 d2 = follower.WorldDirection;
                    Vector3 d = d1 - d2;
                    double t = d.x * d.y + d.z * d.z;
                    const double e = 0.0001;
                    if (t > e)
                    {
                        isTurn[i] = true;
                    }
                }
            }
        }
        // replace turns by curves
        double totalShortage = 0.0;
        for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length; i++)
        {
            if (isTurn[i])
            {
                // estimate radius
                Vector3 AP = TrackManager.CurrentTrack.Elements[i - 1].WorldPosition;
                Vector3 AS = TrackManager.CurrentTrack.Elements[i - 1].WorldSide;
                Vector3 BP = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition;
                Vector3 BS = TrackManager.CurrentTrack.Elements[i + 1].WorldSide;
                Vector3 S = AS - BS;
                double rx;
                if (S.x * S.x > 0.000001)
                {
                    rx = (BP.x - AP.x) / S.x;
                }
                else
                {
                    rx = 0.0;
                }
                double rz;
                if (S.z * S.z > 0.000001)
                {
                    rz = (BP.z - AP.z) / S.z;
                }
                else
                {
                    rz = 0.0;
                }
                if (rx != 0.0 | rz != 0.0)
                {
                    double r;
                    if (rx != 0.0 & rz != 0.0)
                    {
                        if (Math.Sign(rx) == Math.Sign(rz))
                        {
                            double f = rx / rz;
                            if (f > -1.1 & f < -0.9 | f > 0.9 & f < 1.1)
                            {
                                r = Math.Sqrt(Math.Abs(rx * rz)) * Math.Sign(rx);
                            }
                            else
                            {
                                r = 0.0;
                            }
                        }
                        else
                        {
                            r = 0.0;
                        }
                    }
                    else if (rx != 0.0)
                    {
                        r = rx;
                    }
                    else
                    {
                        r = rz;
                    }
                    if (r * r > 1.0)
                    {
                        // apply radius
                        TrackManager.TrackFollower follower = new TrackManager.TrackFollower();
                        TrackManager.CurrentTrack.Elements[i - 1].CurveRadius = r;
                        double p = 0.00000001 * TrackManager.CurrentTrack.Elements[i - 1].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition;
                        TrackManager.UpdateTrackFollower(ref follower, p - 1.0, true, false);
                        TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                        TrackManager.CurrentTrack.Elements[i].CurveRadius = r;
                        //TrackManager.CurrentTrack.Elements[i].CurveCant = TrackManager.CurrentTrack.Elements[i].CurveCant;
                        //TrackManager.CurrentTrack.Elements[i].CurveCantInterpolation = TrackManager.CurrentTrack.Elements[i].CurveCantInterpolation;
                        TrackManager.CurrentTrack.Elements[i].WorldPosition = follower.WorldPosition;
                        TrackManager.CurrentTrack.Elements[i].WorldDirection = follower.WorldDirection;
                        TrackManager.CurrentTrack.Elements[i].WorldUp = follower.WorldUp;
                        TrackManager.CurrentTrack.Elements[i].WorldSide = follower.WorldSide;
                        // iterate to shorten track element length
                        p = 0.00000001 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition;
                        TrackManager.UpdateTrackFollower(ref follower, p - 1.0, true, false);
                        TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                        Vector3 d = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - follower.WorldPosition;
                        double bestT = d.x * d.x + d.y * d.y + d.z * d.z;
                        int bestJ = 0;
                        int n = 1000;
                        double a = 1.0 / (double)n * (TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition - TrackManager.CurrentTrack.Elements[i].StartingTrackPosition);
                        for (int j = 1; j < n - 1; j++)
                        {
                            TrackManager.UpdateTrackFollower(ref follower, TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition - (double)j * a, true, false);
                            d = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - follower.WorldPosition;
                            double t = d.x * d.x + d.y * d.y + d.z * d.z;
                            if (t < bestT)
                            {
                                bestT = t;
                                bestJ = j;
                            }
                            else
                            {
                                break;
                            }
                        }
                        double s = (double)bestJ * a;
                        for (int j = i + 1; j < TrackManager.CurrentTrack.Elements.Length; j++)
                        {
                            TrackManager.CurrentTrack.Elements[j].StartingTrackPosition -= s;
                        }
                        totalShortage += s;
                        // introduce turn to compensate for curve
                        p = 0.00000001 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition;
                        TrackManager.UpdateTrackFollower(ref follower, p - 1.0, true, false);
                        TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                        Vector3 AB = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - follower.WorldPosition;
                        Vector3 AC = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - TrackManager.CurrentTrack.Elements[i].WorldPosition;
                        Vector3 BC = follower.WorldPosition - TrackManager.CurrentTrack.Elements[i].WorldPosition;
                        double sa = Math.Sqrt(BC.x * BC.x + BC.z * BC.z);
                        double sb = Math.Sqrt(AC.x * AC.x + AC.z * AC.z);
                        double sc = Math.Sqrt(AB.x * AB.x + AB.z * AB.z);
                        double denominator = 2.0 * sa * sb;
                        if (denominator != 0.0)
                        {
                            double originalAngle;
                            {
                                double value = (sa * sa + sb * sb - sc * sc) / denominator;
                                if (value < -1.0)
                                {
                                    originalAngle = Math.PI;
                                }
                                else if (value > 1.0)
                                {
                                    originalAngle = 0;
                                }
                                else
                                {
                                    originalAngle = Math.Acos(value);
                                }
                            }
                            TrackManager.TrackElement originalTrackElement = TrackManager.CurrentTrack.Elements[i];
                            bestT = double.MaxValue;
                            bestJ = 0;
                            for (int j = -1; j <= 1; j++)
                            {
                                double g = (double)j * originalAngle;
                                double cosg = Math.Cos(g);
                                double sing = Math.Sin(g);
                                TrackManager.CurrentTrack.Elements[i] = originalTrackElement;
                                // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldDirection.X, ref TrackManager.CurrentTrack.Elements[i].WorldDirection.Y, ref TrackManager.CurrentTrack.Elements[i].WorldDirection.Z, 0.0, 1.0, 0.0, cosg, sing);
                                // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldUp.X, ref TrackManager.CurrentTrack.Elements[i].WorldUp.Y, ref TrackManager.CurrentTrack.Elements[i].WorldUp.Z, 0.0, 1.0, 0.0, cosg, sing);
                                // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldSide.X, ref TrackManager.CurrentTrack.Elements[i].WorldSide.Y, ref TrackManager.CurrentTrack.Elements[i].WorldSide.Z, 0.0, 1.0, 0.0, cosg, sing);
                                p = 0.00000001 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition;
                                TrackManager.UpdateTrackFollower(ref follower, p - 1.0, true, false);
                                TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                                d = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - follower.WorldPosition;
                                double t = d.x * d.x + d.y * d.y + d.z * d.z;
                                if (t < bestT)
                                {
                                    bestT = t;
                                    bestJ = j;
                                }
                            }
                            {
                                double newAngle = (double)bestJ * originalAngle;
                                double cosg = Math.Cos(newAngle);
                                double sing = Math.Sin(newAngle);
                                TrackManager.CurrentTrack.Elements[i] = originalTrackElement;
                                // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldDirection.X, ref TrackManager.CurrentTrack.Elements[i].WorldDirection.Y, ref TrackManager.CurrentTrack.Elements[i].WorldDirection.Z, 0.0, 1.0, 0.0, cosg, sing);
                                // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldUp.X, ref TrackManager.CurrentTrack.Elements[i].WorldUp.Y, ref TrackManager.CurrentTrack.Elements[i].WorldUp.Z, 0.0, 1.0, 0.0, cosg, sing);
                                // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldSide.X, ref TrackManager.CurrentTrack.Elements[i].WorldSide.Y, ref TrackManager.CurrentTrack.Elements[i].WorldSide.Z, 0.0, 1.0, 0.0, cosg, sing);
                            }
                            // iterate again to further shorten track element length
                            p = 0.00000001 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition;
                            TrackManager.UpdateTrackFollower(ref follower, p - 1.0, true, false);
                            TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                            d = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - follower.WorldPosition;
                            bestT = d.x * d.x + d.y * d.y + d.z * d.z;
                            bestJ = 0;
                            n = 1000;
                            a = 1.0 / (double)n * (TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition - TrackManager.CurrentTrack.Elements[i].StartingTrackPosition);
                            for (int j = 1; j < n - 1; j++)
                            {
                                TrackManager.UpdateTrackFollower(ref follower, TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition - (double)j * a, true, false);
                                d = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - follower.WorldPosition;
                                double t = d.x * d.x + d.y * d.y + d.z * d.z;
                                if (t < bestT)
                                {
                                    bestT = t;
                                    bestJ = j;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            s = (double)bestJ * a;
                            for (int j = i + 1; j < TrackManager.CurrentTrack.Elements.Length; j++)
                            {
                                TrackManager.CurrentTrack.Elements[j].StartingTrackPosition -= s;
                            }
                            totalShortage += s;
                        }
                        // compensate for height difference
                        p = 0.00000001 * TrackManager.CurrentTrack.Elements[i].StartingTrackPosition + 0.99999999 * TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition;
                        TrackManager.UpdateTrackFollower(ref follower, p - 1.0, true, false);
                        TrackManager.UpdateTrackFollower(ref follower, p, true, false);
                        Vector3 d1 = TrackManager.CurrentTrack.Elements[i + 1].WorldPosition - TrackManager.CurrentTrack.Elements[i].WorldPosition;
                        double a1 = Math.Atan(d1.y / Math.Sqrt(d1.x * d1.x + d1.z * d1.z));
                        Vector3 d2 = follower.WorldPosition - TrackManager.CurrentTrack.Elements[i].WorldPosition;
                        double a2 = Math.Atan(d2.y / Math.Sqrt(d2.x * d2.x + d2.z * d2.z));
                        double b = a2 - a1;
                        if (b * b > 0.00000001)
                        {
                            double cosa = Math.Cos(b);
                            double sina = Math.Sin(b);
                            // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldDirection.X, ref TrackManager.CurrentTrack.Elements[i].WorldDirection.Y, ref TrackManager.CurrentTrack.Elements[i].WorldDirection.Z, TrackManager.CurrentTrack.Elements[i].WorldSide.X, TrackManager.CurrentTrack.Elements[i].WorldSide.Y, TrackManager.CurrentTrack.Elements[i].WorldSide.Z, cosa, sina);
                            // World.Rotate(ref TrackManager.CurrentTrack.Elements[i].WorldUp.X, ref TrackManager.CurrentTrack.Elements[i].WorldUp.Y, ref TrackManager.CurrentTrack.Elements[i].WorldUp.Z, TrackManager.CurrentTrack.Elements[i].WorldSide.X, TrackManager.CurrentTrack.Elements[i].WorldSide.Y, TrackManager.CurrentTrack.Elements[i].WorldSide.Z, cosa, sina);
                        }
                    }
                }
            }
        }
        // correct events
        // for (int i = 0; i < TrackManager.CurrentTrack.Elements.Length - 1; i++)
        // {
        //     double startingTrackPosition = TrackManager.CurrentTrack.Elements[i].StartingTrackPosition;
        //     double endingTrackPosition = TrackManager.CurrentTrack.Elements[i + 1].StartingTrackPosition;
        //     for (int j = 0; j < TrackManager.CurrentTrack.Elements[i].Events.Length; j++)
        //     {
        //         double p = startingTrackPosition + TrackManager.CurrentTrack.Elements[i].Events[j].TrackPositionDelta;
        //         if (p >= endingTrackPosition)
        //         {
        //             int len = TrackManager.CurrentTrack.Elements[i + 1].Events.Length;
        //             Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[i + 1].Events, len + 1);
        //             TrackManager.CurrentTrack.Elements[i + 1].Events[len] = TrackManager.CurrentTrack.Elements[i].Events[j];
        //             TrackManager.CurrentTrack.Elements[i + 1].Events[len].TrackPositionDelta += startingTrackPosition - endingTrackPosition;
        //             for (int k = j; k < TrackManager.CurrentTrack.Elements[i].Events.Length - 1; k++)
        //             {
        //                 TrackManager.CurrentTrack.Elements[i].Events[k] = TrackManager.CurrentTrack.Elements[i].Events[k + 1];
        //             }
        //             len = TrackManager.CurrentTrack.Elements[i].Events.Length;
        //             Array.Resize<TrackManager.GeneralEvent>(ref TrackManager.CurrentTrack.Elements[i].Events, len - 1);
        //             j--;
        //         }
        //     }
        // }
    }

    private static void CreateMissingBlocks(ref RouteData routeData, ref int blocksUsed, int toIndex, bool previewOnly)
    {
        if (toIndex >= blocksUsed)
        {
            while (routeData.Blocks.Length <= toIndex)
            {
                Array.Resize<Block>(ref routeData.Blocks, routeData.Blocks.Length << 1);
            }
            for (int i = blocksUsed; i <= toIndex; i++)
            {
                routeData.Blocks[i] = new Block();
                if (!previewOnly)
                {
                    routeData.Blocks[i].Background = -1;
                    routeData.Blocks[i].Brightness = new Brightness[] { };
                    //Data.Blocks[i].Fog = Data.Blocks[i - 1].Fog;
                    routeData.Blocks[i].FogDefined = false;
                    routeData.Blocks[i].Cycle = routeData.Blocks[i - 1].Cycle;
                    routeData.Blocks[i].Height = double.NaN;
                }
                routeData.Blocks[i].RailType = new int[routeData.Blocks[i - 1].RailType.Length];
                for (int j = 0; j < routeData.Blocks[i].RailType.Length; j++)
                {
                    routeData.Blocks[i].RailType[j] = routeData.Blocks[i - 1].RailType[j];
                }
                routeData.Blocks[i].Rail = new Rail[routeData.Blocks[i - 1].Rail.Length];
                for (int j = 0; j < routeData.Blocks[i].Rail.Length; j++)
                {
                    routeData.Blocks[i].Rail[j].RailStart = routeData.Blocks[i - 1].Rail[j].RailStart;
                    routeData.Blocks[i].Rail[j].RailStartX = routeData.Blocks[i - 1].Rail[j].RailStartX;
                    routeData.Blocks[i].Rail[j].RailStartY = routeData.Blocks[i - 1].Rail[j].RailStartY;
                    routeData.Blocks[i].Rail[j].RailStartRefreshed = false;
                    routeData.Blocks[i].Rail[j].RailEnd = false;
                    routeData.Blocks[i].Rail[j].RailEndX = routeData.Blocks[i - 1].Rail[j].RailStartX;
                    routeData.Blocks[i].Rail[j].RailEndY = routeData.Blocks[i - 1].Rail[j].RailStartY;
                }
                if (!previewOnly)
                {
                    routeData.Blocks[i].RailWall = new WallDike[routeData.Blocks[i - 1].RailWall.Length];
                    for (int j = 0; j < routeData.Blocks[i].RailWall.Length; j++)
                    {
                        routeData.Blocks[i].RailWall[j] = routeData.Blocks[i - 1].RailWall[j];
                    }
                    routeData.Blocks[i].RailDike = new WallDike[routeData.Blocks[i - 1].RailDike.Length];
                    for (int j = 0; j < routeData.Blocks[i].RailDike.Length; j++)
                    {
                        routeData.Blocks[i].RailDike[j] = routeData.Blocks[i - 1].RailDike[j];
                    }
                    routeData.Blocks[i].RailPole = new Pole[routeData.Blocks[i - 1].RailPole.Length];
                    for (int j = 0; j < routeData.Blocks[i].RailPole.Length; j++)
                    {
                        routeData.Blocks[i].RailPole[j] = routeData.Blocks[i - 1].RailPole[j];
                    }
                    routeData.Blocks[i].Form = new Form[] { };
                    routeData.Blocks[i].Crack = new Crack[] { };
                    routeData.Blocks[i].Signal = new Signal[] { };
                    routeData.Blocks[i].Section = new Section[] { };
                    routeData.Blocks[i].Sound = new Sound[] { };
                    routeData.Blocks[i].Transponder = new Transponder[] { };
                    routeData.Blocks[i].RailFreeObj = new FreeObj[][] { };
                    routeData.Blocks[i].GroundFreeObj = new FreeObj[] { };
                    routeData.Blocks[i].PointsOfInterest = new PointOfInterest[] { };
                }
                routeData.Blocks[i].Pitch = routeData.Blocks[i - 1].Pitch;
                routeData.Blocks[i].Limit = new Limit[] { };
                routeData.Blocks[i].Stop = new Stop[] { };
                routeData.Blocks[i].Station = -1;
                routeData.Blocks[i].StationPassAlarm = false;
                routeData.Blocks[i].CurrentTrackState = routeData.Blocks[i - 1].CurrentTrackState;
                routeData.Blocks[i].Turn = 0.0;
                routeData.Blocks[i].Accuracy = routeData.Blocks[i - 1].Accuracy;
                routeData.Blocks[i].AdhesionMultiplier = routeData.Blocks[i - 1].AdhesionMultiplier;
            }
            blocksUsed = toIndex + 1;
        }
    }

    public static Material CreateSkyboxMaterial(Texture t)
    {
        // Material result = new Material(Shader.Find("RenderFX/Skybox"));
        // result.SetTexture("_FrontTex", t);
        // result.SetTexture("_BackTex", t);
        // result.SetTexture("_LeftTex", t);
        // result.SetTexture("_RightTex", t);
        // result.SetTexture("_UpTex", t);
        // result.SetTexture("_DownTex", t);
        // return result;
        return null;
    }
}