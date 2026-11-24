using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ClearCounter : MonoBehaviour, IKitchenObjectParent
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color SelectedTint = new Color(1f, 0.72f, 0.08f, 1f);
    [SerializeField] private KitchenObjectIO kitchenObject;
    [SerializeField] private Transform counterTopPoint;
    private KitchenObjectMarker storedObject;
    private Transform fallbackCounterTopPoint;

    public event EventHandler OnInteract;

    private Renderer[] renderers;
    private MaterialPropertyBlock[] originalPropertyBlocks;
    private MaterialPropertyBlock[] selectedPropertyBlocks;
    private int[] colorPropertyIds;
    private Color[] originalColors;
    private bool isSelected;

    public Transform KitchenObjectFollowTransform => GetCounterTopPoint();

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalPropertyBlocks = new MaterialPropertyBlock[renderers.Length];
        selectedPropertyBlocks = new MaterialPropertyBlock[renderers.Length];
        colorPropertyIds = new int[renderers.Length];
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            originalPropertyBlocks[i] = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(originalPropertyBlocks[i]);

            selectedPropertyBlocks[i] = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(selectedPropertyBlocks[i]);

            Material material = targetRenderer.sharedMaterial;
            if (material == null)
                continue;

            int colorPropertyId = material.HasProperty(BaseColorId)
                ? BaseColorId
                : material.HasProperty(ColorId) ? ColorId : 0;
            colorPropertyIds[i] = colorPropertyId;

            if (colorPropertyId != 0)
                originalColors[i] = material.GetColor(colorPropertyId);
        }
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
            return;

        isSelected = selected;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (colorPropertyIds[i] == 0)
                continue;

            if (selected)
            {
                Color color = Color.Lerp(originalColors[i], SelectedTint, 0.65f);
                selectedPropertyBlocks[i].SetColor(colorPropertyIds[i], color);
                renderers[i].SetPropertyBlock(selectedPropertyBlocks[i]);
            }
            else
            {
                renderers[i].SetPropertyBlock(originalPropertyBlocks[i]);
            }
        }
    }

    public void Interact(Player player)
    {
        if (player == null)
            return;

        if (storedObject != null)
        {
            if (player.HasKitchenObject)
                return;

            KitchenObjectIO pickedObject = storedObject.KitchenObject;
            if (!storedObject.SetKitchenObjectParent(player))
                return;
            LogInteraction(pickedObject);
            return;
        }

        if (player.HasKitchenObject)
        {
            KitchenObjectMarker carriedObject = player.GetKitchenObject();
            if (carriedObject != null && carriedObject.SetKitchenObjectParent(this))
            {
                PositionObjectOnCounterTop(carriedObject);
                LogInteraction(carriedObject.KitchenObject);
            }
            return;
        }

        if (kitchenObject == null || kitchenObject.Prefab == null)
            return;

        Transform spawnedTransform = Instantiate(kitchenObject.Prefab, GetCounterTopPoint());
        spawnedTransform.localPosition = Vector3.zero;
        KitchenObjectMarker marker = spawnedTransform.GetComponent<KitchenObjectMarker>();
        if (marker == null)
            marker = spawnedTransform.gameObject.AddComponent<KitchenObjectMarker>();
        marker.Initialize(kitchenObject);
        if (!marker.SetKitchenObjectParent(this))
        {
            Destroy(spawnedTransform.gameObject);
            return;
        }
        PositionObjectOnCounterTop(marker);
        LogInteraction(kitchenObject);
    }

    public KitchenObjectMarker GetKitchenObject()
    {
        return storedObject;
    }

    public void SetKitchenObject(KitchenObjectMarker kitchenObjectMarker)
    {
        storedObject = kitchenObjectMarker;
    }

    public void ClearKitchenObject()
    {
        storedObject = null;
    }

    private void LogInteraction(KitchenObjectIO item)
    {
        string objectName = item != null ? item.ObjectName : "unknown kitchen object";
        Debug.Log($"Interacted with {objectName}", this);
        OnInteract?.Invoke(this, EventArgs.Empty);
    }

    private void PositionObjectOnCounterTop(KitchenObjectMarker kitchenObjectMarker)
    {
        Collider counterCollider = GetComponentInChildren<Collider>();
        if (counterCollider == null)
            return;

        Renderer[] objectRenderers = kitchenObjectMarker.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds objectBounds = default;
        foreach (Renderer objectRenderer in objectRenderers)
        {
            if (objectRenderer == null || !objectRenderer.enabled)
                continue;

            if (!hasBounds)
            {
                objectBounds = objectRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                objectBounds.Encapsulate(objectRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            float verticalOffset = counterCollider.bounds.max.y + 0.015f - objectBounds.min.y;
            if (verticalOffset > 0f)
                kitchenObjectMarker.transform.position += Vector3.up * verticalOffset;
        }
    }

    private Transform GetCounterTopPoint()
    {
        if (counterTopPoint != null)
            return counterTopPoint;

        if (fallbackCounterTopPoint != null)
            return fallbackCounterTopPoint;

        Collider counterCollider = GetComponentInChildren<Collider>();
        Vector3 point = transform.position;
        if (counterCollider != null)
            point.y = counterCollider.bounds.max.y + 0.12f;
        fallbackCounterTopPoint = CreateCounterTopAnchor(point);
        return fallbackCounterTopPoint;
    }

    private Transform CreateCounterTopAnchor(Vector3 worldPosition)
    {
        Transform anchor = new GameObject("CounterTopPoint").transform;
        anchor.SetParent(transform, true);
        anchor.position = worldPosition;
        return anchor;
    }
}
