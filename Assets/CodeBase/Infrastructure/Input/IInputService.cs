using System;
using CodeBase.Infrastructure.Services;
using UnityEngine;

namespace CodeBase.Infrastructure.Input
{
    public interface IInputService : IService
    {
        Vector2 GetDirection();

        event Action OnJumpPressed;
    }
}