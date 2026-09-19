using CodeBase.Infrastructure.Input;
using UnityEngine;

namespace CodeBase.Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private float _moveSpeed;
        private IInputService _inputService;

        public void Construct(IInputService inputService)
        {
            _inputService = inputService;
        }

        private void Update()
        {
            Movement(_inputService.GetDirection());
        }

        private void Movement(Vector2 dir)
        {
            Vector3 moveDirection = new Vector3(dir.x, 0, dir.y);
            Vector3 moveOffset = moveDirection * (_moveSpeed * Time.deltaTime);
            _characterController.Move(moveOffset);
        }
    }
}
