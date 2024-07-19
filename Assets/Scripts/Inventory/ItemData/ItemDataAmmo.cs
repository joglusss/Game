using UnityEngine;

[CreateAssetMenu(fileName = "ItemBase", menuName = "Items/ItemAmmo", order = 1)]
public class ItemDataAmmo : ItemDataBase
{
    [Space]
    public float Damage = 1;
    public float ArmorPenetration = 1;
    public int CountBullet;
}
