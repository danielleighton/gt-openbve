using Godot;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;



public static class CsvRwRouteParser
{

    /// <summary>
    /// TODO - move this
    /// </summary>
    public enum Direction
    {
        Left = -1,
        Both = 0,
        Right = 1,
        None = 2,
        Invalid = int.MaxValue
    }
    internal static Direction FindDirection(string direction, string command, bool isWallDike, int line, string file)
    {

        direction = direction.Trim();
        switch (direction.ToUpperInvariant())
        {
            case "-1":
            case "L":
            case "LEFT":
                return CsvRwRouteParser.Direction.Left;
            case "B":
            case "BOTH":
                return CsvRwRouteParser.Direction.Both;
            case "+1":
            case "1":
            case "R":
            case "RIGHT":
                return CsvRwRouteParser.Direction.Right;
            case "0":
                // BVE is inconsistent: Walls / Dikes use 0 for *both* sides, stations use 0 for none....
                return isWallDike ? CsvRwRouteParser.Direction.Both : CsvRwRouteParser.Direction.None;
            case "N":
            case "NONE":
            case "NEITHER":
                return CsvRwRouteParser.Direction.None;
            default:
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Direction is invalid in " + command + " at line " + line + " in file " + file);
                return CsvRwRouteParser.Direction.Invalid;

        }
    }


    #region Preprocessing (CsvRwRouteParser.Preprocess.cs)

    /// <summary>Preprocesses the options contained within a route file</summary>
    /// <param name="expressions">The initial list of expressions</param>
    /// <param name="routeData">The finalized route data</param>
    /// <param name="unitOfLength">The units of length conversion factor to be applied</param>
    /// <param name="previewOnly">Whether this is a preview only</param>
    private static void PreprocessOptions(Expression[] expressions, bool isRW, ref RouteData routeData, ref float[] unitOfLength, bool previewOnly)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        string section = "";
        bool sectionAlwaysPrefix = false;

        // process expressions
        for (int j = 0; j < expressions.Length; j++)
        {
            if (isRW && expressions[j].Text.StartsWith("[") && expressions[j].Text.EndsWith("]"))
            {
                section = expressions[j].Text.Substring(1, expressions[j].Text.Length - 2).Trim();
                if (string.Compare(section, "object", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    section = "Structure";
                }
                else if (string.Compare(section, "railway", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    section = "Track";
                }
                sectionAlwaysPrefix = true;
            }
            else
            {
                expressions[j].Text = Expression.ConvertRwToCsv(expressions[j].Text, section, sectionAlwaysPrefix);

                // separate command and arguments
                string command, argumentSequence;
                Expression.SeparateCommandsAndArguments(expressions[j], out command, out argumentSequence, culture, true, isRW, section);


                // process command
                float number;
                bool numberCheck = !isRW || string.Compare(section, "track", StringComparison.OrdinalIgnoreCase) == 0;
                if (!numberCheck || !Conversions.TryParseFloatVb6(command, unitOfLength, out number))
                {
                    // split arguments
                    string[] arguments;
                    {
                        int n = 0;
                        for (int k = 0; k < argumentSequence.Length; k++)
                        {
                            if (isRW && argumentSequence[k] == ',')
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
                            if (isRW && argumentSequence[k] == ',')
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
                        Array.Resize(ref arguments, h);
                    }
                    // preprocess command
                    if (command.ToLowerInvariant() == "with")
                    {
                        if (arguments.Length >= 1)
                        {
                            section = arguments[0];
                            sectionAlwaysPrefix = false;
                        }
                        else
                        {
                            section = "";
                            sectionAlwaysPrefix = false;
                        }
                        command = null;
                    }
                    else
                    {
                        if (command.StartsWith("."))
                        {
                            command = section + command;
                        }
                        else if (sectionAlwaysPrefix)
                        {
                            command = section + "." + command;
                        }
                        command = command.Replace(".Void", "");
                    }
                    // handle indices
                    if (command != null && command.EndsWith(")"))
                    {
                        for (int k = command.Length - 2; k >= 0; k--)
                        {
                            if (command[k] == '(')
                            {
                                string Indices = command.Substring(k + 1, command.Length - k - 2).TrimStart();
                                command = command.Substring(0, k).TrimEnd();
                                int h = Indices.IndexOf(";", StringComparison.Ordinal);
                                int CommandIndex1;
                                if (h >= 0)
                                {
                                    string a = Indices.Substring(0, h).TrimEnd();
                                    string b = Indices.Substring(h + 1).TrimStart();
                                    if (a.Length > 0 && !Conversions.TryParseIntVb6(a, out CommandIndex1))
                                    {
                                        command = null; break;
                                    }
                                    int CommandIndex2;
                                    if (b.Length > 0 && !Conversions.TryParseIntVb6(b, out CommandIndex2))
                                    {
                                        command = null;
                                    }
                                }
                                else
                                {
                                    if (Indices.Length > 0 && !Conversions.TryParseIntVb6(Indices, out CommandIndex1))
                                    {
                                        command = null;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    // process command
                    if (command != null)
                    {
                        switch (command.ToLowerInvariant())
                        {
                            // options
                            case "options.unitoflength":
                                {
                                    if (arguments.Length == 0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "At least 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    }
                                    else
                                    {
                                        unitOfLength = new float[arguments.Length];
                                        for (int i = 0; i < arguments.Length; i++)
                                        {
                                            unitOfLength[i] = i == arguments.Length - 1 ? 1.0f : 0.0f;
                                            if (arguments[i].Length > 0 && !Conversions.TryParseFloatVb6(arguments[i], out unitOfLength[i]))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FactorInMeters" + i.ToString(culture) + " is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                                unitOfLength[i] = i == 0 ? 1.0f : 0.0f;
                                            }
                                            else if (unitOfLength[i] <= 0.0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FactorInMeters" + i.ToString(culture) + " is expected to be positive in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                                unitOfLength[i] = i == arguments.Length - 1 ? 1.0f : 0.0f;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "options.unitofspeed":
                                {
                                    if (arguments.Length < 1)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    }
                                    else
                                    {
                                        if (arguments.Length > 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        }
                                        if (arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out routeData.UnitOfSpeed))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FactorInKmph is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            routeData.UnitOfSpeed = 0.277777777777778f;
                                        }
                                        else if (routeData.UnitOfSpeed <= 0.0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FactorInKmph is expected to be positive in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            routeData.UnitOfSpeed = 0.277777777777778f;
                                        }
                                        else
                                        {
                                            routeData.UnitOfSpeed *= 0.277777777777778f;
                                        }
                                    }
                                }
                                break;
                            case "options.objectvisibility":
                                {
                                    if (arguments.Length == 0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    }
                                    else
                                    {
                                        if (arguments.Length > 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        }
                                        int mode = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length != 0 && !Conversions.TryParseIntVb6(arguments[0], out mode))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            mode = 0;
                                        }
                                        else if (mode != 0 & mode != 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The specified Mode is not supported in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            mode = 0;
                                        }
                                        routeData.AccurateObjectDisposal = mode == 1;
                                    }
                                }
                                break;
                            case "options.compatibletransparencymode":
                                {
                                    //Whether to use fuzzy matching for BVE2 / BVE4 transparencies
                                    //Should be DISABLED on openBVE content
                                    if (previewOnly)
                                    {
                                        continue;
                                    }
                                    if (arguments.Length == 0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    }
                                    else
                                    {
                                        if (arguments.Length > 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        }
                                        int mode = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length != 0 && !Conversions.TryParseIntVb6(arguments[0], out mode))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            mode = 0;
                                        }
                                        else if (mode != 0 & mode != 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The specified Mode is not supported in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            mode = 0;
                                        }
                                        //Plugin.CurrentOptions.OldTransparencyMode = mode == 1;        // TODO
                                    }
                                }
                                break;
                            case "options.enablebvetshacks":
                            case "options.enablehacks":
                                {
                                    //Whether to apply various hacks to fix BVE2 / BVE4 routes
                                    //Whilst this is harmless, it should be DISABLED on openBVE content
                                    //in order to ensure that all errors are correctly fixed by the developer
                                    if (previewOnly)
                                    {
                                        continue;
                                    }
                                    if (arguments.Length == 0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    }
                                    else
                                    {
                                        if (arguments.Length > 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "Exactly 1 argument is expected in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        }
                                        int mode = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length != 0 && !Conversions.TryParseIntVb6(arguments[0], out mode))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            mode = 0;
                                        }
                                        else if (mode != 0 & mode != 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The specified Mode is not supported in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            mode = 0;
                                        }
                                        // TODO
                                        // Plugin.CurrentOptions.EnableBveTsHacks = mode == 1; 
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Preprocess chr / rnd / sub statements
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="fileEncoding"></param>
    /// <param name="isRW"></param>
    /// <param name="expressions"></param>
    private static void PreprocessChrRndSub(string fileName, Encoding fileEncoding, bool isRW, ref Expression[] expressions)
    {
        System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;
        System.Text.Encoding Encoding = new System.Text.ASCIIEncoding();
        string[] subs = new string[16];
        int openIfs = 0;
        for (int i = 0; i < expressions.Length; i++)
        {
            string epilog = " at line " + expressions[i].Line.ToString(Culture) + ", column " + expressions[i].Column.ToString(Culture) + " in file " + expressions[i].File;
            bool continueWithNextExpression = false;
            for (int j = expressions[i].Text.Length - 1; j >= 0; j--)
            {
                if (expressions[i].Text[j] == '$')
                {
                    int k;
                    for (k = j + 1; k < expressions[i].Text.Length; k++)
                    {
                        if (expressions[i].Text[k] == '(')
                        {
                            break;
                        }
                        else if (expressions[i].Text[k] == '/' | expressions[i].Text[k] == '\\')
                        {
                            k = expressions[i].Text.Length + 1;
                            break;
                        }
                    }
                    if (k <= expressions[i].Text.Length)
                    {
                        string t = expressions[i].Text.Substring(j, k - j).TrimEnd();
                        int l = 1, h;
                        for (h = k + 1; h < expressions[i].Text.Length; h++)
                        {
                            switch (expressions[i].Text[h])
                            {
                                case '(':
                                    l++;
                                    break;
                                case ')':
                                    l--;
                                    if (l < 0)
                                    {
                                        continueWithNextExpression = true;
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid parenthesis structure in " + t + epilog);
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
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid parenthesis structure in " + t + epilog);
                            continueWithNextExpression = true;
                            break;
                        }
                        string s = expressions[i].Text.Substring(k + 1, h - k - 1).Trim();
                        switch (t.ToLowerInvariant())
                        {
                            case "$if":
                                if (j != 0)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The $If directive must not appear within another statement" + epilog);
                                }
                                else
                                {
                                    double num;
                                    if (double.TryParse(s, System.Globalization.NumberStyles.Float, Culture, out num))
                                    {
                                        openIfs++;
                                        expressions[i].Text = string.Empty;
                                        if (num == 0.0)
                                        {
                                            // Blank every expression until the matching $Else or $EndIf
                                            i++;
                                            int level = 1;
                                            while (i < expressions.Length)
                                            {
                                                if (expressions[i].Text.StartsWith("$if", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    expressions[i].Text = string.Empty;
                                                    level++;
                                                }
                                                else if (expressions[i].Text.StartsWith("$else", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    expressions[i].Text = string.Empty;
                                                    if (level == 1)
                                                    {
                                                        level--;
                                                        break;
                                                    }
                                                }
                                                else if (expressions[i].Text.StartsWith("$endif", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    expressions[i].Text = string.Empty;
                                                    level--;
                                                    if (level == 0)
                                                    {
                                                        openIfs--;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    expressions[i].Text = string.Empty;
                                                }
                                                i++;
                                            }
                                            if (level != 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "$EndIf missing at the end of the file" + epilog);
                                            }
                                        }
                                        continueWithNextExpression = true;
                                        break;
                                    }
                                    else
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The $If condition does not evaluate to a number" + epilog);
                                    }
                                }
                                continueWithNextExpression = true;
                                break;
                            case "$else":

                                // Blank every expression until the matching $EndIf

                                expressions[i].Text = string.Empty;
                                if (openIfs != 0)
                                {
                                    i++;
                                    int level = 1;
                                    while (i < expressions.Length)
                                    {
                                        if (expressions[i].Text.StartsWith("$if", StringComparison.OrdinalIgnoreCase))
                                        {
                                            expressions[i].Text = string.Empty;
                                            level++;
                                        }
                                        else if (expressions[i].Text.StartsWith("$else", StringComparison.OrdinalIgnoreCase))
                                        {
                                            expressions[i].Text = string.Empty;
                                            if (level == 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Duplicate $Else encountered" + epilog);
                                            }
                                        }
                                        else if (expressions[i].Text.StartsWith("$endif", StringComparison.OrdinalIgnoreCase))
                                        {
                                            expressions[i].Text = string.Empty;
                                            level--;
                                            if (level == 0)
                                            {
                                                openIfs--;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            expressions[i].Text = string.Empty;
                                        }
                                        i++;
                                    }
                                    if (level != 0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "$EndIf missing at the end of the file" + epilog);
                                    }
                                }
                                else
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "$Else without matching $If encountered" + epilog);
                                }
                                continueWithNextExpression = true;
                                break;
                            case "$endif":
                                expressions[i].Text = string.Empty;
                                if (openIfs != 0)
                                {
                                    openIfs--;
                                }
                                else
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "$EndIf without matching $If encountered" + epilog);
                                }
                                continueWithNextExpression = true;
                                break;
                            case "$include":
                                if (j != 0)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The $Include directive must not appear within another statement" + epilog);
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
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The track position offset " + value + " is invalid in " + t + epilog);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The file " + includeFile + " could not be found in " + t + epilog);
                                            break;
                                        }
                                        else if (2 * ia + 1 < args.Length)
                                        {
                                            if (!Conversions.TryParseDoubleVb6(args[2 * ia + 1], out weights[ia]))
                                            {
                                                continueWithNextExpression = true;
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "A weight is invalid in " + t + epilog);
                                                break;
                                            }
                                            else if (weights[ia] <= 0.0)
                                            {
                                                continueWithNextExpression = true;
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "A weight is not positive in " + t + epilog);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "No file was specified in " + t + epilog);
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


                                        Expression[] expr = Expression.PreprocessSplitIntoExpressions(files[chosenIndex], fileEncoding, isRW, lines, false, offsets[chosenIndex] + expressions[i].TrackPositionOffset).ToArray();

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
                                            expressions[i].Text = expressions[i].Text.Substring(0, j) + new string(Encoding.GetChars(new byte[] { (byte)x })) + expressions[i].Text.Substring(h + 1);
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index does not correspond to a valid ASCII character in " + t + epilog);
                                        }
                                    }
                                    else
                                    {
                                        continueWithNextExpression = true;
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index is invalid in " + t + epilog);
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
                                                expressions[i].Text = expressions[i].Text.Substring(0, j) + z.ToString(Culture) + expressions[i].Text.Substring(h + 1);
                                            }
                                            else
                                            {
                                                continueWithNextExpression = true;
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index2 is invalid in " + t + epilog);
                                            }
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index1 is invalid in " + t + epilog);
                                        }
                                    }
                                    else
                                    {
                                        continueWithNextExpression = true;
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Two arguments are expected in " + t + epilog);
                                    }
                                }
                                break;
                            case "$sub":
                                {
                                    l = 0;
                                    bool f = false;
                                    int m;
                                    for (m = h + 1; m < expressions[i].Text.Length; m++)
                                    {
                                        switch (expressions[i].Text[m])
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
                                                if (!char.IsWhiteSpace(expressions[i].Text[m])) l = -1;
                                                break;
                                        }
                                        if (f | l < 0) break;
                                    }
                                    if (f)
                                    {
                                        l = 0;
                                        int n;
                                        for (n = m + 1; n < expressions[i].Text.Length; n++)
                                        {
                                            switch (expressions[i].Text[n])
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
                                                while (x >= subs.Length)
                                                {
                                                    Array.Resize<string>(ref subs, subs.Length << 1);
                                                }
                                                subs[x] = expressions[i].Text.Substring(m + 1, n - m - 1).Trim();
                                                expressions[i].Text = expressions[i].Text.Substring(0, j) + expressions[i].Text.Substring(n);
                                            }
                                            else
                                            {
                                                continueWithNextExpression = true;
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index is expected to be non-negative in " + t + epilog);
                                            }
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index is invalid in " + t + epilog);
                                        }
                                    }
                                    else
                                    {
                                        int x;
                                        if (Conversions.TryParseIntVb6(s, out x))
                                        {
                                            if (x >= 0 & x < subs.Length && subs[x] != null)
                                            {
                                                expressions[i].Text = expressions[i].Text.Substring(0, j) + subs[x] + expressions[i].Text.Substring(h + 1);
                                            }
                                            else
                                            {
                                                continueWithNextExpression = true;
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index is out of range in " + t + epilog);
                                            }
                                        }
                                        else
                                        {
                                            continueWithNextExpression = true;
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Index is invalid in " + t + epilog);
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
                expressions[i].Text = expressions[i].Text.Trim();
                if (expressions[i].Text.Length != 0)
                {
                    if (expressions[i].Text[0] == ';')
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

    /// <summary>
    /// Preprocessing step - sort expressions by track position
    /// </summary>
    /// <param name="unitFactors"></param>
    /// <param name="isRW"></param>
    /// <param name="expressions"></param>
    private static void PreprocessSortByTrackPosition(float[] unitFactors, bool isRW, ref Expression[] expressions)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;

        PositionedExpression[] p = new PositionedExpression[expressions.Length];
        int n = 0;
        double a = -1.0;
        bool numberCheck = !isRW;
        for (int i = 0; i < expressions.Length; i++)
        {
            if (isRW)
            {
                // only check for track positions in the railway section for RW routes
                if (expressions[i].Text.StartsWith("[", StringComparison.Ordinal) && expressions[i].Text.EndsWith("]", StringComparison.Ordinal))
                {
                    string s = expressions[i].Text.Substring(1, expressions[i].Text.Length - 2).Trim();
                    numberCheck = string.Compare(s, "Railway", StringComparison.OrdinalIgnoreCase) == 0;
                }
            }
            float x;
            if (numberCheck && Conversions.TryParseFloat(expressions[i].Text, unitFactors, out x))
            {
                x += (float)expressions[i].TrackPositionOffset;
                if (x >= 0.0)
                {
                    // TODO: BveTsHacks
                    // if (Plugin.CurrentOptions.EnableBveTsHacks)
                    // {
                    // 	switch (System.IO.Path.GetFileName(Expressions[i].File.ToLowerInvariant()))
                    // 	{
                    // 		case "balloch - dumbarton central special nighttime run.csv":
                    // 		case "balloch - dumbarton central summer 2004 morning run.csv":
                    // 			if (x != 0 || a != 4125)
                    // 			{
                    // 				//Misplaced comma in the middle of the line causes this to be interpreted as a track position
                    // 				a = x;
                    // 			}
                    // 			break;
                    // 		default:
                    // 			a = x;
                    // 			break;
                    // 	}
                    // }
                    // else
                    // {
                    a = x;
                    // }

                }
                else
                {
                    // TODO: GD.Print can be refactored back into Plugin.CurrentHost.AddMessage
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Negative track position encountered at line " + expressions[i].Line.ToString(culture) + ", column " + expressions[i].Column.ToString(culture) + " in file " + expressions[i].File);
                    //Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Negative track position encountered at line " + Expressions[i].Line.ToString(Culture) + ", column " + Expressions[i].Column.ToString(Culture) + " in file " + Expressions[i].File);
                }
            }
            else
            {
                p[n].TrackPosition = a;
                p[n].Expression = expressions[i];
                int j = n;
                n++;
                while (j > 0)
                {
                    if (p[j].TrackPosition < p[j - 1].TrackPosition)
                    {
                        PositionedExpression t = p[j];
                        p[j] = p[j - 1];
                        p[j - 1] = t;
                        j--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        a = -1.0;
        Expression[] e = new Expression[expressions.Length];
        int m = 0;
        for (int i = 0; i < n; i++)
        {
            if (p[i].TrackPosition != a)
            {
                a = p[i].TrackPosition;
                e[m] = new Expression(string.Empty, (a / unitFactors[unitFactors.Length - 1]).ToString(culture), -1, -1, -1);
                m++;
            }
            e[m] = p[i].Expression;
            m++;
        }
        Array.Resize(ref e, m);
        expressions = e;
    }

    #endregion

    #region Parsing (CsvRwRouteParser.cs)

    /// <summary>
    /// Parse route
    /// </summary>
    /// <param name="root"></param>
    /// <param name="fileName"></param>
    /// <param name="fileEncoding"></param>
    /// <param name="isRW"></param>
    /// <param name="trainPath"></param>
    /// <param name="objectPath"></param>
    /// <param name="soundPath"></param>
    /// <param name="previewOnly"></param>
    internal static void ParseRoute(Node root, string fileName, Encoding fileEncoding, bool isRW, string trainPath, string objectPath, string soundPath, bool previewOnly)
    {
        // initialize data
        string compatibilityFolder = System.IO.Path.Combine(objectPath, "Compatibility");

        RouteData routeData = new RouteData
        {
            BlockInterval = 25.0f,
            AccurateObjectDisposal = false,
            FirstUsedBlock = -1,
            Blocks = new List<Block>()
        };
        routeData.Blocks.Add(new Block(previewOnly));
        routeData.Blocks[0].Rails.Add(0, new Rail { RailStarted = true });
        routeData.Blocks[0].RailType = new[] { 0 };
        routeData.Blocks[0].Accuracy = 2.0f;
        routeData.Blocks[0].AdhesionMultiplier = 1.0f;
        routeData.Blocks[0].CurrentTrackState = new TrackElement(0.0f);

        if (!previewOnly)
        {
            routeData.Blocks[0].Background = 0;
            // routeData.Blocks[0].Fog = new Fog(CurrentRoute.NoFogStart, CurrentRoute.NoFogEnd, Color24.Grey, 0);
            routeData.Blocks[0].GroundCycles = new[] { -1 };
            routeData.Blocks[0].RailCycles = new RailCycle[1];
            routeData.Blocks[0].RailCycles[0].RailCycleIndex = -1;
            routeData.Blocks[0].Height = isRW ? 0.3f : 0.0f;
            routeData.Blocks[0].RailFreeObj = new Dictionary<int, List<FreeObj>>();
            routeData.Blocks[0].GroundFreeObj = new List<FreeObj>();
            routeData.Blocks[0].RailWall = new Dictionary<int, WallDike>();
            routeData.Blocks[0].RailDike = new Dictionary<int, WallDike>();
            routeData.Blocks[0].RailPole = new Pole[] { };
            routeData.Markers = new Marker[] { };
            routeData.RequestStops = new StopRequest[] { };
            string poleFolder = System.IO.Path.Combine(compatibilityFolder, "Poles");
            routeData.Structure.Poles = new PoleDictionary
                {
                    {0, new ObjectDictionary()},
                    {1, new ObjectDictionary()},
                    {2, new ObjectDictionary()},
                    {3, new ObjectDictionary()}
                };
            routeData.Structure.Poles[0].Add(0, UnifiedObject.LoadStaticObject(root,System.IO.Path.Combine(poleFolder, "pole_1.csv"), System.Text.Encoding.UTF8, false, false, false));
            routeData.Structure.Poles[1].Add(0, UnifiedObject.LoadStaticObject(root,System.IO.Path.Combine(poleFolder, "pole_2.csv"), System.Text.Encoding.UTF8, false, false, false));
            routeData.Structure.Poles[2].Add(0, UnifiedObject.LoadStaticObject(root,System.IO.Path.Combine(poleFolder, "pole_3.csv"), System.Text.Encoding.UTF8, false, false, false));
            routeData.Structure.Poles[3].Add(0, UnifiedObject.LoadStaticObject(root,System.IO.Path.Combine(poleFolder, "pole_4.csv"), System.Text.Encoding.UTF8, false, false, false));

            routeData.Structure.RailObjects = new ObjectDictionary();
            routeData.Structure.Ground = new ObjectDictionary();
            routeData.Structure.WallL = new ObjectDictionary();
            routeData.Structure.WallR = new ObjectDictionary();
            routeData.Structure.DikeL = new ObjectDictionary();
            routeData.Structure.DikeR = new ObjectDictionary();
            routeData.Structure.FormL = new ObjectDictionary();
            routeData.Structure.FormR = new ObjectDictionary();
            routeData.Structure.FormCL = new ObjectDictionary();
            routeData.Structure.FormCR = new ObjectDictionary();
            routeData.Structure.RoofL = new ObjectDictionary();
            routeData.Structure.RoofR = new ObjectDictionary();
            routeData.Structure.RoofCL = new ObjectDictionary();
            routeData.Structure.RoofCR = new ObjectDictionary();
            routeData.Structure.CrackL = new ObjectDictionary();
            routeData.Structure.CrackR = new ObjectDictionary();
            routeData.Structure.FreeObjects = new ObjectDictionary();
            routeData.Structure.Beacon = new ObjectDictionary();
            routeData.Structure.GroundCycles = new int[][] { };
            routeData.Structure.RailCycles = new int[][] { };
            routeData.Structure.Run = new int[] { };
            routeData.Structure.Flange = new int[] { };
            // routeData.Backgrounds = new BackgroundDictionary();
            routeData.TimetableDaytime = new ImageTexture[] { null, null, null, null };
            routeData.TimetableNighttime = new ImageTexture[] { null, null, null, null };
            routeData.Structure.WeatherObjects = new ObjectDictionary();

            Node signalParent = new Node();
            signalParent.Name = "Signals";

            // TODO alternate method compatibility signal
            // signals
            string signalFolder = System.IO.Path.Combine(compatibilityFolder, @"Signals\Japanese"); //TODO path
            // routeData.SignalData = new SignalData[7];
            // routeData.SignalData[3] = new CompatibilitySignalData(new int[] { 0, 2, 4 }, new UnifiedObject[] {
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_3_0.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_3_2.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_3_4.csv"), fileEncoding, false, false, false)
            //                                                      });
            // routeData.SignalData[4] = new CompatibilitySignalData(new int[] { 0, 1, 2, 4 }, new UnifiedObject[] {
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4_0.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4a_2.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4a_1.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine (signalFolder, "signal_4a_4.csv"), fileEncoding, false, false, false)
            //                                                      });
            // routeData.SignalData[5] = new CompatibilitySignalData(new int[] { 0, 1, 2, 3, 4 }, new UnifiedObject[] {
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_0.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5a_1.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_2.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_3.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_4.csv"), fileEncoding, false, false, false)
            //                                                      });
            // routeData.SignalData[6] = new CompatibilitySignalData(new int[] { 0, 3, 4 }, new UnifiedObject[] {
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_0.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_3.csv"), fileEncoding, false, false, false),
            //                                                          UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_4.csv"), fileEncoding, false, false, false)
            //                                                      });
            // compatibility signals
            routeData.CompatibilitySignals = new CompatibilitySignalData[9];
            routeData.CompatibilitySignals[0] = new CompatibilitySignalData(new int[] { 0, 2 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2a_2.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[1] = new CompatibilitySignalData(new int[] { 0, 4 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_2b_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[2] = new CompatibilitySignalData(new int[] { 0, 2, 4 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_3_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_3_2.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_3_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[3] = new CompatibilitySignalData(new int[] { 0, 1, 2, 4 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4a_1.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4a_2.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4a_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[4] = new CompatibilitySignalData(new int[] { 0, 2, 3, 4 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4b_2.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4b_3.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_4b_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[5] = new CompatibilitySignalData(new int[] { 0, 1, 2, 3, 4 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5a_1.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_2.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_3.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_4.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[6] = new CompatibilitySignalData(new int[] { 0, 2, 3, 4, 5 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_2.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_3.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5_4.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_5b_5.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[7] = new CompatibilitySignalData(new int[] { 0, 1, 2, 3, 4, 5 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_1.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_2.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_3.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_4.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "signal_6_5.csv"), fileEncoding, false, false, false)
                                                                              });
            routeData.CompatibilitySignals[8] = new CompatibilitySignalData(new int[] { 0, 3, 4 }, new UnifiedObject[] {
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_0.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_3.csv"), fileEncoding, false, false, false),
                                                                                  UnifiedObject.LoadStaticObject(signalParent, System.IO.Path.Combine(signalFolder, "repeatingsignal_4.csv"), fileEncoding, false, false, false)
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
        float[] unitOfLength = new float[] { 1.0f };
        routeData.UnitOfSpeed = 0.277777777777778f;

        string[] lines = System.IO.File.ReadAllLines(fileName, fileEncoding);
        Expression[] expressionArray = Expression.PreprocessSplitIntoExpressions(fileName, fileEncoding, isRW, lines, true, 0.0).ToArray();

        PreprocessChrRndSub(fileName, fileEncoding, isRW, ref expressionArray);
        PreprocessOptions(expressionArray, isRW, ref routeData, ref unitOfLength, previewOnly);
        PreprocessSortByTrackPosition(unitOfLength, isRW, ref expressionArray);
        ParseRouteDetails(fileName, fileEncoding, isRW, expressionArray, trainPath, objectPath, soundPath, unitOfLength, ref routeData, previewOnly);
        // >> End OpenBVE ParseRouteForData()

        //Game.RouteUnitOfLength = UnitOfLength;
        // CurrentRoute currRoute = new CurrentRoute();
        CurrentRoute.ApplyRouteData(root, fileName, compatibilityFolder, fileEncoding, ref routeData, previewOnly);
    }

    private static void ParseRouteDetails(string fileName, Encoding fileEncoding, bool isRW, Expression[] routeExpressions, string trainPath, string objectPath, string soundPath, float[] unitOfLength, ref RouteData routeData, bool previewOnly)
    {
        System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
        string section = ""; 
        bool sectionAlwaysPrefix = false;
        int blockIndex = 0;
        int blocksUsed = routeData.Blocks.Count;
        
        //Game.Stations = new Game.Station[] { };
        int currentStation = -1;
        int currentStop = -1;
        bool departureSignalUsed = false;
        int currentSection = 0;
        bool valueBasedSection = false;
        double progressFactor = routeExpressions.Length == 0 ? 0.3333 : 0.3333 / (double)routeExpressions.Length;

        Node rootRoutePrefabs = new Node();
        rootRoutePrefabs.Name = "Route (Prefabs)";

        // process non-track namespaces
        for (int j = 0; j < routeExpressions.Length; j++)
        {
            //Loading.RouteProgress = (double)j * progressFactor;
            if ((j & 255) == 0)
            {
                //System.Threading.Thread.Sleep(1);
                //if (Loading.Cancel) return;
            }

            if (routeExpressions[j].Text.StartsWith("[") & routeExpressions[j].Text.EndsWith("]"))
            {
                section = routeExpressions[j].Text.Substring(1, routeExpressions[j].Text.Length - 2).Trim();
                if (string.Compare(section, "object", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    section = "Structure";
                }
                else if (string.Compare(section, "railway", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    section = "Track";
                }
                sectionAlwaysPrefix = true;
            }
            else
            {
                // find equals
                int equals = routeExpressions[j].Text.IndexOf('=');
                if (equals >= 0)
                {
                    // handle RW cycle syntax
                    string t = routeExpressions[j].Text.Substring(0, equals);
                    if (section.ToLowerInvariant() == "cycle" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Ground(" + t + ")";
                        }
                    }
                    else if (section.ToLowerInvariant() == "signal" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Void(" + t + ")";
                        }
                    }
                    // convert RW style into CSV style
                    routeExpressions[j].Text = t + " " + routeExpressions[j].Text.Substring(equals + 1);
                }
                // separate command and arguments
                string command, argumentSequence;
                Expression.SeparateCommandsAndArguments(routeExpressions[j], out command, out argumentSequence, culture, true, isRW, section);

                // process command
                float number;
                bool numberCheck = !isRW || string.Compare(section, "track", StringComparison.OrdinalIgnoreCase) == 0;
                if (numberCheck && Conversions.TryParseFloat(command, unitOfLength, out number))
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
                            section = arguments[0];
                            sectionAlwaysPrefix = false;
                        }
                        else
                        {
                            section = "";
                            sectionAlwaysPrefix = false;
                        }
                        command = null;
                    }
                    else
                    {
                        if (command.StartsWith("."))
                        {
                            command = section + command;
                        }
                        else if (sectionAlwaysPrefix)
                        {
                            command = section + "." + command;
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid first index appeared at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File + ".");
                                        command = null; break;
                                    }
                                    else if (b.Length > 0 && !Conversions.TryParseIntVb6(b, out commandIndex2))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid second index appeared at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File + ".");
                                        command = null; break;
                                    }
                                }
                                else
                                {
                                    if (Indices.Length > 0 && !Conversions.TryParseIntVb6(Indices, out commandIndex1))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid index appeared at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File + ".");
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
                                    float length = 25.0f;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], unitOfLength, out length))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Length is invalid in Options.BlockLength at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        length = 25.0f;
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    int a;
                                    if (!Conversions.TryParseIntVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a != 0 & a != 1)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is expected to be either 0 or 1 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    int a;
                                    if (!Conversions.TryParseIntVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a != 0 & a != 1)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is expected to be either 0 or 1 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    int a;
                                    if (!Conversions.TryParseIntVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a != 0 & a != 1)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is expected to be either 0 or 1 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    //Game.RouteComment = arguments[0];
                                }
                                break;
                            case "route.image":
                                if (arguments.Length < 1)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    string f = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fileName), arguments[0]);
                                    if (!System.IO.File.Exists(f))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "" + command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        change = 0;
                                    }
                                    else if (change < -1 | change > 1)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Mode is expected to be -1, 0 or 1 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        change = 0;
                                    }
                                    //Game.TrainStart = (Game.TrainStartMode)change;
                                }
                                break;
                            case "route.gauge":
                            case "train.gauge":
                                if (arguments.Length < 1)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInMillimeters is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a <= 0.0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInMillimeters is expected to be positive in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else
                                    {
                                        double a;
                                        if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (commandIndex1 < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "AspectIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (a < 0.0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Interval" + k.ToString(culture) + " is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is invalid in Train.Velocity at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            limit = 0.0;
                                        }
                                        //Game.PrecedingTrainSpeedLimit = limit <= 0.0 ? double.PositiveInfinity : data.UnitOfSpeed * limit;
                                    }
                                }
                                break;
                            case "route.accelerationduetogravity":
                                if (arguments.Length < 1)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a <= 0.0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is expected to be positive in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    float a;
                                    if (!Conversions.TryParseFloatVb6(arguments[0], unitOfLength, out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Height is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInCelsius is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a <= -273.15)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInCelsius is expected to be greater than to -273.15 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    double a;
                                    if (!Conversions.TryParseDoubleVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInKPa is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (a <= 0.0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInKPa is expected to be positive in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RedValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (r < 0 | r > 255)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RedValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        r = r < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out g))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GreenValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (g < 0 | g > 255)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GreenValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        g = g < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out b))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BlueValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (b < 0 | b > 255)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BlueValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RedValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (r < 0 | r > 255)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RedValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        r = r < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out g))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GreenValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (g < 0 | g > 255)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GreenValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        g = g < 0 ? 0 : 255;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out b))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BlueValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else if (b < 0 | b > 255)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BlueValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Theta is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out phi))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Phi is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FolderName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailTypeIndex is out of range in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            int val = 0;
                                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out val))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RunSoundIndex is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                val = 0;
                                            }
                                            if (val < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RunSoundIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailTypeIndex is out of range in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            int val = 0;
                                            if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out val))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FlangeSoundIndex is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                val = 0;
                                            }
                                            if (val < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FlangeSoundIndex expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "TimetableIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "TimetableIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (!previewOnly)
                                                {
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);

                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                        // missingObjectCount++;
                                                    }
                                                    else
                                                    {
                                                        if (!previewOnly)
                                                        {
                                                            UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                            if (obj != null)
                                                            {
                                                                routeData.Structure.RailObjects.Add(commandIndex1, obj, "RailStructure");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // railtypeCount++;
                                                        }
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BeaconStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.Beacon.Add(commandIndex1, obj, "BeaconStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "AdditionalRailsCovered is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (commandIndex2 < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PoleStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (!routeData.Structure.Poles.ContainsKey(commandIndex1))
                                                {
                                                    routeData.Structure.Poles.Add(commandIndex1, new ObjectDictionary());
                                                }

                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    bool overwriteDefault = commandIndex2 >= 0 && commandIndex2 >= 3;
                                                    routeData.Structure.Poles[commandIndex1].Add(commandIndex2, obj, overwriteDefault);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GroundStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.Ground.Add(commandIndex1, obj, "GroundStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.WallL.Add(commandIndex1, obj, "Left WallStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.WallR.Add(commandIndex1, obj, "Right WallStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.DikeL.Add(commandIndex1, obj, "Left DikeStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.DikeR.Add(commandIndex1, obj, "Right DikeStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.FormL.Add(commandIndex1, obj, "Left FormStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.FormR.Add(commandIndex1, obj, "Right FormStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.FormCL.Add(commandIndex1, obj, "Left FormCStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.FormCR.Add(commandIndex1, obj, "Right FormCStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    }
                                                    else
                                                    {
                                                        UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                        if (obj != null)
                                                        {
                                                            routeData.Structure.RoofL.Add(commandIndex1, obj, "Left RoofStructure");
                                                        }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    }
                                                    else
                                                    {
                                                        UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                        if (obj != null)
                                                        {
                                                            routeData.Structure.RoofR.Add(commandIndex1, obj, "Right RoofStructure");
                                                        }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    }
                                                    else
                                                    {
                                                        UnifiedObject obj = UnifiedObject.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                        if (obj != null)
                                                        {
                                                            routeData.Structure.RoofCL.Add(commandIndex1, obj, "Left RoofCStructure");
                                                        }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (commandIndex1 == 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex was omitted or is 0 in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    commandIndex1 = 1;
                                                }
                                                if (commandIndex1 < 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is expected to be non-negative in " + command + " argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                    if (!System.IO.File.Exists(f))
                                                    {
                                                        Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    }
                                                    else
                                                    {
                                                        UnifiedObject obj = UnifiedObject.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                        if (obj != null)
                                                        {
                                                            routeData.Structure.RoofCR.Add(commandIndex1, obj, "Right RoofCStructure");
                                                        }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CrackStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                if (!System.IO.File.Exists(f))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, true, "FileName " + f + " not found in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                else
                                                {
                                                    UnifiedObject obj = UnifiedObject.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                    if (obj != null)
                                                    {
                                                        routeData.Structure.CrackL.Add(commandIndex1, obj, "Left CrackStructure");
                                                    }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CrackStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                UnifiedObject obj = UnifiedObject.LoadStaticObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                if (obj != null)
                                                {
                                                    routeData.Structure.CrackR.Add(commandIndex1, obj, "Right CrackStructure");
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FreeObjStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (arguments.Length < 1)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                                UnifiedObject obj = UnifiedObject.LoadObject(rootRoutePrefabs, f, fileEncoding, false, false, false);
                                                if (obj != null)
                                                {
                                                    routeData.Structure.FreeObjects.Add(commandIndex1, obj, "FreeObject");
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            // signal
                            case "signal":
                                {
                                    // TODO
                                }
                                break;

                            // texture
                            case "texture.background":
                            case "structure.back":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     if (commandIndex1 < 0)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex is expected to be non-negative at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else if (arguments.Length < 1)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else
                                    //     {
                                    //         if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //         }
                                    //         else
                                    //         {
                                    //             if (commandIndex1 >= routeData.Backgrounds.Length)
                                    //             {
                                    //                 int a = routeData.Backgrounds.Length;
                                    //                 Array.Resize<ImageTexture>(ref routeData.Backgrounds, commandIndex1 + 1);
                                    //                 for (int k = a; k <= commandIndex1; k++)
                                    //                 {
                                    //                     routeData.Backgrounds[k] = new ImageTexture();
                                    //                 }
                                    //             }
                                    //             string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                    //             if (!System.IO.File.Exists(f))
                                    //             {
                                    //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName" + f + " not found in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             }
                                    //             else
                                    //             {
                                    //                 // TODO: Only BMP support...

                                    //                 ImageTexture background = new ImageTexture();
                                    //                 background.Load(f);
                                    //                 if (background != null)
                                    //                     routeData.Backgrounds[commandIndex1] = background;

                                    //                 //routeData.Backgrounds

                                    //                 // OLD (works when the textuers are in the asset folder, and have been converted by unity editor) 
                                    //                 // routeData.Backgrounds[commandIndex1] = Resources.Load(Path.Combine("Objects/gaku/", Path.GetFileNameWithoutExtension(arguments[0]))) as Texture;
                                    //             }
                                    //         }
                                    //     }
                                    // }
                                }
                                break;
                            case "texture.background.x":
                            case "structure.back.x":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     if (commandIndex1 < 0)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex is expected to be non-negative at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else if (arguments.Length < 1)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else
                                    //     {
                                    //         if (commandIndex1 >= data.Backgrounds.Length)
                                    //         {
                                    //            int a = data.Backgrounds.Length;
                                    //            Array.Resize<World.Background>(ref data.Backgrounds, commandIndex1 + 1);
                                    //            for (int k = a; k <= commandIndex1; k++)
                                    //            {
                                    //                data.Backgrounds[k] = new World.Background(null, 6, false);
                                    //            }
                                    //         }
                                    //         int x;
                                    //         if (!Conversions.TryParseIntVb6(arguments[0], out x))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    //         }
                                    //         else if (x == 0)
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RepetitionCount is expected to be non-zero in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    //         }
                                    //         else
                                    //         {
                                    //            data.Backgrounds[commandIndex1].Repetition = x;
                                    //         }
                                    //     }
                                    // }
                                }
                                break;
                            case "texture.background.aspect":
                            case "structure.back.aspect":
                                {
                                    if (!previewOnly)
                                    {
                                        if (commandIndex1 < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex is expected to be non-negative at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (arguments.Length < 1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            // TODO: Dynamic and static backgrounds
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
                                            //     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                            //}
                                            //else if (aspect != 0 & aspect != 1)
                                            //{
                                            //     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is expected to be either 0 or 1 in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                                    if (commandIndex1 >= routeData.Structure.GroundCycles.Length)
                                    {
                                        Array.Resize(ref routeData.Structure.GroundCycles, commandIndex1 + 1);
                                    }
                                    routeData.Structure.GroundCycles[commandIndex1] = new int[arguments.Length];
                                    for (int k = 0; k < arguments.Length; k++)
                                    {
                                        int ix = 0;
                                        if (arguments[k].Length > 0 && !Conversions.TryParseIntVb6(arguments[k], out ix))
                                        {
                                            // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GroundStructureIndex " + (k + 1).ToString(culture) + " is invalid in " + command + " at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                            ix = 0;
                                        }

                                        if (ix < 0 | !routeData.Structure.Ground.ContainsKey(ix))
                                        {
                                            // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GroundStructureIndex " + (k + 1).ToString(culture) + " is out of range in " + command + " at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                            ix = 0;
                                        }

                                        routeData.Structure.GroundCycles[commandIndex1][k] = ix;
                                    }
                                }

                                break;
                        }
                    }
                }
            }
        }

        // process track namespace
        for (int j = 0; j < routeExpressions.Length; j++)
        {
            //Loading.RouteProgress = 0.3333 + (double)j * progressFactor;
            if ((j & 255) == 0)
            {
                //System.Threading.Thread.Sleep(1);
                //if (Loading.Cancel) return;
            }
            if (routeExpressions[j].Text.StartsWith("[") & routeExpressions[j].Text.EndsWith("]"))
            {
                section = routeExpressions[j].Text.Substring(1, routeExpressions[j].Text.Length - 2).Trim();
                if (string.Compare(section, "object", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    section = "Structure";
                }
                else if (string.Compare(section, "railway", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    section = "Track";
                }
                sectionAlwaysPrefix = true;
            }
            else
            {
                // find equals
                int equals = routeExpressions[j].Text.IndexOf('=');
                if (equals >= 0)
                {
                    // handle RW cycle syntax
                    string t = routeExpressions[j].Text.Substring(0, equals);
                    if (section.ToLowerInvariant() == "cycle" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Ground(" + t + ")";
                        }
                    }
                    else if (section.ToLowerInvariant() == "signal" & sectionAlwaysPrefix)
                    {
                        double b; if (Conversions.TryParseDoubleVb6(t, out b))
                        {
                            t = ".Void(" + t + ")";
                        }
                    }
                    // convert RW style into CSV style
                    routeExpressions[j].Text = t + " " + routeExpressions[j].Text.Substring(equals + 1);
                }
                // separate command and arguments
                string command, argumentSequence;
                Expression.SeparateCommandsAndArguments(routeExpressions[j], out command, out argumentSequence, culture, false, isRW, section);

                // process command
                float number;
                bool numberCheck = !isRW || string.Compare(section, "track", StringComparison.OrdinalIgnoreCase) == 0;
                if (numberCheck && Conversions.TryParseFloat(command, unitOfLength, out number))
                {
                    // track position
                    if (argumentSequence.Length != 0)
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "A track position must not contain any arguments at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                    }
                    else if (number < 0.0)
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Negative track position encountered at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                    }
                    else
                    {
                        routeData.TrackPosition = number;
                        blockIndex = (int)Math.Floor(number / routeData.BlockInterval + 0.001);
                        if (routeData.FirstUsedBlock == -1) routeData.FirstUsedBlock = blockIndex;
                        routeData.CreateMissingBlocks(blockIndex, previewOnly);
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
                            section = arguments[0];
                            sectionAlwaysPrefix = false;
                        }
                        else
                        {
                            section = "";
                            sectionAlwaysPrefix = false;
                        }
                        command = null;
                    }
                    else
                    {
                        if (command.StartsWith("."))
                        {
                            command = section + command;
                        }
                        else if (sectionAlwaysPrefix)
                        {
                            command = section + "." + command;
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
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid first index appeared at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File + ".");
                                        command = null; break;
                                    }
                                    else if (b.Length > 0 && !Conversions.TryParseIntVb6(b, out CommandIndex2))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid second index appeared at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File + ".");
                                        command = null; break;
                                    }
                                }
                                else
                                {
                                    if (Indices.Length > 0 && !Conversions.TryParseIntVb6(Indices, out CommandIndex1))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Invalid index appeared at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File + ".");
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
                                if (!previewOnly)
                                {
                                    int idx = 0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        idx = 0;
                                    }
                                    if (idx < 1)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be positive in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                    }
                                    else
                                    {
                                        if (string.Compare(command, "track.railstart", StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            if (routeData.Blocks[blockIndex].Rails.ContainsKey(idx) && routeData.Blocks[blockIndex].Rails[idx].RailStarted)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is required to reference a non-existing rail in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                        }

                                        if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx))
                                        {
                                            routeData.Blocks[blockIndex].Rails.Add(idx, new Rail());

                                            if (idx >= routeData.Blocks[blockIndex].RailCycles.Length)
                                            {
                                                int ol = routeData.Blocks[blockIndex].RailCycles.Length;
                                                Array.Resize(ref routeData.Blocks[blockIndex].RailCycles, idx + 1);
                                                for (int rc = ol; rc < routeData.Blocks[blockIndex].RailCycles.Length; rc++)
                                                {
                                                    routeData.Blocks[blockIndex].RailCycles[rc].RailCycleIndex = -1;
                                                }
                                            }

                                        }

                                        Rail currentRail = routeData.Blocks[blockIndex].Rails[idx];
                                        if (currentRail.RailStartRefreshed)
                                        {
                                            currentRail.RailEnded = true;
                                        }

                                        currentRail.RailStarted = true;
                                        currentRail.RailStartRefreshed = true;
                                        if (arguments.Length >= 2)
                                        {
                                            if (arguments[1].Length > 0)
                                            {
                                                if (!Conversions.TryParseFloatVb6(arguments[1], unitOfLength, out currentRail.RailStart.x))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    currentRail.RailStart.x = 0.0f;
                                                }
                                            }

                                            if (!currentRail.RailEnded)
                                            {
                                                currentRail.RailEnd.x = currentRail.RailStart.x;
                                            }
                                        }

                                        if (arguments.Length >= 3)
                                        {
                                            if (arguments[2].Length > 0)
                                            {
                                                if (!Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out currentRail.RailStart.y))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    currentRail.RailStart.y = 0.0f;
                                                }
                                            }

                                            if (!currentRail.RailEnded)
                                            {
                                                currentRail.RailEnd.y = currentRail.RailStart.y;
                                            }
                                        }

                                        if (routeData.Blocks[blockIndex].RailType.Length <= idx)
                                        {
                                            Array.Resize(ref routeData.Blocks[blockIndex].RailType, idx + 1);
                                        }

                                        if (arguments.Length >= 4 && arguments[3].Length != 0)
                                        {
                                            int sttype;
                                            if (!Conversions.TryParseIntVb6(arguments[3], out sttype))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                sttype = 0;
                                            }

                                            if (sttype < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (!routeData.Structure.RailObjects.ContainsKey(sttype))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex " + sttype + " references an object not loaded in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (sttype < routeData.Structure.RailCycles.Length && routeData.Structure.RailCycles[sttype] != null)
                                                {
                                                    routeData.Blocks[blockIndex].RailType[idx] = routeData.Structure.RailCycles[sttype][0];
                                                    routeData.Blocks[blockIndex].RailCycles[idx].RailCycleIndex = sttype;
                                                    routeData.Blocks[blockIndex].RailCycles[idx].CurrentCycle = 0;
                                                }
                                                else
                                                {
                                                    routeData.Blocks[blockIndex].RailType[idx] = sttype;
                                                    routeData.Blocks[blockIndex].RailCycles[idx].RailCycleIndex = -1;
                                                }
                                            }
                                        }

                                        float cant = 0.0f;
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseFloatVb6(arguments[4], out cant))
                                        {
                                            if (arguments[4] != "id 0") //RouteBuilder inserts these, harmless so let's ignore
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CantInMillimeters is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            cant = 0.0f;
                                        }
                                        else
                                        {
                                            cant *= 0.001f;
                                        }

                                        currentRail.CurveCant = cant;
                                        routeData.Blocks[blockIndex].Rails[idx] = currentRail;
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex " + idx + " is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            break;
                                        }

                                        if (idx == 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The command " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            break;
                                        }

                                        if (idx < 0 || !routeData.Blocks[blockIndex].Rails.ContainsKey(idx) || !routeData.Blocks[blockIndex].Rails[idx].RailStarted)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex " + idx + " references a non-existing rail in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            break;
                                        }

                                        if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx))
                                        {
                                            routeData.Blocks[blockIndex].Rails.Add(idx, new Rail());
                                        }

                                        Rail currentRail = routeData.Blocks[blockIndex].Rails[idx];
                                        currentRail.RailStarted = false;
                                        currentRail.RailStartRefreshed = false;
                                        currentRail.RailEnded = true;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0)
                                        {
                                            if (!Conversions.TryParseFloatVb6(arguments[1], unitOfLength, out currentRail.RailEnd.x))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                currentRail.RailEnd.x = 0.0f;
                                            }
                                        }

                                        if (arguments.Length >= 3 && arguments[2].Length > 0)
                                        {
                                            if (!Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out currentRail.RailEnd.y))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                currentRail.RailEnd.y = 0.0f;
                                            }
                                        }

                                        routeData.Blocks[blockIndex].Rails[idx] = currentRail;
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }

                                        int sttype = 0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out sttype))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex is invalid in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }

                                        if (idx < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be non-negative in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx) || !routeData.Blocks[blockIndex].Rails[idx].RailStarted)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex " + idx + " could be out of range in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (sttype < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex is expected to be non-negative in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (!routeData.Structure.RailObjects.ContainsKey(sttype))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailStructureIndex " + sttype + " references an object not loaded in " + command + " is invalid for Rail 0 at line " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (routeData.Blocks[blockIndex].RailType.Length <= idx)
                                                {
                                                    Array.Resize(ref routeData.Blocks[blockIndex].RailType, idx + 1);
                                                    int ol = routeData.Blocks[blockIndex].RailCycles.Length;
                                                    Array.Resize(ref routeData.Blocks[blockIndex].RailCycles, idx + 1);
                                                    for (int rc = ol; rc < routeData.Blocks[blockIndex].RailCycles.Length; rc++)
                                                    {
                                                        routeData.Blocks[blockIndex].RailCycles[rc].RailCycleIndex = -1;
                                                    }
                                                }

                                                if (sttype < routeData.Structure.RailCycles.Length && routeData.Structure.RailCycles[sttype] != null)
                                                {
                                                    routeData.Blocks[blockIndex].RailType[idx] = routeData.Structure.RailCycles[sttype][0];
                                                    routeData.Blocks[blockIndex].RailCycles[idx].RailCycleIndex = sttype;
                                                    routeData.Blocks[blockIndex].RailCycles[idx].CurrentCycle = 0;
                                                }
                                                else
                                                {
                                                    routeData.Blocks[blockIndex].RailType[idx] = sttype;
                                                    routeData.Blocks[blockIndex].RailCycles[idx].RailCycleIndex = -1;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.accuracy":
                                {
                                    float r = 2.0f;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out r))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        r = 2.0f;
                                    }
                                    if (r < 0.0)
                                    {
                                        r = 0.0f;
                                    }
                                    else if (r > 4.0)
                                    {
                                        r = 4.0f;
                                    }
                                    routeData.Blocks[blockIndex].Accuracy = r;
                                }
                                break;
                            case "track.pitch":
                                {
                                    float p = 0.0f;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out p))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ValueInPermille is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        p = 0.0f;
                                    }
                                    routeData.Blocks[blockIndex].Pitch = 0.001f * p;
                                }
                                break;
                            case "track.curve":
                                {
                                    float radius = 0.0f;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], unitOfLength, out radius))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Radius is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        radius = 0.0f;
                                    }
                                    float cant = 0.0f;
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseFloatVb6(arguments[1], out cant))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CantInMillimeters is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        cant = 0.0f;
                                    }
                                    else
                                    {
                                        cant *= 0.001f;
                                    }
                                    if (routeData.SignedCant)
                                    {
                                        if (radius != 0.0)
                                        {
                                            cant *= Mathf.Sign(radius);
                                        }
                                    }
                                    else
                                    {
                                        cant = Mathf.Abs(cant) * Mathf.Sign(radius);
                                    }
                                    routeData.Blocks[blockIndex].CurrentTrackState.CurveRadius = radius;
                                    routeData.Blocks[blockIndex].CurrentTrackState.CurveCant = cant;
                                    routeData.Blocks[blockIndex].CurrentTrackState.CurveCantTangent = 0.0f;
                                }
                                break;
                            case "track.turn":
                                {
                                    float s = 0.0f;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out s))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Ratio is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        s = 0.0f;
                                    }
                                    routeData.Blocks[blockIndex].Turn = s;
                                }
                                break;
                            case "track.adhesion":
                                {
                                    float a = 100.0f;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out a))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        a = 100.0f;
                                    }
                                    if (a < 0.0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        a = 100.0f;
                                    }
                                    routeData.Blocks[blockIndex].AdhesionMultiplier = 0.01f * a;
                                }
                                break;
                            case "track.brightness":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     float value = 255.0f;
                                    //     if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out value))
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Value is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //         value = 255.0f;
                                    //     }
                                    //     value /= 255.0f;
                                    //     if (value < 0.0f) value = 0.0f;
                                    //     if (value > 1.0f) value = 1.0f;
                                    //     int n = routeData.Blocks[blockIndex].Brightness.Length;
                                    //     Array.Resize<Brightness>(ref routeData.Blocks[blockIndex].Brightness, n + 1);
                                    //     routeData.Blocks[blockIndex].Brightness[n].TrackPosition = routeData.TrackPosition;
                                    //     routeData.Blocks[blockIndex].Brightness[n].Value = value;
                                    // }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "StartingDistance is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            start = 0.0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out end))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "EndingDistance is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            end = 0.0;
                                        }
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out r))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RedValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            r = 128;
                                        }
                                        else if (r < 0 | r > 255)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RedValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            r = r < 0 ? 0 : 255;
                                        }
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out g))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GreenValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            g = 128;
                                        }
                                        else if (g < 0 | g > 255)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "GreenValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            g = g < 0 ? 0 : 255;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseIntVb6(arguments[4], out b))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BlueValue is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            b = 128;
                                        }
                                        else if (b < 0 | b > 255)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BlueValue is required to be within the range from 0 to 255 in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "At least one argument is required in " + command + "at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            int[] aspects = new int[arguments.Length];
                                            for (int i = 0; i < arguments.Length; i++)
                                            {
                                                if (!Conversions.TryParseIntVb6(arguments[i], out aspects[i]))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Aspect" + i.ToString(culture) + " is invalid in " + command + "at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    aspects[i] = -1;
                                                }
                                                else if (aspects[i] < 0)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Aspect" + i.ToString(culture) + " is expected to be non-negative in " + command + "at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    aspects[i] = -1;
                                                }
                                            }
                                            bool valueBased = valueBasedSection | string.Equals(command, "Track.SectionS", StringComparison.OrdinalIgnoreCase);
                                            if (valueBased)
                                            {
                                                Array.Sort<int>(aspects);
                                            }

                                            int n = routeData.Blocks[blockIndex].Sections.Length;
                                            Array.Resize<Section>(ref routeData.Blocks[blockIndex].Sections, n + 1);
                                            int departureStationIndex = -1;
                                            // if (CurrentStation >= 0 && CurrentRoute.Stations[CurrentStation].ForceStopSignal)
                                            // {
                                            //     if (CurrentStation >= 0 & CurrentStop >= 0 & !DepartureSignalUsed)
                                            //     {
                                            //         departureStationIndex = CurrentStation;
                                            //         DepartureSignalUsed = true;
                                            //     }
                                            // }
                                            routeData.Blocks[blockIndex].Sections[n] = new Section(routeData.TrackPosition, aspects, departureStationIndex, valueBased ? SectionType.ValueBased : SectionType.IndexBased);

                                            // routeData.Blocks[blockIndex].Section[n].TrackPosition = routeData.TrackPosition;
                                            // routeData.Blocks[blockIndex].Section[n].Aspects = aspects;
                                            //data.Blocks[blockIndex].Section[n].Type = valueBased ? Game.SectionType.ValueBased : Game.SectionType.IndexBased;
                                            // routeData.Blocks[blockIndex].Section[n].DepartureStationIndex = -1;
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
                                    // if (!previewOnly)
                                    // {
                                    //     int objidx = 0;
                                    //     if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out objidx))
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "SignalIndex is invalid in Track.SigF at line " + command + "at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //         objidx = 0;
                                    //     }

                                    //     if (objidx >= 0 & routeData.Signals.ContainsKey(objidx))
                                    //     {
                                    //         int section = 0;
                                    //         if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out section))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Section is invalid in Track.SigF at line " + command + "at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             section = 0;
                                    //         }

                                    //         float x = 0.0f, y = 0.0f;
                                    //         if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out x))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in Track.SigF at line " + command + "at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             x = 0.0f;
                                    //         }

                                    //         if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseFloatVb6(arguments[3], unitOfLength, out y))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in Track.SigF at line " + command + "at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             y = 0.0f;
                                    //         }

                                    //         float yaw = 0.0f, pitch = 0.0f, roll = 0.0f;
                                    //         if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseFloatVb6(arguments[4], out yaw))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in Track.SigF at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             yaw = 0.0f;
                                    //         }

                                    //         if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseFloatVb6(arguments[5], out pitch))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in Track.SigF at line "+ expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             pitch = 0.0f;
                                    //         }

                                    //         if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseFloatVb6(arguments[6], out roll))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in Track.SigF at line "+ expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             roll = 0.0f;
                                    //         }

                                    //         int n = routeData.Blocks[BlockIndex].Signals.Length;
                                    //         Array.Resize(ref routeData.Blocks[BlockIndex].Signals, n + 1);
                                    //         routeData.Blocks[BlockIndex].Signals[n] = new Signal(routeData.TrackPosition, CurrentSection + section, routeData.Signals[objidx], new Vector2(x, y < 0.0f ? 4.8 : y), yaw.ToRadians(), pitch.ToRadians(), roll.ToRadians(), true, y < 0.0f);
                                    //     }
                                    //     else
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "SignalIndex " + objidx + " references a signal object not loaded in Track.SigF at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                    //     }
                                    // }
                                    
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Aspects is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            num = -2;
                                        }
                                        if (num != -2 & num != 2 & num != 3 & num != -4 & num != 4 & num != -5 & num != 5 & num != 6)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Aspects has an unsupported value in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            num = num == -3 | num == -6 ? -num : -4;
                                        }
                                        float x = 0.0f, y = 0.0f;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out x))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            x = 0.0f;
                                        }
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseFloatVb6(arguments[3], unitOfLength, out y))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            y = 0.0f;
                                        }
                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out yaw))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out pitch))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out roll))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        int n = routeData.Blocks[blockIndex].Sections.Length;
                                        Array.Resize<Section>(ref routeData.Blocks[blockIndex].Sections, n + 1);

                                        int departureStationIndex = -1;
                                        // if (CurrentStation >= 0 && CurrentRoute.Stations[CurrentStation].ForceStopSignal)
                                        // {
                                        //     if (CurrentStation >= 0 & CurrentStop >= 0 & !DepartureSignalUsed)
                                        //     {
                                        //         departureStationIndex = CurrentStation;
                                        //         DepartureSignalUsed = true;
                                        //     }
                                        // }

                                        routeData.Blocks[blockIndex].Sections[n] = new Section(routeData.TrackPosition, aspects, departureStationIndex, SectionType.ValueBased, x == 0.0);
                                        currentSection++;
                                        // TODO: compatibility signal(s)
                                        // n = routeData.Blocks[blockIndex].Signals.Length;
                                        // Array.Resize(ref routeData.Blocks[blockIndex].Signals, n + 1);
                                        // routeData.Blocks[blockIndex].Signals[n] = new Signal(routeData.TrackPosition, currentSection, routeData.CompatibilitySignals[comp], new Vector2(x, y < 0.0 ? 4.8 : y), yaw.ToRadians(), pitch.ToRadians(), roll.ToRadians(), x != 0.0, x != 0.0 & y < 0.0);
                                    }
                                }
                                break;
                            case "track.relay":
                                {
                                    if (!previewOnly)
                                    {
                                        float x = 0.0f, y = 0.0f;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], unitOfLength, out x))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in Track.Relay at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            x = 0.0f;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseFloatVb6(arguments[1], unitOfLength, out y))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in Track.Relay at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            y = 0.0f;
                                        }
                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[2], out yaw))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in Track.Relay at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[3], out pitch))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in Track.Relay at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[4], out roll))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in Track.Relay at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            roll = 0.0;
                                        }
                                        int n = routeData.Blocks[blockIndex].Signals.Length;
                                        Array.Resize<Signal>(ref routeData.Blocks[blockIndex].Signals, n + 1);
                                        routeData.Blocks[blockIndex].Signals[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Signals[n].Section = currentSection + 1;
                                        routeData.Blocks[blockIndex].Signals[n].SignalCompatibilityObjectIndex = 8;
                                        routeData.Blocks[blockIndex].Signals[n].SignalObjectIndex = -1;
                                        routeData.Blocks[blockIndex].Signals[n].X = x;
                                        routeData.Blocks[blockIndex].Signals[n].Y = y < 0.0 ? 4.8 : y;
                                        routeData.Blocks[blockIndex].Signals[n].Yaw = yaw * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Signals[n].Pitch = pitch * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Signals[n].Roll = roll * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Signals[n].ShowObject = x != 0.0;
                                        routeData.Blocks[blockIndex].Signals[n].ShowPost = x != 0.0 & y < 0.0;
                                    }
                                }
                                break;
                            case "track.beacon":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     int type = 0;
                                    //     if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out type))
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Type is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //         type = 0;
                                    //     }
                                    //     if (type < 0)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Type is expected to be non-positive in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else
                                    //     {
                                    //         int structure = 0, sec = 0, optional = 0;
                                    //         if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out structure))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BeaconStructureIndex is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             structure = 0;
                                    //         }
                                    //         if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sec))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "sec is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             sec = 0;
                                    //         }
                                    //         if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out optional))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Data is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             optional = 0;
                                    //         }
                                    //         if (structure < -1)
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BeaconStructureIndex is expected to be non-negative or -1 in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             structure = -1;
                                    //         }
                                    //         else if (structure >= 0 && (structure >= routeData.Structure.Beacon.Count || routeData.Structure.Beacon[structure] == null))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BeaconStructureIndex references an object not loaded in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             structure = -1;
                                    //         }
                                    //         if (sec == -1)
                                    //         {
                                    //             //sec = (int)TrackManager.TransponderSpecialsec.NextRedsec;
                                    //         }
                                    //         else if (sec < 0)
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "sec is expected to be non-negative or -1 in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             sec = currentsection + 1;
                                    //         }
                                    //         else
                                    //         {
                                    //             sec += currentsection;
                                    //         }
                                    //         float x = 0.0f, y = 0.0f;
                                    //         float yaw = 0.0f, pitch = 0.0f, roll = 0.0f;
                                    //         if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseFloatVb6(arguments[4], unitOfLength, out x))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             x = 0.0f;
                                    //         }
                                    //         if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseFloatVb6(arguments[5], unitOfLength, out y))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             y = 0.0f;
                                    //         }
                                    //         if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseFloatVb6(arguments[6], out yaw))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             yaw = 0.0f;
                                    //         }
                                    //         if (arguments.Length >= 8 && arguments[7].Length > 0 && !Conversions.TryParseFloatVb6(arguments[7], out pitch))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             pitch = 0.0f;
                                    //         }
                                    //         if (arguments.Length >= 9 && arguments[8].Length > 0 && !Conversions.TryParseFloatVb6(arguments[8], out roll))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in Track.Beacon at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             roll = 0.0f;
                                    //         }
                                    //         int n = routeData.Blocks[blockIndex].Transponders.Length;
                                    //         Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponders, n + 1);
                                    //         routeData.Blocks[blockIndex].Transponders[n].TrackPosition = routeData.TrackPosition;
                                    //         routeData.Blocks[blockIndex].Transponders[n].Type = type;
                                    //         routeData.Blocks[blockIndex].Transponders[n].Data = optional;
                                    //         routeData.Blocks[blockIndex].Transponders[n].BeaconStructureIndex = structure;
                                    //         routeData.Blocks[blockIndex].Transponders[n].sec = sec;
                                    //         routeData.Blocks[blockIndex].Transponders[n].ShowDefaultObject = false;
                                    //         routeData.Blocks[blockIndex].Transponders[n].X = x;
                                    //         routeData.Blocks[blockIndex].Transponders[n].Y = y;
                                    //         routeData.Blocks[blockIndex].Transponders[n].Yaw = yaw * 0.0174532925199433;
                                    //         routeData.Blocks[blockIndex].Transponders[n].Pitch = pitch * 0.0174532925199433;
                                    //         routeData.Blocks[blockIndex].Transponders[n].Roll = roll * 0.0174532925199433;
                                    //     }
                                    // }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Type is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            type = 0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out oversig))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Signals is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            oversig = 0;
                                        }
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out work))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "SwitchSystems is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            work = 0;
                                        }
                                        if (oversig < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Signals is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            oversig = 0;
                                        }

                                        float x = 0.0f, y = 0.0f;
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseFloatVb6(arguments[3], unitOfLength, out x))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            x = 0.0f;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseFloatVb6(arguments[4], unitOfLength, out y))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            y = 0.0f;
                                        }

                                        double yaw = 0.0, pitch = 0.0, roll = 0.0;
                                        if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[5], out yaw))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            yaw = 0.0;
                                        }
                                        if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[6], out pitch))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            pitch = 0.0;
                                        }
                                        if (arguments.Length >= 8 && arguments[7].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[7], out roll))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            roll = 0.0;
                                        }

                                        int n = routeData.Blocks[blockIndex].Transponders.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponders, n + 1);
                                        routeData.Blocks[blockIndex].Transponders[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Transponders[n].Type = type;
                                        routeData.Blocks[blockIndex].Transponders[n].Data = work;
                                        routeData.Blocks[blockIndex].Transponders[n].ShowDefaultObject = true;
                                        routeData.Blocks[blockIndex].Transponders[n].BeaconStructureIndex = -1;
                                        routeData.Blocks[blockIndex].Transponders[n].X = x;
                                        routeData.Blocks[blockIndex].Transponders[n].Y = y;
                                        routeData.Blocks[blockIndex].Transponders[n].Yaw = yaw * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Transponders[n].Pitch = pitch * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Transponders[n].Roll = roll * 0.0174532925199433;
                                        routeData.Blocks[blockIndex].Transponders[n].Section = currentSection + oversig + 1;
                                        routeData.Blocks[blockIndex].Transponders[n].ClipToFirstRedSection = true;
                                    }
                                }
                                break;
                            case "track.atssn":
                                {
                                    if (!previewOnly)
                                    {
                                        int n = routeData.Blocks[blockIndex].Transponders.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponders, n + 1);
                                        routeData.Blocks[blockIndex].Transponders[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Transponders[n].Type = 0;
                                        routeData.Blocks[blockIndex].Transponders[n].Data = 0;
                                        routeData.Blocks[blockIndex].Transponders[n].ShowDefaultObject = true;
                                        routeData.Blocks[blockIndex].Transponders[n].BeaconStructureIndex = -1;
                                        routeData.Blocks[blockIndex].Transponders[n].Section = currentSection + 1;
                                        routeData.Blocks[blockIndex].Transponders[n].ClipToFirstRedSection = true;
                                    }
                                }
                                break;
                            case "track.atsp":
                                {
                                    if (!previewOnly)
                                    {
                                        int n = routeData.Blocks[blockIndex].Transponders.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponders, n + 1);
                                        routeData.Blocks[blockIndex].Transponders[n].TrackPosition = routeData.TrackPosition;
                                        routeData.Blocks[blockIndex].Transponders[n].Type = 3;
                                        routeData.Blocks[blockIndex].Transponders[n].Data = 0;
                                        routeData.Blocks[blockIndex].Transponders[n].ShowDefaultObject = true;
                                        routeData.Blocks[blockIndex].Transponders[n].BeaconStructureIndex = -1;
                                        routeData.Blocks[blockIndex].Transponders[n].Section = currentSection + 1;
                                        routeData.Blocks[blockIndex].Transponders[n].ClipToFirstRedSection = true;
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Type is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            type = 0;
                                        }
                                        double speed = 0.0;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out speed))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            speed = 0.0;
                                        }
                                        int n = routeData.Blocks[blockIndex].Transponders.Length;
                                        Array.Resize<Transponder>(ref routeData.Blocks[blockIndex].Transponders, n + 1);
                                        routeData.Blocks[blockIndex].Transponders[n].TrackPosition = routeData.TrackPosition;
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
                                        routeData.Blocks[blockIndex].Transponders[n].Section = -1;
                                        routeData.Blocks[blockIndex].Transponders[n].BeaconStructureIndex = -1;
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
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is invalid in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                                    float limit = 0.0f;
                                    int direction = 0, cource = 0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], out limit))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is invalid in Track.Limit at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        limit = 0.0f;
                                    }
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out direction))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Direction is invalid in Track.Limit at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        direction = 0;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out cource))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Cource is invalid in Track.Limit at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        cource = 0;
                                    }
                                    // int n = routeData.Blocks[blockIndex].Limits.Length;
                                    // Array.Resize<Limit>(ref routeData.Blocks[blockIndex].Limits, n + 1);
                                    // routeData.Blocks[blockIndex].Limits[n].TrackPosition = routeData.TrackPosition;
                                    // routeData.Blocks[blockIndex].Limits[n].Speed = limit <= 0.0 ? double.PositiveInfinity : routeData.UnitOfSpeed * limit;
                                    // routeData.Blocks[blockIndex].Limits[n].Direction = direction;
                                    // routeData.Blocks[blockIndex].Limits[n].Cource = cource;

                                    int n = routeData.Blocks[blockIndex].Limits.Length;
		                			Array.Resize(ref routeData.Blocks[blockIndex].Limits, n + 1);
					                routeData.Blocks[blockIndex].Limits[n] = new Limit(routeData.TrackPosition, limit <= 0.0f ? float.PositiveInfinity : routeData.UnitOfSpeed * limit, direction, cource);
		
                                }
                                break;
                            case "track.stop":
                                if (currentStation == -1)
                                {
                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "A stop without a station is invalid in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                }
                                else
                                {
                                    int dir = 0;
                                    if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out dir))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Direction is invalid in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        dir = 0;
                                    }
                                    float backw = 5.0f, forw = 5.0f;
                                    if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseFloatVb6(arguments[1], unitOfLength, out backw))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackwardTolerance is invalid in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        backw = 5.0f;
                                    }
                                    else if (backw <= 0.0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackwardTolerance is expected to be positive in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        backw = 5.0f;
                                    }
                                    if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out forw))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ForwardTolerance is invalid in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        forw = 5.0f;
                                    }
                                    else if (forw <= 0.0)
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ForwardTolerance is expected to be positive in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        forw = 5.0f;
                                    }
                                    int cars = 0;
                                    if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out cars))
                                    {
                                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Cars is invalid in Track.Stop at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        cars = 0;
                                    }
                                    int n = routeData.Blocks[blockIndex].StopPositions.Length;
                                    Array.Resize<Stop>(ref routeData.Blocks[blockIndex].StopPositions, n + 1);
                                    routeData.Blocks[blockIndex].StopPositions[n].TrackPosition = routeData.TrackPosition;
                                    routeData.Blocks[blockIndex].StopPositions[n].Station = currentStation;
                                    routeData.Blocks[blockIndex].StopPositions[n].Direction = dir;
                                    routeData.Blocks[blockIndex].StopPositions[n].ForwardTolerance = forw;
                                    routeData.Blocks[blockIndex].StopPositions[n].BackwardTolerance = backw;
                                    routeData.Blocks[blockIndex].StopPositions[n].Cars = cars;
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
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                arr = -1.0;
                            //            }
                            //        }
                            //        else if (!Conversions.TryParseTime(arguments[1], out arr))
                            //        {
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                dep = -1.0;
                            //            }
                            //        }
                            //        else if (!Conversions.TryParseTime(arguments[2], out dep))
                            //        {
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            dep = -1.0;
                            //        }
                            //    }
                            //    int passalarm = 0;
                            //    if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out passalarm))
                            //    {
                            //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PassAlarm is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Doors is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    door = 0;
                            //                }
                            //                break;
                            //        }
                            //    }
                            //    int stop = 0;
                            //    if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseIntVb6(arguments[5], out stop))
                            //    {
                            //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ForcedRedSignal is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "System is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            device = 0;
                            //        }
                            //        if (device != 0 & device != 1)
                            //        {
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "System is not supported in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalSound contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            }
                            //            else
                            //            {
                            //                string f = System.IO.Path.Combine(soundPath, arguments[7]);
                            //                if (!File.Exists(f))
                            //                {
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalSound " + f + " not found in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "StopDuration is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PassengerRatio is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            jam = 100.0;
                            //        }
                            //        else if (jam < 0.0)
                            //        {
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PassengerRatio is expected to be non-negative in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            jam = 100.0;
                            //        }
                            //    }
                            //    if (!previewOnly)
                            //    {
                            //        if (arguments.Length >= 11 && arguments[10].Length > 0)
                            //        {
                            //            if (arguments[10].LastIndexOfAny(Path.GetInvalidPathChars()) >= 0)
                            //            {
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureSound contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //            }
                            //            else
                            //            {
                            //                string f = System.IO.Path.Combine(soundPath, arguments[10]);
                            //                if (!File.Exists(f))
                            //                {
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureSound " + f + " not found in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "TimetableIndex is expected to be non-negative in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    ttidx = -1;
                            //                }
                            //                else if (ttidx >= data.TimetableDaytime.Length & ttidx >= data.TimetableNighttime.Length)
                            //                {
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "TimetableIndex references textures not loaded in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    arr = -1.0;
                            //                }
                            //            }
                            //            else if (!Conversions.TryParseTime(arguments[1], out arr))
                            //            {
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ArrivalTime is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureTime is invalid in Track.Sta at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                    dep = -1.0;
                            //                }
                            //            }
                            //            else if (!Conversions.TryParseTime(arguments[2], out dep))
                            //            {
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureTime is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                dep = -1.0;
                            //            }
                            //        }
                            //        int stop = 0;
                            //        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out stop))
                            //        {
                            //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "ForcedRedSignal is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "System is invalid in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                device = 0;
                            //            }
                            //            else if (device != 0 & device != 1)
                            //            {
                            //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "System is not supported in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                            //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureSound contains illegal characters in " + command + " at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                            //                }
                            //                else
                            //                {
                            //                    string f = System.IO.Path.Combine(soundPath, arguments[5]);
                            //                    if (!File.Exists(f))
                            //                    {
                            //                         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DepartureSound " + f + " not found in Track.Station at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex1 is invalid in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex2 is invalid in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex1 is expected to be non-negative in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (idx2 < 0 & idx2 != Form.SecondaryRailStub & idx2 != Form.SecondaryRailL & idx2 != Form.SecondaryRailR)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex2 is expected to be greater or equal to -2 in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                           if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx1) || !routeData.Blocks[blockIndex].Rails[idx1].RailStarted)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex1 could be out of range in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (idx2 != Form.SecondaryRailStub & idx2 != Form.SecondaryRailL & idx2 != Form.SecondaryRailR && (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx2) || !routeData.Blocks[blockIndex].Rails[idx2].RailStarted))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex2 could be out of range in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            int roof = 0, pf = 0;
                                            if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out roof))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex is invalid in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                roof = 0;
                                            }

                                            if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseIntVb6(arguments[3], out pf))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex is invalid in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                pf = 0;
                                            }

                                            if (roof != 0 & (roof < 0 || (!routeData.Structure.RoofL.ContainsKey(roof) && !routeData.Structure.RoofR.ContainsKey(roof))))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex " + roof + " references an object not loaded in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (pf < 0 | (!routeData.Structure.FormL.ContainsKey(pf) & !routeData.Structure.FormR.ContainsKey(pf)))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex " + pf + " references an object not loaded in Track.Form at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            int n = routeData.Blocks[blockIndex].Forms.Length;
                                            Array.Resize(ref routeData.Blocks[blockIndex].Forms, n + 1);
                                            routeData.Blocks[blockIndex].Forms[n] = new Form(idx1, idx2, pf, roof, routeData.Structure);
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
                                            // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.Pole at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                            idx = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be non-negative in Track.Pole at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        }
                                        else
                                        {
                                            if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx) || !routeData.Blocks[blockIndex].Rails[idx].RailStarted)
                                            {
                                                // Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex " + idx + " could be out of range in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                            }

                                            if (idx >= routeData.Blocks[blockIndex].RailPole.Length)
                                            {
                                                Array.Resize(ref routeData.Blocks[blockIndex].RailPole, idx + 1);
                                                routeData.Blocks[blockIndex].RailPole[idx] = new Pole();
                                                routeData.Blocks[blockIndex].RailPole[idx].Mode = 0;
                                                routeData.Blocks[blockIndex].RailPole[idx].Location = 0;
                                                routeData.Blocks[blockIndex].RailPole[idx].Interval = 2.0f * routeData.BlockInterval;
                                                routeData.Blocks[blockIndex].RailPole[idx].Type = 0;
                                            }

                                            int typ = routeData.Blocks[blockIndex].RailPole[idx].Mode;
                                            int sttype = routeData.Blocks[blockIndex].RailPole[idx].Type;
                                            if (arguments.Length >= 2 && arguments[1].Length > 0)
                                            {
                                                if (!Conversions.TryParseIntVb6(arguments[1], out typ))
                                                {
//                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "AdditionalRailsCovered is invalid in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                                    typ = 0;
                                                }
                                            }

                                            if (arguments.Length >= 3 && arguments[2].Length > 0)
                                            {
                                                float loc;
                                                if (!Conversions.TryParseFloatVb6(arguments[2], out loc))
                                                {
                                                    // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Location is invalid in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                                    loc = 0.0f;
                                                }

                                                routeData.Blocks[blockIndex].RailPole[idx].Location = loc;
                                            }

                                            if (arguments.Length >= 4 && arguments[3].Length > 0)
                                            {
                                                float dist;
                                                if (!Conversions.TryParseFloatVb6(arguments[3], unitOfLength, out dist))
                                                {
                                                    // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Interval is invalid in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                                    dist = routeData.BlockInterval;
                                                }

                                                routeData.Blocks[blockIndex].RailPole[idx].Interval = dist;
                                            }

                                            if (arguments.Length >= 5 && arguments[4].Length > 0)
                                            {
                                                if (!Conversions.TryParseIntVb6(arguments[4], out sttype))
                                                {
                                                    // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PoleStructureIndex is invalid in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                                    sttype = 0;
                                                }
                                            }

                                            if (typ < 0 || !routeData.Structure.Poles.ContainsKey(typ) || routeData.Structure.Poles[typ] == null)
                                            {
                                                // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PoleStructureIndex " + typ + " references an object not loaded in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
                                            }
                                            else if (sttype < 0 || !routeData.Structure.Poles[typ].ContainsKey(sttype) || routeData.Structure.Poles[typ][sttype] == null)
                                            {
                                                // Plugin.CurrentHost.AddMessage(MessageType.Error, false, "PoleStructureIndex " + typ + " references an object not loaded in Track.Pole at line " + Expression.Line.ToString(Culture) + ", column " + Expression.Column.ToString(Culture) + " in file " + Expression.File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.PoleEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }
                                        if (idx < 0 | idx >= routeData.Blocks[blockIndex].RailPole.Length)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex does not reference an existing pole in Track.PoleEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (idx >= routeData.Blocks[blockIndex].Rails.Count || (!routeData.Blocks[blockIndex].Rails[idx].RailStarted & !routeData.Blocks[blockIndex].Rails[idx].RailEnded))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex could be out of range in Track.PoleEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.Wall at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }

                                        if (idx < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be a non-negative integer in Track.Wall at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }

                                        Direction dir = Direction.Invalid;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0)
                                        {
                                            dir = FindDirection(arguments[1], "Track.Wall", true, routeExpressions[j].Line, routeExpressions[j].File);
                                        }
                                        if (dir == Direction.Invalid || dir == Direction.None)
                                        {
                                            break;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sttype))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex is invalid in Track.Wall at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }

                                        if (sttype < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex is expected to be a non-negative integer in Track.Wall at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }

                                        if (dir < 0 && !routeData.Structure.WallL.ContainsKey(sttype) || dir > 0 && !routeData.Structure.WallR.ContainsKey(sttype) || dir == 0 && (!routeData.Structure.WallL.ContainsKey(sttype) && !routeData.Structure.WallR.ContainsKey(sttype)))
                                        {
                                            if (dir < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex " + sttype + " references an object not loaded in Track.WallL at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (dir > 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex " + sttype + " references an object not loaded in Track.WallR at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "WallStructureIndex " + sttype + " references an object not loaded in Track.WallBothSides at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                        }
                                        else
                                        {
                                            if (dir == Direction.Both)
                                            {
                                                if (!routeData.Structure.WallL.ContainsKey(sttype))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "LeftWallStructureIndex " + sttype + " references an object not loaded in Track.WallBothSides at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    dir = Direction.Right;
                                                }

                                                if (!routeData.Structure.WallR.ContainsKey(sttype))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RightWallStructureIndex " + sttype + " references an object not loaded in Track.WallBothSides at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    dir = Direction.Left;
                                                }
                                            }

                                            if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx) || !routeData.Blocks[blockIndex].Rails[idx].RailStarted)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex " + idx + " could be out of range in Track.Wall at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (routeData.Blocks[blockIndex].RailWall.ContainsKey(idx))
                                            {
                                                routeData.Blocks[blockIndex].RailWall[idx] = new WallDike(sttype, dir, routeData.Structure.WallL, routeData.Structure.WallR);
                                            }
                                            else
                                            {
                                                routeData.Blocks[blockIndex].RailWall.Add(idx, new WallDike(sttype, dir, routeData.Structure.WallL, routeData.Structure.WallR));
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.WallEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }

                                        if (!routeData.Blocks[blockIndex].RailWall.ContainsKey(idx))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex " + idx + " does not reference an existing wall in Track.WallEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " +  routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx) || (!routeData.Blocks[blockIndex].Rails[idx].RailStarted & !routeData.Blocks[blockIndex].Rails[idx].RailEnded))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex " + idx + " could be out of range in Track.WallEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " +  routeExpressions[j].File);
                                            }

                                            if (routeData.Blocks[blockIndex].RailWall.ContainsKey(idx))
                                            {
                                                routeData.Blocks[blockIndex].RailWall[idx].Exists = false;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.dike":
                                {
                                    if (!previewOnly)
                                    {
                                        int railIdx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out railIdx))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.Dike at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            railIdx = 0;
                                        }

                                        if (railIdx < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be a non-negative integer in Track.Dike at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            railIdx = 0;
                                        }

                                        Direction dir = Direction.Invalid;

                                        if (arguments.Length >= 2 && arguments[1].Length > 0)
                                        {
                                            dir = FindDirection(arguments[1], "Track.Dike", true, routeExpressions[j].Line, routeExpressions[j].File);
                                        }
                                        if (dir == Direction.Invalid || dir == Direction.None)
                                        {
                                            break;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sttype))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex is invalid in Track.Dike at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }

                                        if (sttype < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex is expected to be a non-negative integer in Track.DikeL at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }

                                        if (dir < 0 && !routeData.Structure.DikeL.ContainsKey(sttype) || dir > 0 && !routeData.Structure.DikeR.ContainsKey(sttype) || dir == 0 && (!routeData.Structure.DikeL.ContainsKey(sttype) && !routeData.Structure.DikeR.ContainsKey(sttype)))
                                        {
                                            if (dir > 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex " + sttype + " references an object not loaded in Track.DikeL at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (dir < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex " + sttype + " references an object not loaded in Track.DikeR at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex " + sttype + " references an object not loaded in Track.DikeBothSides at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "DikeStructureIndex " + sttype + " references an object not loaded in Track.Dike at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (dir == Direction.Both)
                                            {
                                                if (!routeData.Structure.DikeL.ContainsKey(sttype))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "LeftDikeStructureIndex " + sttype + " references an object not loaded in Track.DikeBothSides at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    dir = Direction.Right;
                                                }

                                                if (!routeData.Structure.DikeR.ContainsKey(sttype))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RightDikeStructureIndex " + sttype + " references an object not loaded in Track.DikeBothSides at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    dir = Direction.Left;
                                                }
                                            }

                                            if (!routeData.Blocks[blockIndex].Rails.ContainsKey(railIdx) || !routeData.Blocks[blockIndex].Rails[railIdx].RailStarted)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex " + railIdx + " could be out of range in Track.Dike at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (routeData.Blocks[blockIndex].RailDike.ContainsKey(railIdx))
                                            {
                                                routeData.Blocks[blockIndex].RailDike[railIdx] = new WallDike(sttype, dir, routeData.Structure.DikeL, routeData.Structure.DikeR);
                                            }
                                            else
                                            {
                                                routeData.Blocks[blockIndex].RailDike.Add(railIdx, new WallDike(sttype, dir, routeData.Structure.DikeL, routeData.Structure.DikeR));
                                            }
                                        }

                                    }
                                }
                                break;
                            case "track.dikeend":
                                {
                                    if (!previewOnly)
                                    {
                                        int railIdx = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out railIdx))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.DikeEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            railIdx = 0;
                                        }
                                        
                                        if (!routeData.Blocks[blockIndex].RailDike.ContainsKey(railIdx))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex does not reference an existing dike in Track.DikeEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (!routeData.Blocks[blockIndex].Rails.ContainsKey(railIdx) || (!routeData.Blocks[blockIndex].Rails[railIdx].RailStarted & !routeData.Blocks[blockIndex].Rails[railIdx].RailEnded))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex could be out of range in Track.DikeEnd at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (routeData.Blocks[blockIndex].RailDike.ContainsKey(railIdx))
                                            {
                                                routeData.Blocks[blockIndex].RailDike[railIdx].Exists = false;
                                            }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Track.Marker is expected to have at least one argument at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            string f = System.IO.Path.Combine(objectPath, arguments[0]);
                                            if (!System.IO.File.Exists(f))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName " + f + " not found in Track.Marker at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                float dist = routeData.BlockInterval;
                                                if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseFloatVb6(arguments[1], unitOfLength, out dist))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Distance is invalid in Track.Marker at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
                                        float h = 0.0f;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseFloatVb6(arguments[0], unitOfLength, out h))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Height is invalid in Track.Height at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            h = 0.0f;
                                        }
                                        routeData.Blocks[blockIndex].Height = isRW ? h + 0.3f : h;
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CycleIndex is invalid in Track.Ground at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            cytype = 0;
                                        }

                                        if (cytype < routeData.Structure.GroundCycles.Length && routeData.Structure.GroundCycles[cytype] != null)
                                        {
                                            routeData.Blocks[blockIndex].GroundCycles = routeData.Structure.GroundCycles[cytype];
                                        }
                                        else
                                        {
                                            if (!routeData.Structure.Ground.ContainsKey(cytype))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CycleIndex " + cytype + " references an object not loaded in Track.Ground at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                routeData.Blocks[blockIndex].GroundCycles = new[] { cytype };
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex1 is invalid in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx1 = 0;
                                        }
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out idx2))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex2 is invalid in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx2 = 0;
                                        }
                                        int sttype = 0;
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseIntVb6(arguments[2], out sttype))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CrackStructureIndex is invalid in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }
                                        if (sttype < 0 || (sttype >= routeData.Structure.CrackL.Count || routeData.Structure.CrackL[sttype] == null) || (sttype >= routeData.Structure.CrackR.Count || routeData.Structure.CrackR[sttype] == null))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "CrackStructureIndex references an object not loaded in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (idx1 < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex1 is expected to be non-negative in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (idx2 < 0)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex2 is expected to be non-negative in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else if (idx1 == idx2)
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex1 is expected to be unequal to Index2 in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                if (idx1 >= routeData.Blocks[blockIndex].Rails.Count || !routeData.Blocks[blockIndex].Rails[idx1].RailStarted)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex1 could be out of range in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                if (idx2 >= routeData.Blocks[blockIndex].Rails.Count || !routeData.Blocks[blockIndex].Rails[idx2].RailStarted)
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex2 could be out of range in Track.Crack at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }
                                                int n = routeData.Blocks[blockIndex].Cracks.Length;
                                                Array.Resize<Crack>(ref routeData.Blocks[blockIndex].Cracks, n + 1);
                                                routeData.Blocks[blockIndex].Cracks[n].PrimaryRail = idx1;
                                                routeData.Blocks[blockIndex].Cracks[n].SecondaryRail = idx2;
                                                routeData.Blocks[blockIndex].Cracks[n].Type = sttype;
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.freeobj":
                                {
                                    if (!previewOnly)
                                    {
                                        if (arguments.Length < 2)
                                        {
                                            /*
                                             * If no / one arguments are supplied, this previously produced FreeObject 0 dropped on either
                                             * Rail 0 (no arguments) or on the rail specified by the first argument.
                                             *
                                             * BVE4 ignores these, and we should too.
                                             */
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "An insufficient number of arguments was supplied in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            return;
                                        }

                                        int idx = 0, sttype = 0;
                                        if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out idx))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }

                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseIntVb6(arguments[1], out sttype))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FreeObjStructureIndex is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            sttype = 0;
                                        }

                                        if (idx < -1)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be non-negative or -1 in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else if (sttype < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FreeObjStructureIndex is expected to be non-negative in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        else
                                        {
                                            if (idx >= 0 && (!routeData.Blocks[blockIndex].Rails.ContainsKey(idx) || !routeData.Blocks[blockIndex].Rails[idx].RailStarted))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Warning, false, "RailIndex " + idx + " could be out of range in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }

                                            if (!routeData.Structure.FreeObjects.ContainsKey(sttype))
                                            {
                                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FreeObjStructureIndex " + sttype + " references an object not loaded in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            }
                                            else
                                            {
                                                Vector2 objectPosition = new Vector2();
                                                float yaw = 0.0f, pitch = 0.0f, roll = 0.0f;
                                                if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out objectPosition.x))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }

                                                if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseFloatVb6(arguments[3], unitOfLength, out objectPosition.y))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                }

                                                if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseFloatVb6(arguments[4], out yaw))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    yaw = 0.0f;
                                                }

                                                if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseFloatVb6(arguments[5], out pitch))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    pitch = 0.0f;
                                                }

                                                if (arguments.Length >= 7 && arguments[6].Length > 0 && !Conversions.TryParseFloatVb6(arguments[6], out roll))
                                                {
                                                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in Track.FreeObj at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                                    roll = 0.0f;
                                                }

                                                if (idx == -1)
                                                {

                                                    if (!routeData.IgnorePitchRoll)
                                                    {
                                                        routeData.Blocks[blockIndex].GroundFreeObj.Add(new FreeObj(routeData.TrackPosition, sttype, objectPosition, Godot.Mathf.Deg2Rad(yaw), Godot.Mathf.Deg2Rad(pitch), Godot.Mathf.Deg2Rad(roll)));
                                                    }
                                                    else
                                                    {
                                                        routeData.Blocks[blockIndex].GroundFreeObj.Add(new FreeObj(routeData.TrackPosition, sttype, objectPosition, Godot.Mathf.Deg2Rad(yaw)));
                                                    }
                                                }
                                                else
                                                {
                                                    if (!routeData.Blocks[blockIndex].RailFreeObj.ContainsKey(idx))
                                                    {
                                                        routeData.Blocks[blockIndex].RailFreeObj.Add(idx, new List<FreeObj>());
                                                    }
                                                    if (!routeData.IgnorePitchRoll)
                                                    {
                                                        routeData.Blocks[blockIndex].RailFreeObj[idx].Add(new FreeObj(routeData.TrackPosition, sttype, objectPosition, Godot.Mathf.Deg2Rad(yaw), Godot.Mathf.Deg2Rad(pitch), Godot.Mathf.Deg2Rad(roll)));
                                                    }
                                                    else
                                                    {
                                                        routeData.Blocks[blockIndex].RailFreeObj[idx].Add(new FreeObj(routeData.TrackPosition, sttype, objectPosition, Godot.Mathf.Deg2Rad(yaw)));
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "track.back":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     int typ = 0;
                                    //     if (arguments.Length >= 1 && arguments[0].Length > 0 && !Conversions.TryParseIntVb6(arguments[0], out typ))
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //         typ = 0;
                                    //     }
                                    //     if (typ < 0 | typ >= routeData.Backgrounds.Length)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex references a texture not loaded in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else if (routeData.Backgrounds[typ] == null)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, "BackgroundTextureIndex has not been loaded via Texture.Background in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else
                                    //     {
                                    //         routeData.Blocks[blockIndex].Background = typ;
                                    //     }
                                    // }
                                }
                                break;
                            case "track.announce":
                                {
                                    if (!previewOnly)
                                    {
                                        //     if (arguments.Length == 0)
                                        //     {
                                        //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have between 1 and 2 arguments at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        //     }
                                        //     else
                                        //     {
                                        //         if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                        //         {
                                        //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        //         }
                                        //         else
                                        //         {
                                        //             string f = System.IO.Path.Combine(soundPath, arguments[0]);
                                        //             if (!System.IO.File.Exists(f))
                                        //             {
                                        //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName " + f + " not found in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        //             }
                                        //             else
                                        //             {
                                        //                 double speed = 0.0;
                                        //                 if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseDoubleVb6(arguments[1], out speed))
                                        //                 {
                                        //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Speed is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                        //                     speed = 0.0;
                                        //                 }
                                        //                 int n = routeData.Blocks[blockIndex].SoundEvents.Length;
                                        //                 Array.Resize<Sound>(ref routeData.Blocks[blockIndex].SoundEvents, n + 1);
                                        //                 routeData.Blocks[blockIndex].SoundEvents[n].TrackPosition = routeData.TrackPosition;
                                        //                 const double radius = 15.0;
                                        //                 //data.Blocks[blockIndex].Sound[n].SoundBuffer = Sounds.RegisterBuffer(f, radius);
                                        //                 routeData.Blocks[blockIndex].SoundEvents[n].Type = speed == 0.0 ? SoundType.TrainStatic : SoundType.TrainDynamic;
                                        //                 routeData.Blocks[blockIndex].SoundEvents[n].Speed = speed * routeData.UnitOfSpeed;
                                        //             }
                                        //         }
                                        //     }
                                    }
                                }
                                break;
                            case "track.doppler":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     if (arguments.Length == 0)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have between 1 and 3 arguments at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else
                                    //     {
                                    //         if (arguments[0].LastIndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName contains illegal characters in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //         }
                                    //         else
                                    //         {
                                    //             string f = System.IO.Path.Combine(soundPath, arguments[0]);
                                    //             if (!System.IO.File.Exists(f))
                                    //             {
                                    //                 Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FileName " + f + " not found in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             }
                                    //             else
                                    //             {
                                    //                 double x = 0.0, y = 0.0;
                                    //                 if (arguments.Length >= 2 && arguments[1].Length > 0 & !Conversions.TryParseDoubleVb6(arguments[1], unitOfLength, out x))
                                    //                 {
                                    //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //                     x = 0.0;
                                    //                 }
                                    //                 if (arguments.Length >= 3 && arguments[2].Length > 0 & !Conversions.TryParseDoubleVb6(arguments[2], unitOfLength, out y))
                                    //                 {
                                    //                     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //                     y = 0.0;
                                    //                 }
                                    //                 int n = routeData.Blocks[blockIndex].SoundEvents.Length;
                                    //                 Array.Resize<Sound>(ref routeData.Blocks[blockIndex].SoundEvents, n + 1);
                                    //                 routeData.Blocks[blockIndex].SoundEvents[n].TrackPosition = routeData.TrackPosition;
                                    //                 const double radius = 15.0;
                                    //                 //data.Blocks[blockIndex].Sound[n].SoundBuffer = Sounds.RegisterBuffer(f, radius);
                                    //                 routeData.Blocks[blockIndex].SoundEvents[n].Type = SoundType.World;
                                    //                 routeData.Blocks[blockIndex].SoundEvents[n].X = x;
                                    //                 routeData.Blocks[blockIndex].SoundEvents[n].Y = y;
                                    //                 routeData.Blocks[blockIndex].SoundEvents[n].Radius = radius;
                                    //             }
                                    //         }
                                    //     }
                                    // }
                                }
                                break;
                            case "track.pretrain":
                                {
                                    // if (!previewOnly)
                                    // {
                                    //     if (arguments.Length == 0)
                                    //     {
                                    //         Plugin.CurrentHost.AddMessage(MessageType.Error, false, command + " is expected to have exactly 1 argument at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //     }
                                    //     else
                                    //     {
                                    //         double time = 0.0;
                                    //         if (arguments[0].Length > 0 & !Conversions.TryParseTime(arguments[0], out time))
                                    //         {
                                    //             Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Time is invalid in " + command + " at line " + expressions[j].Line.ToString(culture) + ", column " + expressions[j].Column.ToString(culture) + " in file " + expressions[j].File);
                                    //             time = 0.0;
                                    //         }
                                    //         //int n = Game.BogusPretrainInstructions.Length;
                                    //         //if (n != 0 && Game.BogusPretrainInstructions[n - 1].Time >= time)
                                    //         //{
                                    //         //     Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Time is expected to be in ascending order between successive " + command + " commands at line " + expressions[j].line.ToString(culture) + ", column " + expressions[j].column.ToString(culture) + " in file " + expressions[j].file);
                                    //         //}
                                    //         //Array.Resize<Game.BogusPretrainInstruction>(ref Game.BogusPretrainInstructions, n + 1);
                                    //         //Game.BogusPretrainInstructions[n].TrackPosition = data.TrackPosition;
                                    //         //Game.BogusPretrainInstructions[n].Time = time;
                                    //     }
                                    // }
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
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }
                                        if (idx < 0)
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex is expected to be non-negative in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            idx = 0;
                                        }
                                        if (idx >= 0 && (idx >= routeData.Blocks[blockIndex].Rails.Count || !routeData.Blocks[blockIndex].Rails[idx].RailStarted))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex references a non-existing rail in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                        }
                                        float x = 0.0f, y = 0.0f;
                                        if (arguments.Length >= 2 && arguments[1].Length > 0 && !Conversions.TryParseFloatVb6(arguments[1], unitOfLength, out x))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "X is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            x = 0.0f;
                                        }
                                        if (arguments.Length >= 3 && arguments[2].Length > 0 && !Conversions.TryParseFloatVb6(arguments[2], unitOfLength, out y))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Y is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            y = 0.0f;
                                        }
                                        float yaw = 0.0f, pitch = 0.0f, roll = 0.0f;
                                        if (arguments.Length >= 4 && arguments[3].Length > 0 && !Conversions.TryParseFloatVb6(arguments[3], out yaw))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Yaw is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            yaw = 0.0f;
                                        }
                                        if (arguments.Length >= 5 && arguments[4].Length > 0 && !Conversions.TryParseFloatVb6(arguments[4], out pitch))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Pitch is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            pitch = 0.0f;
                                        }
                                        if (arguments.Length >= 6 && arguments[5].Length > 0 && !Conversions.TryParseFloatVb6(arguments[5], out roll))
                                        {
                                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "Roll is invalid in " + command + " at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
                                            roll = 0.0f;
                                        }
                                        string text = null;
                                        if (arguments.Length >= 7 && arguments[6].Length != 0)
                                        {
                                            text = arguments[6];
                                        }

                                        int n = routeData.Blocks[blockIndex].PointsOfInterest.Length;
                                        Array.Resize(ref routeData.Blocks[blockIndex].PointsOfInterest, n + 1);
                                        routeData.Blocks[blockIndex].PointsOfInterest[n] = new POI(routeData.TrackPosition, idx, text, new Vector2(x, y), Godot.Mathf.Deg2Rad(yaw), Godot.Mathf.Deg2Rad(pitch), Godot.Mathf.Deg2Rad(roll));
                                    }
                                }
                                break;
                            default:
                                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "The command " + command + " is not supported at line " + routeExpressions[j].Line.ToString(culture) + ", column " + routeExpressions[j].Column.ToString(culture) + " in file " + routeExpressions[j].File);
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
        // Array.Resize<Block>(ref routeData.Blocks, blocksUsed);
    }
    

    
    #endregion
    
}