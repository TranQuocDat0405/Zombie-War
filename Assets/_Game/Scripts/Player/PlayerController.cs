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
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 14f;
        [SerializeField] private Animator animator;

        private CharacterController controller;
        private AutoAim autoAim;
        private float verticalVelocity;
        private Joystick _joystick;

        /// <summary>
        /// The joystick now lives in the GamePlayMenu prefab (another scene's UI),
        /// so it can't be a serialized reference — resolve it lazily through
        /// UIManager. Null for the first frame or two before the HUD opens; the
        /// soldier just stands still, which is imperceptible.
        /// </summary>
        private Joystick joystick
        {
            get
            {
                if (_joystick == null && NFramework.UIManager.IsSingletonAlive)
                {
                    var hud = NFramework.UIManager.I.GetOpenedView<UI.GamePlayMenu>(Define.UIName.GAMEPLAY_MENU);
                    if (hud != null) _joystick = hud.Joystick;
                }
                return _joystick;
            }
        }

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");

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

                // Movement relative to facing: walking away from the aim target
                // plays the run cycle in reverse (backpedal) instead of moonwalking.
                Vector3 local = transform.InverseTransformDirection(MoveDirection);
                animator.SetFloat(MoveXHash, local.x, 0.1f, Time.deltaTime);
                animator.SetFloat(MoveYHash, local.z, 0.1f, Time.deltaTime);
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
