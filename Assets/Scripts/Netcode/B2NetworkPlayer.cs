using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class B2NetworkPlayer : NetworkBehaviour
{
    [Header("B2 仅用于验证生成与 Owner")]
    [SerializeField] private Vector3 spawnOrigin = new Vector3(0f, 1f, 0f);
    [SerializeField] private float spawnSpacing = 3f;

    [Header("B3 输入所有权原型")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotateSpeed = 12f;

    private bool ownerInputEnabled;

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

        ownerInputEnabled = IsOwner;
        Debug.Log($"[B3] Input ownership => owner={OwnerClientId}, local={NetworkManager.LocalClientId}, inputEnabled={ownerInputEnabled}");

        Debug.Log(
            $"[B2] NetworkPlayer spawned => netId={NetworkObjectId}, owner={OwnerClientId}, local={NetworkManager.LocalClientId}, isOwner={IsOwner}, isServer={IsServer}, isClient={IsClient}, pos={transform.position}");
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"[B2] NetworkPlayer despawned => netId={NetworkObjectId}, owner={OwnerClientId}");
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !ownerInputEnabled) return;

        Vector2 input = ReadMoveInput();
        SubmitMoveInputServerRpc(input, Time.fixedDeltaTime);
    }

    private Vector3 ComputeSpawnPosition(ulong ownerClientId)
    {
        int index = (int)(ownerClientId % 20UL);
        int row = index / 5;
        int col = index % 5;
        return spawnOrigin + new Vector3(col * spawnSpacing, 0f, row * spawnSpacing);
    }

    [ServerRpc]
    private void SubmitMoveInputServerRpc(Vector2 input, float deltaTime, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        Vector3 move = new Vector3(input.x, 0f, input.y);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        if (move.sqrMagnitude < 0.0001f) return;

        transform.position += move * moveSpeed * Mathf.Max(0f, deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Mathf.Max(0f, deltaTime));
    }

    private static Vector2 ReadMoveInput()
    {
        float x = 0f;
        float y = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        }

        if (Mathf.Approximately(x, 0f) && Mathf.Approximately(y, 0f))
        {
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
        }

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }
}
