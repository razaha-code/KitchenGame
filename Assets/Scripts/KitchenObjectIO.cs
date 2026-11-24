using UnityEngine;

[CreateAssetMenu(fileName = "KitchenObjectIO", menuName = "Scriptable Objects/KitchenObjectIO")]
public sealed class KitchenObjectIO : ScriptableObject
{
    [SerializeField] private string objectName;
    [SerializeField] private Transform prefab;
    [SerializeField] private Sprite sprite;

    public string ObjectName => string.IsNullOrWhiteSpace(objectName) ? name : objectName;
    public Transform Prefab => prefab;
    public Sprite Sprite => sprite;
}
