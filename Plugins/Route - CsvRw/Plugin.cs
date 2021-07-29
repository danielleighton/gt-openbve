using Godot;
using System;

public enum MessageType
{
    Error,
    Warning,
    Info
}

public class Host 
{
    public void AddMessage(MessageType type, bool fileNotFound, string text)
    {
        GD.Print(text);
    }
   		
}

public static class Plugin
{
    public static Host CurrentHost = new Host();
}