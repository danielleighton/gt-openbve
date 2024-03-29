using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

internal class WallDike
{
    /// <summary>Whether the wall/ dike is shown for this block</summary>
    internal bool Exists;
    /// <summary>The routefile index of the object</summary>
    internal readonly int Type;
    /// <summary>The direction the object(s) are placed in: -1 for left, 0 for both, 1 for right</summary>
    internal readonly CsvRwRouteParser.Direction Direction;
    /// <summary>Reference to the appropriate left-sided object array</summary>
    internal readonly ObjectDictionary leftObjects;
    /// <summary>Reference to the appropriate right-sided object array</summary>
    internal readonly ObjectDictionary rightObjects;

    internal WallDike(int type, CsvRwRouteParser.Direction direction, ObjectDictionary LeftObjects, ObjectDictionary RightObjects, bool exists = true)
    {
        Exists = exists;
        Type = type;
        Direction = direction;
        leftObjects = LeftObjects;
        rightObjects = RightObjects;
    }

    internal WallDike Clone()
    {
        WallDike w = new WallDike(Type, Direction, leftObjects, rightObjects, Exists);
        return w;
    }

    internal void Create(Vector3 pos, Transform RailTransformation, double StartingDistance, double EndingDistance)
    {
        if (!Exists)
        {
            return;
        }
        if (Direction <= 0)
        {
            if (leftObjects.ContainsKey(Type))
            {
                leftObjects[Type].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
            }

        }
        if (Direction >= 0)
        {
            if (rightObjects.ContainsKey(Type))
            {
                rightObjects[Type].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
            }

        }
    }
}
