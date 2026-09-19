using System;
using CodeBase.Infrastructure.Services;
using UnityEngine;

namespace CodeBase.Infrastructure.Input
{
    public interface IInputService : IService
    {
        event Action OnJumpPressed;

        Vector2 GetDirection();
    }
}