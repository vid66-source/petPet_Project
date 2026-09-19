using System;
using UnityEngine;

namespace CodeBase.Infrastructure.Input
{
    public class InputService : IInputService
    {
        private readonly InputActions _inputActions;

        public event Action OnJumpPressed;

        public InputService()
        {
            _inputActions = new InputActions();
            _inputActions.Player.Enable();
            _inputActions.Player.Jump.performed += ctx => OnJumpPressed?.Invoke();
        }

        public Vector2 GetDirection()
        {
            Vector2 moveDirection = _inputActions.Player.Move.ReadValue<Vector2>();
            return moveDirection;
        }
    }
}