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
    public override void CreateObject(Vector3 Position, Transform WorldTransformation, Transform LocalTransformation,
                int SectionIndex, double StartingDistance, double EndingDistance,
                double TrackPosition, double Brightness, bool DuplicateMaterials = false)
    {
        // bool[] free = new bool[Objects.Length];
        // bool anyfree = false;
        // bool allfree = true;
        // for (int i = 0; i < Objects.Length; i++)
        // {
        //     free[i] = Objects[i].IsFreeOfFunctions();
        //     if (free[i])
        //     {
        //         anyfree = true;
        //     }
        //     else
        //     {
        //         allfree = false;
        //     }
        // }
        // if (anyfree && !allfree && Objects.Length > 1)
        // {
        //     //Optimise a little: If *all* are free of functions, this can safely be converted into a static object without regard to below
        //     if (LocalTransformation.X != Vector3.Right || LocalTransformation.Y != Vector3.Down || LocalTransformation.Z != Vector3.Forward)
        //     {
        //         //HACK:
        //         //An animated object containing a mix of functions and non-functions and using yaw, pitch or roll must not be converted into a mix
        //         //of animated and static objects, as this causes rounding differences....
        //         anyfree = false;
        //     }
        // }
        // if (anyfree)
        // {
        //     for (int i = 0; i < Objects.Length; i++)
        //     {
        //         if (Objects[i].States.Length != 0)
        //         {
        //             if (free[i])
        //             {
        //                 Matrix4D transformationMatrix = (Matrix4D)new Transformation(LocalTransformation, WorldTransformation);
        //                 Matrix4D mat = Matrix4D.Identity;
        //                 mat *= Objects[i].States[0].Translation;
        //                 mat *= transformationMatrix;
        //                 double zOffset = Objects[i].States[0].Translation.ExtractTranslation().Z * -1.0; //To calculate the Z-offset within the object, we want the untransformed co-ordinates, not the world co-ordinates

        //                 currentHost.CreateStaticObject(Objects[i].States[0].Prototype, LocalTransformation, mat, Matrix4D.CreateTranslation(Position.X, Position.Y, -Position.Z), zOffset, StartingDistance, EndingDistance, TrackPosition, Brightness);
        //             }
        //             else
        //             {
        //                 Objects[i].CreateObject(Position, WorldTransformation, LocalTransformation, SectionIndex, TrackPosition, Brightness);
        //             }
        //         }
        //     }
        // }
        // else
        // {
        //     for (int i = 0; i < Objects.Length; i++)
        //     {
        //         if (Objects[i].States.Length != 0)
        //         {
        //             Objects[i].CreateObject(Position, WorldTransformation, LocalTransformation, SectionIndex, TrackPosition, Brightness);
        //         }
        //     }
        // }
        // if (this.Sounds == null)
        // {
        //     return;
        // }
        // for (int i = 0; i < Sounds.Length; i++)
        // {
        //     if (this.Sounds[i] == null)
        //     {
        //         continue;
        //     }
        //     (Sounds[i] as WorldSound)?.CreateSound(Position + Sounds[i].Position, WorldTransformation, LocalTransformation, SectionIndex, TrackPosition);
        //     (Sounds[i] as AnimatedWorldObjectStateSound)?.Create(Position, WorldTransformation, LocalTransformation, SectionIndex, TrackPosition, Brightness);
        // }
    }

    internal AnimatedObject[] Objects;

    // todo
    public override UnifiedObject Clone() { return null; }

    // todo
    public override UnifiedObject Mirror() { return null; }

    // public void Reverse()
    // {
    // }

    /// <inheritdoc/>
    public override UnifiedObject Transform(double NearDistance, double FarDistance)
    {
        return null;
    }

    public override void OptimizeObject(bool PreserveVerticies, int Threshold, bool VertexCulling)
    {
        // for (int i = 0; i < Objects.Length; i++)
        // {
        //     for (int j = 0; j < Objects[i].States.Length; j++)
        //     {
        //         if (Objects[i].States[j].Prototype == null)
        //         {
        //             continue;
        //         }
        //         Objects[i].States[j].Prototype.OptimizeObject(PreserveVerticies, Threshold, VertexCulling);
        //     }
        // }
    }
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