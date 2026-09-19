using CodeBase.Infrastructure.Input;
using UnityEngine;

namespace CodeBase.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _moveSpeed;
        private const float GroundedVerticalSpeed = -2f;
        private IInputService _inputService;

        private float _verticalSpeed;

        public void Construct(IInputService inputService)
        {
            _inputService = inputService;
        }

        private void Update()
        {
            Vector3 horizontalOffset = HorizontalMovement(_inputService.GetDirection());

            if (_characterController.isGrounded && _verticalSpeed < 0)
                _verticalSpeed = GroundedVerticalSpeed;

            Debug.Log($"Speed: {_verticalSpeed} and Player isGrounded flag: {_characterController.isGrounded}");

            Vector3 verticalOffset = VerticalMovement();

            _characterController.Move(horizontalOffset +  verticalOffset);
        }

        private Vector3 HorizontalMovement(Vector2 dir)
        {
            Vector3 moveDirection = new Vector3(dir.x, 0, dir.y);
            Vector3 moveOffset = moveDirection * (_moveSpeed * Time.deltaTime);
            return moveOffset;
        }

        private Vector3 VerticalMovement()
        {
            _verticalSpeed += Physics.gravity.y * Time.deltaTime;
            Vector3 moveOffset = Vector3.up * (_verticalSpeed * Time.deltaTime);
            return moveOffset;
        }
    }
}
