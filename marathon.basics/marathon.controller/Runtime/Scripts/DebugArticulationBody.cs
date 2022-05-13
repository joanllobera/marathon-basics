using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugArticulationBody : MonoBehaviour
{
    public Vector3 jointAcceleration;
    public Vector3 jointForce;
    public Vector3 jointPosition;
    public Vector3 jointVelocity;
    public ArticulationJacobian jacobian;
	public List<int> dofStartIndices;
	public List<float> driveTargets;
	public List<float> driveTargetVelocities;
	public List<float> jointAccelerations;
	public List<float> jointForces;
	public List<float> jointPositions;
	public List<float> jointVelocities;
    ArticulationBody _articulationBody;
    // Start is called before the first frame update
    void Start()
    {
        _articulationBody = GetComponent<ArticulationBody>();
        foreach (var ab in this.GetComponentsInChildren<ArticulationBody>())
        {
            if (ab.GetComponent<DebugArticulationBody>() == null)
            {
                ab.gameObject.AddComponent<DebugArticulationBody>();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_articulationBody == null)
            return;
        if (_articulationBody.isRoot)
        {
            if (dofStartIndices == null)
            {
                dofStartIndices = Enumerable.Range(0,_articulationBody.dofCount).ToList();
                driveTargets = Enumerable.Range(0,_articulationBody.dofCount)
                    .Select(x=>0f).ToList();
                driveTargetVelocities = Enumerable.Range(0,_articulationBody.dofCount)
                    .Select(x=>0f).ToList();
                jointAccelerations = Enumerable.Range(0,_articulationBody.dofCount)
                    .Select(x=>0f).ToList();
                jointForces = Enumerable.Range(0,_articulationBody.dofCount)
                    .Select(x=>0f).ToList();
                jointPositions = Enumerable.Range(0,_articulationBody.dofCount)
                    .Select(x=>0f).ToList();
                jointVelocities = Enumerable.Range(0,_articulationBody.dofCount)
                    .Select(x=>0f).ToList();
            }
            // _articulationBody.GetDenseJacobian(ref jacobian);
            _articulationBody.GetDofStartIndices(dofStartIndices);
            _articulationBody.GetDriveTargets(driveTargets);
            _articulationBody.GetDriveTargetVelocities(driveTargetVelocities);
            _articulationBody.GetJointAccelerations(jointAccelerations);
            _articulationBody.GetJointForces(jointForces);
            _articulationBody.GetJointPositions(jointPositions);
            _articulationBody.GetJointVelocities(jointVelocities);
        }
        if (_articulationBody.dofCount > 0)
        {
            jointAcceleration.x = _articulationBody.jointAcceleration[0]; 
            jointForce.x = _articulationBody.jointForce[0]; 
            jointPosition.x = _articulationBody.jointPosition[0]; 
            jointVelocity.x = _articulationBody.jointVelocity[0]; 
        }
        if (_articulationBody.dofCount > 1)
        {
            jointAcceleration.y = _articulationBody.jointAcceleration[1]; 
            jointForce.y = _articulationBody.jointForce[1]; 
            jointPosition.y = _articulationBody.jointPosition[1]; 
            jointVelocity.y = _articulationBody.jointVelocity[1]; 
        }        
        if (_articulationBody.dofCount > 2)
        {
            jointAcceleration.z = _articulationBody.jointAcceleration[2]; 
            jointForce.z = _articulationBody.jointForce[2]; 
            jointPosition.z = _articulationBody.jointPosition[2]; 
            jointVelocity.z = _articulationBody.jointVelocity[2]; 
        }        
    }
}
