using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

internal struct AnimatedObjectState
{
    internal Vector3 Position;
    internal StaticObject Object;
}

internal class AnimatedObject
{
    internal AnimatedObjectState[] States;
    //internal FunctionScripts.FunctionScript StateFunction;
    internal int CurrentState;
    internal Vector3 TranslateXDirection;
    internal Vector3 TranslateYDirection;
    internal Vector3 TranslateZDirection;
    //internal FunctionScripts.FunctionScript TranslateXFunction;
    //internal FunctionScripts.FunctionScript TranslateYFunction;
    //internal FunctionScripts.FunctionScript TranslateZFunction;
    internal Vector3 RotateXDirection;
    internal Vector3 RotateYDirection;
    internal Vector3 RotateZDirection;
    //internal FunctionScripts.FunctionScript RotateXFunction;
    //internal FunctionScripts.FunctionScript RotateYFunction;
    //internal FunctionScripts.FunctionScript RotateZFunction;
    internal Damping RotateXDamping;
    internal Damping RotateYDamping;
    internal Damping RotateZDamping;
    internal Vector2 TextureShiftXDirection;
    internal Vector2 TextureShiftYDirection;
    //internal FunctionScripts.FunctionScript TextureShiftXFunction;
    //internal FunctionScripts.FunctionScript TextureShiftYFunction;
    internal bool LEDClockwiseWinding;
    internal double LEDInitialAngle;
    internal double LEDLastAngle;
    /// <summary>If LEDFunction is used, an array of five vectors representing the bottom-left, up-left, up-right, bottom-right and center coordinates of the LED square, or a null reference otherwise.</summary>
    internal Vector3[] LEDVectors;
    //internal FunctionScripts.FunctionScript LEDFunction;
    internal double RefreshRate;
    internal double CurrentTrackZOffset;
    internal double SecondsSinceLastUpdate;
    //internal int ObjectIndex;
    internal bool IsFreeOfFunctions()
    {
        //if (this.StateFunction != null) return false;
        //if (this.TranslateXFunction != null | this.TranslateYFunction != null | this.TranslateZFunction != null) return false;
        //if (this.RotateXFunction != null | this.RotateYFunction != null | this.RotateZFunction != null) return false;
        //if (this.TextureShiftXFunction != null | this.TextureShiftYFunction != null) return false;
        //if (this.LEDFunction != null) return false;
        return true;
    }

    internal AnimatedObject Clone()
    {
        AnimatedObject Result = new AnimatedObject();
        Result.States = new AnimatedObjectState[this.States.Length];
        for (int i = 0; i < this.States.Length; i++)
        {
            Result.States[i].Position = this.States[i].Position;
            //Result.States[i].Object = ObjectManager.Instance.CloneObject(this.States[i].Object);
        }
        //Result.StateFunction = this.StateFunction == null ? null : this.StateFunction.Clone();
        Result.CurrentState = this.CurrentState;
        Result.TranslateZDirection = this.TranslateZDirection;
        Result.TranslateYDirection = this.TranslateYDirection;
        Result.TranslateXDirection = this.TranslateXDirection;
        //Result.TranslateXFunction = this.TranslateXFunction == null ? null : this.TranslateXFunction.Clone();
        //Result.TranslateYFunction = this.TranslateYFunction == null ? null : this.TranslateYFunction.Clone();
        //Result.TranslateZFunction = this.TranslateZFunction == null ? null : this.TranslateZFunction.Clone();
        Result.RotateXDirection = this.RotateXDirection;
        Result.RotateYDirection = this.RotateYDirection;
        Result.RotateZDirection = this.RotateZDirection;
        //Result.RotateXFunction = this.RotateXFunction == null ? null : this.RotateXFunction.Clone();
        //Result.RotateXDamping = this.RotateXDamping == null ? null : this.RotateXDamping.Clone();
        //Result.RotateYFunction = this.RotateYFunction == null ? null : this.RotateYFunction.Clone();
        //Result.RotateYDamping = this.RotateYDamping == null ? null : this.RotateYDamping.Clone();
        //Result.RotateZFunction = this.RotateZFunction == null ? null : this.RotateZFunction.Clone();
        //Result.RotateZDamping = this.RotateZDamping == null ? null : this.RotateZDamping.Clone();
        Result.TextureShiftXDirection = this.TextureShiftXDirection;
        Result.TextureShiftYDirection = this.TextureShiftYDirection;
        //Result.TextureShiftXFunction = this.TextureShiftXFunction == null ? null : this.TextureShiftXFunction.Clone();
        //Result.TextureShiftYFunction = this.TextureShiftYFunction == null ? null : this.TextureShiftYFunction.Clone();
        Result.LEDClockwiseWinding = this.LEDClockwiseWinding;
        Result.LEDInitialAngle = this.LEDInitialAngle;
        Result.LEDLastAngle = this.LEDLastAngle;
        if (this.LEDVectors != null)
        {
            Result.LEDVectors = new Vector3[this.LEDVectors.Length];
            for (int i = 0; i < this.LEDVectors.Length; i++)
            {
                Result.LEDVectors[i] = this.LEDVectors[i];
            }
        }
        else
        {
            Result.LEDVectors = null;
        }
        //Result.LEDFunction = this.LEDFunction == null ? null : this.LEDFunction.Clone();
        Result.RefreshRate = this.RefreshRate;
        Result.CurrentTrackZOffset = 0.0;
        Result.SecondsSinceLastUpdate = 0.0;
        //Result.ObjectIndex = -1;
        return Result;
    }
}

internal class AnimatedObjectCollection : UnifiedObject
{
    internal AnimatedObject[] Objects;
}

// animated objects
internal class Damping
{
    internal double NaturalFrequency;
    internal double NaturalTime;
    internal double DampingRatio;
    internal double NaturalDampingFrequency;
    internal double OriginalAngle;
    internal double OriginalDerivative;
    internal double TargetAngle;
    internal double CurrentAngle;
    internal double CurrentValue;
    internal double CurrentTimeDelta;
    internal Damping(double NaturalFrequency, double DampingRatio)
    {
        if (NaturalFrequency < 0.0)
        {
            throw new ArgumentException("NaturalFrequency must be non-negative in the constructor of the Damping class.");
        }
        if (DampingRatio < 0.0)
        {
            throw new ArgumentException("DampingRatio must be non-negative in the constructor of the Damping class.");
        }
        this.NaturalFrequency = NaturalFrequency;
        this.NaturalTime = NaturalFrequency != 0.0 ? 1.0 / NaturalFrequency : 0.0;
        this.DampingRatio = DampingRatio;
        if (DampingRatio < 1.0)
        {
            this.NaturalDampingFrequency = NaturalFrequency * Math.Sqrt(1.0 - DampingRatio * DampingRatio);
        }
        else if (DampingRatio == 1.0)
        {
            this.NaturalDampingFrequency = NaturalFrequency;
        }
        else
        {
            this.NaturalDampingFrequency = NaturalFrequency * Math.Sqrt(DampingRatio * DampingRatio - 1.0);
        }
        this.OriginalAngle = 0.0;
        this.OriginalDerivative = 0.0;
        this.TargetAngle = 0.0;
        this.CurrentAngle = 0.0;
        this.CurrentValue = 1.0;
        this.CurrentTimeDelta = 0.0;
    }
    internal Damping Clone()
    {
        return (Damping)this.MemberwiseClone();
    }
}