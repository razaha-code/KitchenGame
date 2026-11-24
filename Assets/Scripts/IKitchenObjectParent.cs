using UnityEngine;

public interface IKitchenObjectParent
{
    Transform KitchenObjectFollowTransform { get; }
    KitchenObjectMarker GetKitchenObject();
    void SetKitchenObject(KitchenObjectMarker kitchenObject);
    void ClearKitchenObject();
}
