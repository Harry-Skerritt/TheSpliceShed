using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon = null;
    public bool isStackable = true;
    public int maxStackSize = 100;
    public PotSize requiredPotSize = PotSize.Small;
    [UnityEngine.Range(0, 20)] public float growthTime = 0.5f; // In days 
    [UnityEngine.Range(0.0f, 1.0f)] public float drainRate = 0.5f;
    
    public ItemType itemType = ItemType.Misc;
}

public enum ItemType
{
    All,
    Flower,
    Vegetable,
    Fruit,
    Misc
}
