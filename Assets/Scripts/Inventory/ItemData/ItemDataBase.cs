using UnityEngine;

[CreateAssetMenu(fileName = "ItemBase", menuName = "Items/ItemBase", order = 0)]
public class ItemDataBase : ScriptableObject
{
    public string ID;
    public GameObject DropPrefab;
    public GameObject HandPrefab;
    [Space]
    public Mesh MeshModel;
    public Material MaterialModel;
}
