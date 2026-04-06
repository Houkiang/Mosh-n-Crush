using Unity.Netcode;
using Unity.Netcode.Components;
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

    [Header("B4 位姿同步与基础平滑")]
    [SerializeField] private float serverInputSmoothing = 14f;
    [SerializeField] private float positionThreshold = 0.015f;
    [SerializeField] private float rotationThreshold = 0.6f;
    [SerializeField] private bool enableSlerpPosition = true;

    private bool ownerInputEnabled;
    private NetworkTransform networkTransform;
    private Vector2 serverTargetInput;
    private Vector2 serverAppliedInput;

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
        networkTransform = GetComponent<NetworkTransform>();
        ConfigureNetworkTransform();
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
        if (!IsSpawned) return;

        if (ownerInputEnabled)
        {
            Vector2 input = ReadMoveInput();
            if (IsServer)
            {
                serverTargetInput = input;
            }
            else
            {
                SubmitMoveInputServerRpc(input);
            }
        }

        if (IsServer)
        {
            RunServerAuthoritativeMovement(Time.fixedDeltaTime);
        }
    }

    private Vector3 ComputeSpawnPosition(ulong ownerClientId)
    {
        int index = (int)(ownerClientId % 20UL);
        int row = index / 5;
        int col = index % 5;
        return spawnOrigin + new Vector3(col * spawnSpacing, 0f, row * spawnSpacing);
    }

    [ServerRpc]
    private void SubmitMoveInputServerRpc(Vector2 input, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        serverTargetInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void RunServerAuthoritativeMovement(float deltaTime)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(1f, serverInputSmoothing) * Mathf.Max(0f, deltaTime));
        serverAppliedInput = Vector2.Lerp(serverAppliedInput, serverTargetInput, t);
        serverAppliedInput = Vector2.ClampMagnitude(serverAppliedInput, 1f);

        Vector3 move = new Vector3(serverAppliedInput.x, 0f, serverAppliedInput.y);
        if (move.sqrMagnitude < 0.0001f) return;

        float dt = Mathf.Max(0f, deltaTime);
        transform.position += move * moveSpeed * dt;

        Quaternion targetRotation = Quaternion.LookRotation(move.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * dt);
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

    private void ConfigureNetworkTransform()
    {
        if (networkTransform == null) return;

        networkTransform.Interpolate = true;
        networkTransform.SlerpPosition = enableSlerpPosition;
        networkTransform.InLocalSpace = false;
        networkTransform.UseHalfFloatPrecision = false;
        networkTransform.PositionThreshold = Mathf.Max(0.001f, positionThreshold);
        networkTransform.RotAngleThreshold = Mathf.Max(0.01f, rotationThreshold);

        Debug.Log(
            $"[B4] Sync config => owner={OwnerClientId}, serverAuth={networkTransform.IsServerAuthoritative()}, interpolate={networkTransform.Interpolate}, slerpPos={networkTransform.SlerpPosition}, posThreshold={networkTransform.PositionThreshold}, rotThreshold={networkTransform.RotAngleThreshold}");
    }
}
