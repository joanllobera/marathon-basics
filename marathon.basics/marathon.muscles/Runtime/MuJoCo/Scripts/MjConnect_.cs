using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System.Xml;
using System;

public class MjConnect_ : MjBaseConstraint
{
    public MjBaseBody Body1;
    public MjBaseBody Body2;
    public Vector3 Anchor;
    protected override string _constraintName => "connect";


    protected override void FromMjcf(XmlElement mjcf)
    {
        Body1 = mjcf.GetObjectReferenceAttribute<MjBaseBody>("body1");
        Body2 = mjcf.GetObjectReferenceAttribute<MjBaseBody>("body2");
        if (mjcf.GetStringAttribute("anchor") != null)
        {
            Debug.Log($"anchor in connect {name} ignored. Set Transforms in the editor.");
        }
    }

    // Generate implementation specific XML element.
    protected override void ToMjcf(XmlElement mjcf)
    {
        if (Body1 == null || Body2 == null)
        {
            throw new NullReferenceException($"Both bodies in connect {name} are required.");
        }
        mjcf.SetAttribute("body1", Body1.MujocoName);
        mjcf.SetAttribute("body2", Body2.MujocoName);
    }

    public void OnValidate()
    {
        if (Body1 != null && Body1 == Body2)
        {
            Debug.LogError("Body1 and Body2 can't be the same - resetting Body2.", this);
            Body2 = null;
        }
    }

    protected override unsafe void OnBindToRuntime(MujocoLib.mjModel_* model, MujocoLib.mjData_* data)
    {

        MjEngineTool.SetMjTransform(model->eq_data, Anchor,
                                    Quaternion.identity, MujocoId);
    }
}

