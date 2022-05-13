using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class AnimationMimic : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    Transform animatedHierarchyRoot;

    [SerializeField]
    Transform sourceHierarchyRoot;

    IReadOnlyList<Tuple<Transform, Transform>> pairedTransforms; // Source, Animated


    void Awake()
    {
        var candidateSources = sourceHierarchyRoot.GetComponentsInChildren<Transform>().ToList();
        pairedTransforms = animatedHierarchyRoot.GetComponentsInChildren<Transform>().Select(at => Tuple.Create(candidateSources.FirstOrDefault(ct => Utils.SegmentName(ct.name) == Utils.SegmentName(at.name)), at)).Where(tup => tup.Item1 != null).ToList();
    }

    void LateUpdate()
    {
        pairedTransforms[0].Item2.position = pairedTransforms[0].Item1.position;

        foreach((var source, var target) in pairedTransforms)
        {
            target.rotation = source.rotation;
        }
    }
}
