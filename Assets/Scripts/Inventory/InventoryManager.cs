using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Inv
    [SerializeField] private int inventorySize = 18;
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    
    // UI
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < inventorySize; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item.isStackable)
        {
            foreach (InventorySlot slot in inventorySlots)
            {
                if (slot.itemData == item && slot.amount < item.maxStackSize)
                {
                    int spaceLeft = item.maxStackSize - slot.amount;
                    int addAmount = Mathf.Min(amount, spaceLeft);
                    slot.amount += addAmount;
                    amount -= addAmount;

                    if (amount == 0)
                    {
                        onInventoryChangedCallback?.Invoke();
                        return true; // All items added
                    }
                    // Todo: Add handling for excess items
                }
            }
        }

        for (int i = 0; i < inventorySize; i++)
        {
            if (inventorySlots[i].itemData == null)
            {
                inventorySlots[i].itemData = item;
                inventorySlots[i].amount = amount;
                return true;
            }
        }
        
        Debug.Log($"Inventory Full");
        return false;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].itemData == item)
            {
                inventorySlots[i].amount -= amount;
                if (inventorySlots[i].amount <= 0)
                {
                    inventorySlots[i].itemData = null;
                    inventorySlots[i].amount = 0;
                }
                onInventoryChangedCallback?.Invoke();
                return true;
            }
        }
        Debug.Log("Item not found in inventory: " + item.itemName);
        return false;
    }
    
    
    // Inventory Slot
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int amount;

        public InventorySlot()
        {
            itemData = null;
            amount = 0;
        }

        public InventorySlot(ItemData item, int count)
        {
            itemData = item;
            amount = count;
        }
    }

}
