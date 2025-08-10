using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PotSpawnPoint : MonoBehaviour
{
    [SerializeField] private PotSize potSize = PotSize.Medium;
    [SerializeField] private int unlockLevel = 0;

    public PotSize GetPotSize => potSize;
    public int GetUnlockLevel => unlockLevel;
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.5f);
        Gizmos.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, $"Size: {potSize}\nUnlock: {unlockLevel}");
    }
#endif
}
