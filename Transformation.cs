// using System;
// using Godot;

// // transformation
// public struct Transformation
// {
//     public Vector3 X;
//     public Vector3 Y;
//     public Vector3 Z;

//     public Transformation(double yaw, double pitch, double roll)
//     {
//         if (yaw == 0.0 & pitch == 0.0 & roll == 0.0)
//         {
//             this.X = new Vector3(1.0f, 0.0f, 0.0f);
//             this.Y = new Vector3(0.0f, 1.0f, 0.0f);
//             this.Z = new Vector3(0.0f, 0.0f, 1.0f);
//         }
//         else if (pitch == 0.0 & roll == 0.0)
//         {
//             double cosYaw = Math.Cos(yaw);
//             double sinYaw = Math.Sin(yaw);
//             this.X = new Vector3((float)cosYaw, 0.0f, (float)-sinYaw);
//             this.Y = new Vector3(0.0f, 1.0f, 0.0f);
//             this.Z = new Vector3((float)sinYaw, 0.0f, (float)cosYaw);
//         }
//         else
//         {
//             double sx = 1.0, sy = 0.0, sz = 0.0;
//             double ux = 0.0, uy = 1.0, uz = 0.0;
//             double dx = 0.0, dy = 0.0, dz = 1.0;
//             double cosYaw = Math.Cos(yaw);
//             double sinYaw = Math.Sin(yaw);
//             double cosPitch = Math.Cos(-pitch);
//             double sinPitch = Math.Sin(-pitch);
//             double cosRoll = Math.Cos(-roll);
//             double sinRoll = Math.Sin(-roll);
//             Calc.Rotate(ref sx, ref sy, ref sz, ux, uy, uz, cosYaw, sinYaw);
//             Calc.Rotate(ref dx, ref dy, ref dz, ux, uy, uz, cosYaw, sinYaw);
//             Calc.Rotate(ref ux, ref uy, ref uz, sx, sy, sz, cosPitch, sinPitch);
//             Calc.Rotate(ref dx, ref dy, ref dz, sx, sy, sz, cosPitch, sinPitch);
//             Calc.Rotate(ref sx, ref sy, ref sz, dx, dy, dz, cosRoll, sinRoll);
//             Calc.Rotate(ref ux, ref uy, ref uz, dx, dy, dz, cosRoll, sinRoll);
//             this.X = new Vector3((float)sx, (float)sy, (float)sz);
//             this.Y = new Vector3((float)ux, (float)uy, (float)uz);
//             this.Z = new Vector3((float)dx, (float)dy, (float)dz);
//         }
//     }
//     public Transformation(Transformation Transformation, double Yaw, double Pitch, double Roll)
//     {
//         double sx = Transformation.X.x, sy = Transformation.X.y, sz = Transformation.X.z;
//         double ux = Transformation.Y.x, uy = Transformation.Y.y, uz = Transformation.Y.z;
//         double dx = Transformation.Z.x, dy = Transformation.Z.y, dz = Transformation.Z.z;
//         double cosYaw = Math.Cos(Yaw);
//         double sinYaw = Math.Sin(Yaw);
//         double cosPitch = Math.Cos(-Pitch);
//         double sinPitch = Math.Sin(-Pitch);
//         double cosRoll = Math.Cos(Roll);
//         double sinRoll = Math.Sin(Roll);
//         Calc.Rotate(ref sx, ref sy, ref sz, ux, uy, uz, cosYaw, sinYaw);
//         Calc.Rotate(ref dx, ref dy, ref dz, ux, uy, uz, cosYaw, sinYaw);
//         Calc.Rotate(ref ux, ref uy, ref uz, sx, sy, sz, cosPitch, sinPitch);
//         Calc.Rotate(ref dx, ref dy, ref dz, sx, sy, sz, cosPitch, sinPitch);
//         Calc.Rotate(ref sx, ref sy, ref sz, dx, dy, dz, cosRoll, sinRoll);
//         Calc.Rotate(ref ux, ref uy, ref uz, dx, dy, dz, cosRoll, sinRoll);
//         this.X = new Vector3((float)sx, (float)sy, (float)sz);
//         this.Y = new Vector3((float)ux, (float)uy, (float)uz);
//         this.Z = new Vector3((float)dx, (float)dy, (float)dz);
//     }

//     //public Transformation(Transformation BaseTransformation, Transformation AuxTransformation)
//     //{
//     //    Vector3 x = BaseTransformation.X;
//     //    Vector3 y = BaseTransformation.Y;
//     //    Vector3 z = BaseTransformation.Z;
//     //    Vector3 s = AuxTransformation.X;
//     //    Vector3 u = AuxTransformation.Y;
//     //    Vector3 d = AuxTransformation.Z;
//     //    Calc.Rotate(ref x.x, ref x.y, ref x.z, d.x, d.y, d.z, u.x, u.y, u.z, s.x, s.y, s.z);
//     //    Calc.Rotate(ref y.x, ref y.y, ref y.z, d.x, d.y, d.z, u.x, u.y, u.z, s.x, s.y, s.z);
//     //    Calc.Rotate(ref z.x, ref z.y, ref z.z, d.x, d.y, d.z, u.x, u.y, u.z, s.x, s.y, s.z);
//     //    this.X = x;
//     //    this.Y = y;
//     //    this.Z = z;
//     //}

// }