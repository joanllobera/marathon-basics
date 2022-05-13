using UnityEngine;

namespace Unity.MLAgents 
{
    public interface IOnTerrainCollision
    {
        void OnTerrainCollision(GameObject other, GameObject terrain);
    }
}