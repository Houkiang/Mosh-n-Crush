using Unity.Netcode;
using UnityEngine;

public class B2NetworkPlayer : NetworkBehaviour
{
    [Header("B2 仅用于验证生成与 Owner")]
    [SerializeField] private Vector3 spawnOrigin = new Vector3(0f, 1f, 0f);
    [SerializeField] private float spawnSpacing = 3f;

    public void SetSpawnRule(Vector3 origin, float spacing)
    {
        spawnOrigin = origin;
        spawnSpacing = Mathf.Max(0.5f, spacing);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = ComputeSpawnPosition(OwnerClientId);
        }

        Debug.Log(
            $"[B2] NetworkPlayer spawned => netId={NetworkObjectId}, owner={OwnerClientId}, local={NetworkManager.LocalClientId}, isOwner={IsOwner}, isServer={IsServer}, isClient={IsClient}, pos={transform.position}");
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[B2] NetworkPlayer despawned => netId={NetworkObjectId}, owner={OwnerClientId}");
    }

    private Vector3 ComputeSpawnPosition(ulong ownerClientId)
    {
        int index = (int)(ownerClientId % 20UL);
        int row = index / 5;
        int col = index % 5;
        return spawnOrigin + new Vector3(col * spawnSpacing, 0f, row * spawnSpacing);
    }
}
