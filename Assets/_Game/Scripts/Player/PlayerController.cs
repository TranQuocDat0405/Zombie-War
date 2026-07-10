using UnityEngine;

namespace ZombieWar.Player
{
    /// <summary>
    /// Moves the soldier with the virtual joystick. Rotation prefers the
    /// auto-aim target; falls back to movement direction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Joystick joystick;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private Animator animator;

        private CharacterController controller;
        private AutoAim autoAim;
        private float verticalVelocity;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        public Vector3 MoveDirection { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            autoAim = GetComponent<AutoAim>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            Vector2 input = joystick != null
                ? new Vector2(joystick.Horizontal, joystick.Vertical)
                : Vector2.zero;

#if UNITY_EDITOR
            // Keyboard fallback for quick editor testing.
            if (input == Vector2.zero)
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
#endif
            if (input.sqrMagnitude > 1f) input.Normalize();

            MoveDirection = new Vector3(input.x, 0f, input.y);

            // Gravity so the controller sticks to slopes.
            if (controller.isGrounded) verticalVelocity = -2f;
            else verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 motion = MoveDirection * moveSpeed + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            HandleRotation();

            if (animator != null)
            {
                animator.SetFloat(SpeedHash, input.magnitude, 0.08f, Time.deltaTime);
            }
        }

        private void HandleRotation()
        {
            Vector3 lookDir = Vector3.zero;

            if (autoAim != null && autoAim.CurrentTarget != null)
            {
                lookDir = autoAim.CurrentTarget.position - transform.position;
            }
            else if (MoveDirection.sqrMagnitude > 0.001f)
            {
                lookDir = MoveDirection;
            }

            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 0.001f) return;

            Quaternion target = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}
