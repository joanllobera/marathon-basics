using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class RenderingOptions : MonoBehaviour
{

    [SerializeField]
    bool renderOnlyTarget;

    public
    GameObject movementsource;
    
    public
    GameObject ragdollcontroller;

    SkinnedMeshRenderer[] SkinnedRenderers;
    MeshRenderer[] MeshRenderers;
    MeshRenderer[] MeshRenderersRagdoll;

    bool currentRenderingState = false;

    // Start is called before the first frame update
    void Start()
    {
        SkinnedRenderers = movementsource.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        MeshRenderers = movementsource.GetComponentsInChildren<MeshRenderer>(true);
        MeshRenderersRagdoll = ragdollcontroller.GetComponentsInChildren<MeshRenderer>(true);
    }

    // Update is called once per frame
    void Update()
    {
        bool isTrainingMode = Academy.Instance.IsCommunicatorOn;
        bool onlyTarget = renderOnlyTarget || isTrainingMode;
        if (onlyTarget != currentRenderingState)
        {
            currentRenderingState = onlyTarget;
            if (onlyTarget)
            {
                foreach (SkinnedMeshRenderer r in SkinnedRenderers)
                    r.enabled = false;

                foreach (MeshRenderer r in MeshRenderers)
                    r.enabled = false;

                foreach (MeshRenderer r in MeshRenderersRagdoll)
                    r.enabled = false;

            }
            else {

                foreach (SkinnedMeshRenderer r in SkinnedRenderers)
                    r.enabled = true;

                foreach (MeshRenderer r in MeshRenderers)
                    r.enabled = true;

                foreach (MeshRenderer r in MeshRenderersRagdoll)
                    r.enabled = true;


            }

        }
    }
}
