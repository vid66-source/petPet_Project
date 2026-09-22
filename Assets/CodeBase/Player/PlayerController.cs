using CodeBase.Infrastructure.Input;
using UnityEngine;

namespace CodeBase.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _jumpHeight;
        private const float GroundedVerticalSpeed = -2f;
        private IInputService _inputService;

        private float _verticalSpeed;
        private Vector3 _moveDirection = Vector3.zero;

        public void Construct(IInputService inputService)
        {
            _inputService = inputService;
            _inputService.OnJumpPressed += Jump;
        }

        private void Update()
        {
            if (_characterController.isGrounded && _verticalSpeed < 0)
                _verticalSpeed = GroundedVerticalSpeed;

            if (_characterController.isGrounded)
                _moveDirection = MoveDirectionConverter(_inputService.GetDirection());

            Vector3 verticalOffset = VerticalMovement();

            Vector3 horizontalOffset = HorizontalMovement(_moveDirection);

            _characterController.Move(horizontalOffset + verticalOffset);
        }

        private Vector3 MoveDirectionConverter(Vector2 dir)
        {
            Vector3 convertedDir = new Vector3(dir.x, 0, dir.y);
            return convertedDir;
        }

        private Vector3 HorizontalMovement(Vector3 vector3Dir)
        {
            Vector3 moveOffset = vector3Dir * (_moveSpeed * Time.deltaTime);
            return moveOffset;
        }

        private Vector3 VerticalMovement()
        {
            _verticalSpeed += Physics.gravity.y * Time.deltaTime;
            Vector3 moveOffset = Vector3.up * (_verticalSpeed * Time.deltaTime);
            return moveOffset;
        }

        private void Jump()
        {
            if (_characterController.isGrounded)
            {
                _verticalSpeed = Mathf.Sqrt(_jumpHeight * -2 * Physics.gravity.y);
            }
        }

        private void OnDestroy()
        {
            if (_inputService != null)
                _inputService.OnJumpPressed -= Jump;
        }
    }
}
