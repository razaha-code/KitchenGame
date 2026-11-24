using UnityEngine;

public sealed class KitchenObjectMarker : MonoBehaviour
{
    public KitchenObjectIO KitchenObject { get; private set; }
    private IKitchenObjectParent kitchenObjectParent;

    public bool SetKitchenObjectParent(IKitchenObjectParent parent)
    {
        if (parent == null || (parent.GetKitchenObject() != null && parent.GetKitchenObject() != this))
            return false;

        if (kitchenObjectParent == parent)
            return true;

        kitchenObjectParent?.ClearKitchenObject();
        kitchenObjectParent = parent;
        kitchenObjectParent.SetKitchenObject(this);

        transform.SetParent(parent.KitchenObjectFollowTransform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        return true;
    }

    public void Initialize(KitchenObjectIO kitchenObject)
    {
        KitchenObject = kitchenObject;
    }

    public void ClearKitchenObjectParent()
    {
        kitchenObjectParent?.ClearKitchenObject();
        kitchenObjectParent = null;
        transform.SetParent(null, true);
    }
}
