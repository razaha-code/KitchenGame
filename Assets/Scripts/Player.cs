using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class Player : MonoBehaviour, IKitchenObjectParent
{
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;
    [SerializeField, Min(0f)] private float collisionPadding = 0.04f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0f)] private float walkCycleSpeed = 8f;
    [SerializeField, Min(0f)] private float bodyBobHeight = 0.045f;
    [SerializeField, Min(0f)] private float bodySwayAngle = 4f;
    [SerializeField, Min(0f)] private float headBobHeight = 0.08f;
    [SerializeField, Min(0f)] private float headNodAngle = 10f;

    [SerializeField, Min(0f)] private float interactionDistance = 1.5f;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private Vector3 defaultKitchenObjectHoldPosition = new Vector3(0f, 1.05f, 0.45f);

    private Rigidbody body;
    private Animator animator;
    private GameInput gameInput;
    private ClearCounter selectedCounter;
    private Collider[] duckColliders;
    private Transform bodyVisual;
    private Transform headVisual;
    private Vector3 bodyStartPosition;
    private Vector3 headStartPosition;
    private Quaternion bodyStartRotation;
    private Quaternion headStartRotation;
    private Vector2 moveInput;
    private bool isWalking;
    private float walkBlend;
    private float walkPhase;
    private KitchenObjectMarker heldKitchenObject;

    public bool HasKitchenObject => heldKitchenObject != null;
    public Transform KitchenObjectFollowTransform => GetKitchenObjectHoldPoint();

    private void Awake()
    {
        collisionMask = collisionMask.value | LayerMask.GetMask("Counters");

        body = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        gameInput = GetComponent<GameInput>();
        bodyVisual = transform.Find("Body");
        if (bodyVisual == null)
            bodyVisual = transform.Find("Duck_Body");

        headVisual = transform.Find("Head");
        if (headVisual == null)
            headVisual = transform.Find("Duck_Head");

        if (bodyVisual != null)
        {
            bodyStartPosition = bodyVisual.localPosition;
            bodyStartRotation = bodyVisual.localRotation;
        }

        if (headVisual != null)
        {
            headStartPosition = headVisual.localPosition;
            headStartRotation = headVisual.localRotation;
        }

        if (gameInput == null)
        {
            Debug.LogError("Player needs a GameInput component.", this);
            enabled = false;
            return;
        }

        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        duckColliders = GetComponentsInChildren<Collider>(true);
        if (duckColliders.Length == 0)
        {
            Debug.LogError("Player needs at least one collider in its hierarchy.", this);
            enabled = false;
        }

        gameInput.InteractAction += HandleInteractAction;
    }

    private void OnDestroy()
    {
        if (gameInput != null)
            gameInput.InteractAction -= HandleInteractAction;

        SetSelectedCounter(null);
    }

    private void Update()
    {
        moveInput = gameInput.MoveInput;
        SetSelectedCounter(FindInteractableCounter());

        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, isWalking);
            return;
        }

        walkBlend = Mathf.MoveTowards(walkBlend, isWalking ? 1f : 0f, Time.deltaTime * 8f);
        if (walkBlend > 0.001f)
            walkPhase += Time.deltaTime * walkCycleSpeed;
        else
            walkPhase = 0f;

        float step = Mathf.Sin(walkPhase);
        float bob = Mathf.Abs(step) * bodyBobHeight * walkBlend;

        if (bodyVisual != null)
        {
            bodyVisual.localPosition = bodyStartPosition + Vector3.up * bob;
            bodyVisual.localRotation = bodyStartRotation
                * Quaternion.Euler(0f, 0f, step * bodySwayAngle * walkBlend);
        }

        if (headVisual != null)
        {
            float headBob = Mathf.Abs(step) * headBobHeight * walkBlend;
            headVisual.localPosition = headStartPosition + Vector3.up * headBob;
            headVisual.localRotation = headStartRotation
                * Quaternion.Euler(0f, 0f, -step * headNodAngle * walkBlend);
        }
    }

    private void HandleInteractAction(object sender, EventArgs e)
    {
        selectedCounter?.Interact(this);
    }

    public KitchenObjectMarker GetKitchenObject()
    {
        return heldKitchenObject;
    }

    public void SetKitchenObject(KitchenObjectMarker kitchenObject)
    {
        heldKitchenObject = kitchenObject;
    }

    public void ClearKitchenObject()
    {
        heldKitchenObject = null;
    }

    private Transform GetKitchenObjectHoldPoint()
    {
        if (kitchenObjectHoldPoint != null)
            return kitchenObjectHoldPoint;

        GameObject holdPoint = new GameObject("KitchenObjectHoldPoint");
        kitchenObjectHoldPoint = holdPoint.transform;
        kitchenObjectHoldPoint.SetParent(transform, false);
        kitchenObjectHoldPoint.localPosition = defaultKitchenObjectHoldPosition;
        kitchenObjectHoldPoint.localRotation = Quaternion.identity;
        return kitchenObjectHoldPoint;
    }

    private void SetSelectedCounter(ClearCounter counter)
    {
        if (selectedCounter == counter)
            return;

        if (selectedCounter != null)
            selectedCounter.SetSelected(false);

        selectedCounter = counter;
        if (selectedCounter != null)
            selectedCounter.SetSelected(true);
    }

    private ClearCounter FindInteractableCounter()
    {
        ClearCounter[] counters = FindObjectsByType<ClearCounter>(FindObjectsSortMode.None);
        ClearCounter closestCounter = null;
        float closestDistance = interactionDistance;
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        foreach (ClearCounter counter in counters)
        {
            if (counter == null)
                continue;

            Collider counterCollider = counter.GetComponentInChildren<Collider>();
            if (counterCollider == null)
                continue;

            Vector3 colliderCenter = counterCollider.bounds.center;
            Vector3 samplePosition = new Vector3(origin.x, colliderCenter.y, origin.z);
            Vector3 closestPoint = counterCollider.ClosestPoint(samplePosition);
            Vector3 toCounter = closestPoint - origin;
            toCounter.y = 0f;
            if (toCounter.sqrMagnitude <= 0.001f)
            {
                toCounter = colliderCenter - origin;
                toCounter.y = 0f;
            }

            float distance = toCounter.magnitude;
            if (distance > closestDistance || distance <= 0.001f)
                continue;

            if (Vector3.Dot(forward, toCounter / distance) < 0.35f)
                continue;

            closestDistance = distance;
            closestCounter = counter;
        }

        return closestCounter;
    }

    private void FixedUpdate()
    {
        if (moveInput.sqrMagnitude < 0.0001f)
        {
            isWalking = false;
            return;
        }

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        float distance = moveSpeed * Time.fixedDeltaTime;
        Bounds bounds = default;
        bool hasBounds = false;

        foreach (Collider duckCollider in duckColliders)
        {
            if (duckCollider == null || !duckCollider.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = duckCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(duckCollider.bounds);
            }
        }

        if (!hasBounds)
        {
            isWalking = false;
            return;
        }

        // Try the intended direction first. If an obstacle blocks a diagonal
        // move, test the two axes independently so movement can slide along it.
        float diagonalDistance = GetAllowedDistance(bounds, direction, distance);
        Vector3 movement = Vector3.zero;

        if (diagonalDistance >= distance - 0.0001f)
        {
            movement = direction * distance;
        }
        else
        {
            Vector3 xDirection = new Vector3(Mathf.Sign(moveInput.x), 0f, 0f);
            Vector3 zDirection = new Vector3(0f, 0f, Mathf.Sign(moveInput.y));
            float xAvailable = Mathf.Abs(moveInput.x) > 0.0001f
                ? GetAllowedDistance(bounds, xDirection, distance)
                : 0f;
            float zAvailable = Mathf.Abs(moveInput.y) > 0.0001f
                ? GetAllowedDistance(bounds, zDirection, distance)
                : 0f;

            bool canMoveX = xAvailable > 0.0001f;
            bool canMoveZ = zAvailable > 0.0001f;

            if (canMoveX && canMoveZ)
            {
                float xDistance = Mathf.Min(xAvailable, distance * Mathf.Abs(moveInput.x));
                Vector3 xMovement = xDirection * xDistance;
                movement += xMovement;

                // Test the second axis from the bounds' position after moving
                // along X, so the combined slide cannot cut through a corner.
                bounds.center += xMovement;
                float zDistance = Mathf.Min(
                    distance * Mathf.Abs(moveInput.y),
                    GetAllowedDistance(bounds, zDirection, distance * Mathf.Abs(moveInput.y)));
                movement += zDirection * zDistance;
            }
            else if (canMoveX)
            {
                movement = xDirection * xAvailable;
            }
            else if (canMoveZ)
            {
                movement = zDirection * zAvailable;
            }
        }

        if (movement.sqrMagnitude > 0.000001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime));
            body.MovePosition(body.position + movement);
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }
    }

    private float GetAllowedDistance(Bounds bounds, Vector3 direction, float distance)
    {
        if (distance <= 0f)
            return 0f;

        // Sweep the combined collider bounds, ignoring the duck's own colliders.
        RaycastHit[] hits = Physics.BoxCastAll(
            bounds.center,
            bounds.extents,
            direction,
            Quaternion.identity,
            distance + collisionPadding,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float allowedDistance = distance;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.root == transform.root)
                continue;

            allowedDistance = Mathf.Min(
                allowedDistance,
                Mathf.Max(0f, hit.distance - collisionPadding));
        }

        return allowedDistance;
    }
}
