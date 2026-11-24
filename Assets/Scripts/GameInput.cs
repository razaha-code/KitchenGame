using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public sealed class GameInput : MonoBehaviour
{
    public event EventHandler InteractAction;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction interactAction;

    public Vector2 MoveInput { get; private set; }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput.actions == null)
        {
            Debug.LogError("GameInput needs an Input Actions asset on PlayerInput.", this);
            enabled = false;
            return;
        }

        moveAction = playerInput.actions.FindAction("Player/Move", true);
        interactAction = playerInput.actions.FindAction("Player/Interact", true);
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
        MoveInput = Vector2.zero;
    }

    private void Update()
    {
        if (moveAction == null)
            return;

        MoveInput = moveAction.ReadValue<Vector2>();
        if (MoveInput.sqrMagnitude > 1f)
            MoveInput = MoveInput.normalized;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        InteractAction?.Invoke(this, EventArgs.Empty);
    }
}
