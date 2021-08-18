using Godot;
using System;
using System.Globalization;

internal class Form
{
    internal Form(int primaryRail, int secondaryRail, int formType, int roofType, StructureData structure)
    {
        PrimaryRail = primaryRail;
        SecondaryRail = secondaryRail;
        FormType = formType;
        RoofType = roofType;
        Structure = structure;
    }
    /// <summary>The platform face rail</summary>
    internal readonly int PrimaryRail;
    /// <summary>The rail for the rear transformation</summary>
    internal readonly int SecondaryRail;
    /// <summary>The index of the FormType to use</summary>
    internal readonly int FormType;
    /// <summary>The index of the RoofType to use</summary>
    internal readonly int RoofType;
    /*
     * Magic number constants used by BVE2 /4
     */
    internal const int SecondaryRailStub = 0;
    internal const int SecondaryRailL = -1;
    internal const int SecondaryRailR = -2;

    internal readonly StructureData Structure;
    private readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    internal void CreatePrimaryRail(Node parentNode, Block currentBlock, Block nextBlock, Vector3 pos, Transform RailTransformation, float StartingDistance, float EndingDistance, string FileName)
    {

        if (SecondaryRail == Form.SecondaryRailStub)
        {
            if (!Structure.FormL.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                Structure.FormL[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                if (RoofType > 0)
                {
                    if (!Structure.RoofL.ContainsKey(RoofType))
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                    }
                    else
                    {
                        Structure.RoofL[RoofType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                    }
                }
            }
        }
        else if (SecondaryRail == Form.SecondaryRailL)
        {
            if (!Structure.FormL.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                Structure.FormL[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
            }

            if (!Structure.FormCL.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormCL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                UnifiedObject.CreateStaticObject(parentNode,(StaticObject)Structure.FormCL[FormType], pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0);
            }

            if (RoofType > 0)
            {
                if (!Structure.RoofL.ContainsKey(RoofType))
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                }
                else
                {
                    Structure.RoofL[RoofType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                }

                if (!Structure.RoofCL.ContainsKey(RoofType))
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofCL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                }
                else
                {
                    UnifiedObject.CreateStaticObject(parentNode,(StaticObject)Structure.RoofCL[RoofType], pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
                }
            }
        }
        else if (SecondaryRail == Form.SecondaryRailR)
        {
            if (!Structure.FormR.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                Structure.FormR[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
            }

            if (!Structure.FormCR.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormCR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                UnifiedObject.CreateStaticObject(parentNode,(StaticObject)Structure.FormCR[FormType], pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
            }

            if (RoofType > 0)
            {
                if (!Structure.RoofR.ContainsKey(RoofType))
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                }
                else
                {
                    Structure.RoofR[RoofType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                }

                if (!Structure.RoofCR.ContainsKey(RoofType))
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofCR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                }
                else
                {
                    UnifiedObject.CreateStaticObject(parentNode,(StaticObject)Structure.RoofCR[RoofType], pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
                }
            }
        }
        else if (SecondaryRail > 0)
        {
            int p = PrimaryRail;
            double px0 = p > 0 ? currentBlock.Rails[p].RailStart.x : 0.0f;
            double px1 = p > 0 ? nextBlock.Rails[p].RailEnd.x : 0.0f;
            int s = SecondaryRail;
            if (s < 0 || !currentBlock.Rails.ContainsKey(s) || !currentBlock.Rails[s].RailStarted)
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RailIndex2 is out of range in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName);
            }
            else
            {
                double d0 = currentBlock.Rails[s].RailStart.x - px0;
                double d1 = nextBlock.Rails[s].RailEnd.x - px1;
                if (d0 < 0.0)
                {
                    if (!Structure.FormL.ContainsKey(FormType))
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                    }
                    else
                    {
                        Structure.FormL[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                    }

                    if (!Structure.FormCL.ContainsKey(FormType))
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormCL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                    }
                    else
                    {
                        StaticObject FormC = (StaticObject)Structure.FormCL[FormType].Transform(d0, d1);
                        UnifiedObject.CreateStaticObject(parentNode,FormC, pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
                    }

                    if (RoofType > 0)
                    {
                        if (!Structure.RoofL.ContainsKey(RoofType))
                        {
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                        }
                        else
                        {
                            Structure.RoofL[RoofType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                        }

                        if (!Structure.RoofCL.ContainsKey(RoofType))
                        {
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofCL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                        }
                        else
                        {
                            StaticObject RoofC = (StaticObject)Structure.RoofCL[RoofType].Transform(d0, d1);
                            UnifiedObject.CreateStaticObject(parentNode,RoofC, pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
                        }
                    }
                }
                else if (d0 > 0.0)
                {
                    if (!Structure.FormR.ContainsKey(FormType))
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                    }
                    else
                    {
                        Structure.FormR[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                    }

                    if (!Structure.FormCR.ContainsKey(FormType))
                    {
                        Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormCR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                    }
                    else
                    {
                        StaticObject FormC = (StaticObject)Structure.FormCR[FormType].Transform(d0, d1);
                        UnifiedObject.CreateStaticObject(parentNode,FormC, pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
                    }

                    if (RoofType > 0)
                    {
                        if (!Structure.RoofR.ContainsKey(RoofType))
                        {
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                        }
                        else
                        {
                            Structure.RoofR[RoofType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                        }

                        if (!Structure.RoofCR.ContainsKey(RoofType))
                        {
                            Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofCR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                        }
                        else
                        {
                            StaticObject RoofC = (StaticObject)Structure.RoofCR[RoofType].Transform(d0, d1);
                            UnifiedObject.CreateStaticObject(parentNode,RoofC, pos, RailTransformation, Transform.Identity, 0.0f, StartingDistance, EndingDistance, StartingDistance, 1.0f);
                        }
                    }
                }
            }
        }

    }

    internal void CreateSecondaryRail(Block currentBlock, Vector3 pos, Transform RailTransformation, double StartingDistance, double EndingDistance, string FileName)
    {
        double px = 0.0;
        if (currentBlock.Rails.ContainsKey(PrimaryRail))
        {
            px = PrimaryRail > 0 ? currentBlock.Rails[PrimaryRail].RailStart.x : 0.0;
        }
        double d = px - currentBlock.Rails[SecondaryRail].RailStart.x;
        if (d < 0.0)
        {
            if (!Structure.FormL.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                Structure.FormL[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
            }

            if (RoofType > 0)
            {
                if (!Structure.RoofL.ContainsKey(RoofType))
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofL not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                }
                else
                {
                    Structure.RoofL[RoofType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
                }
            }
        }
        else
        {
            if (!Structure.FormR.ContainsKey(FormType))
            {
                Plugin.CurrentHost.AddMessage(MessageType.Error, false, "FormStructureIndex references a FormR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
            }
            else
            {
                Structure.FormR[FormType].CreateObject(pos, RailTransformation, StartingDistance, EndingDistance, StartingDistance);
            }

            if (RoofType > 0)
            {
                if (!Structure.RoofR.ContainsKey(RoofType))
                {
                    Plugin.CurrentHost.AddMessage(MessageType.Error, false, "RoofStructureIndex references a RoofR not loaded in Track.Form at track position " + StartingDistance.ToString(Culture) + " in file " + FileName + ".");
                }
                else
                {
                    Structure.RoofR[RoofType].CreateObject(pos, RailTransformation, Transform.Identity, StartingDistance, EndingDistance, StartingDistance);
                }
            }
        }
    }
}