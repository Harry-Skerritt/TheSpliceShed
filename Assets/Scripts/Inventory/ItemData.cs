using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public bool isStackable = true;
    public int maxStackSize = 30;
    
    public ItemType itemType = ItemType.Generic;
}

public enum ItemType
{
    Generic,
    Flower,
    Vegetable,
    Fruit,
    Money,
    SplicedSprig
}
