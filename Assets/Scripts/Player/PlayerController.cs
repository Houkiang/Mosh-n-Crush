using PinePie.SimpleJoystick;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotateSpeed = 15f;

    [Header("组件引用")]
    [SerializeField] private Animator _animator; 



    [Header("跳跃设置")]
    [SerializeField] private float jumpForce = 0;
    [SerializeField] private LayerMask groundLayer; // 地面所在的层级
    [SerializeField] private float groundCheckRadius = 0.2f; // 地面检测半径
    [SerializeField] private Transform groundCheckPoint; // 脚底的一个空物体

    [SerializeField] private JoystickController joystick;
    // 内部变量

    // 缓存动画参数ID
    private int _animIDMoving;

    private Rigidbody _rb;
    private Vector3 _inputVector;
    [SerializeField] private bool _isGrounded;
    private bool _jumpInput;
    private Vector3 _groundNormal; 
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animIDMoving = Animator.StringToHash("IsMoving");
        _rb.freezeRotation = true; 
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }


    private void Update()
    {
        HandleInput();
        CheckGround();
    }

 
    private void FixedUpdate()
    {
        ApplyMovement();
        HandleRotation();
        UpdateAnimation();

    }

    private void UpdateAnimation()
    {
        if (_animator == null) return;

        // 只要输入向量长度大于 0.1，就视为正在移动
        bool isMoving = _inputVector.sqrMagnitude > 0.01f;

        // 设置布尔值
        _animator.SetBool(_animIDMoving, isMoving);
    }
    private void HandleInput()
    {
        float x = joystick.InputDirection.x;
        float z = joystick.InputDirection.y;
        _inputVector = new Vector3(x, 0, z).normalized;


    }

    private void HandleRotation()
    {
        if (_inputVector.magnitude > 0.1f)
        {
            // 计算目标角度
            Quaternion targetRotation = Quaternion.LookRotation(_inputVector);
            // 平滑旋转
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        // 修改速度
        Vector3 targetVelocity = _inputVector * moveSpeed;
        
        _rb.velocity = new Vector3(targetVelocity.x, _rb.velocity.y, targetVelocity.z);
    }

    private void CheckGround()
    {
        // 使用球形检测判断是否在地面
        Vector3 checkPos = groundCheckPoint ? groundCheckPoint.position : transform.position + Vector3.up * 0.1f;
        
        
        _isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundLayer);
    }

    // 辅助线
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
