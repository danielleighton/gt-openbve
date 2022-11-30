using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public enum MessageType
{
    Error,
    Warning,
    Info
}

public class Host
{
    /// <summary>
    /// Dictionary of StaticObject with Path and PreserveVertices as keys.
    /// </summary>
    public readonly Dictionary<ValueTuple<string, bool>, StaticObject> StaticObjectCache;

    public Host()
    {
        // Application = host;
        StaticObjectCache = new Dictionary<ValueTuple<string, bool>, StaticObject>();
        // AnimatedObjectCollectionCache = new Dictionary<string, AnimatedObjectCollection>();
        // MissingFiles = new List<string>();
    }

    public void AddMessage(MessageType type, bool fileNotFound, string text)
    {
        GD.Print(text);
    }

    public bool LoadObject(string path, System.Text.Encoding encoding, out UnifiedObject obj)
    {
        ValueTuple<string, bool> key = ValueTuple.Create(path, false);

        if (StaticObjectCache.ContainsKey(key))
        {
            obj = StaticObjectCache[key].Clone();
            
            return true;
        }

        // if (AnimatedObjectCollectionCache.ContainsKey(Path))
        // {
        //     Object = AnimatedObjectCollectionCache[Path].Clone();
        //     return true;
        // }

        obj = null;
        return false;
    }






}

public static class Plugin
{
    public static Host CurrentHost = new Host();
}