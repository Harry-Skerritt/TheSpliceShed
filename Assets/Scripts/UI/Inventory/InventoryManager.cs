using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }

    [Header("Scene Names")] 
    [SerializeField] private string gardenSceneName;
    
    
    [Header("UI")] [Tooltip("The parent panel for the entire inventory UI.")] [SerializeField]
    private GameObject inventoryPanel;

    [Tooltip("The parent transform where inventory slots will be instantiated.")] [SerializeField]
    private Transform inventoryGridParent;

    [Tooltip("The prefab for a single inventory slot UI element.")] [SerializeField]
    private GameObject inventorySlotPrefab;

    [Tooltip("The Text/TextMeshProUGUI element used to display the item being dragged.")] [SerializeField]
    private TextMeshProUGUI draggedItemDisplayText;

    [Tooltip("The parent panel for the tab buttons.")] [SerializeField]
    private GameObject tabsPanel;

    [Tooltip("Array of tab buttons. Assign in order.")] [SerializeField]
    private Button[] tabButtons;

    [Tooltip("Array of colours which tab buttons will turn when active. Assign in order. Last = default colour")] [SerializeField]
    private Color[] tabActiveColours;

    [Header("Inventory Settings")] [Tooltip("The number of slots the inventory starts with.")] [SerializeField]
    private int startingSlotsCount = 27;

    [Tooltip("The number of new slots to add when the inventory expands.")] [SerializeField]
    private int slotsToExpandBy = 1;

    [Tooltip("Default sprite for item icons if a specific one isn't found.")] [SerializeField]
    public Sprite defaultItemIcon;


    private List<InventoryItem> inventorySlots;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private ItemType currentFilter = ItemType.All;

    // For dragging
    public InventorySlotUI draggedFromSlot { get; set; }
    public InventoryItem draggedItemData { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Init inventory slots
        inventorySlots = new List<InventoryItem>();
        for (int i = 0; i < startingSlotsCount; i++)
        {
            inventorySlots.Add(new InventoryItem());
        }

        GenerateInventorySlotsUI();

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            SetInventoryData(SaveManager.Instance.CurrentGameData.inventoryData);
            Debug.Log("InventoryManager: Loaded Inventory Data");
        }
        else
        {
            Debug.Log("InventoryManager: No inventory data loaded! Creating some!");
            AddItem(Resources.Load<ItemData>("Items/Rose"), 10);
        }

        SetupTabListeners();
        ResetTabColours();
        // Init the all tab every time
        SetTabColour(0, 0);
        

        if (draggedItemDisplayText != null)
        {
            draggedItemDisplayText.gameObject.SetActive(false);
        }

        SetFilter(currentFilter);
        
        // Hide Inventory on start
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (inventoryPanel != null)
        {
            if (!DevConsole.Instance.ConsoleVisible)
            {
                if (SceneManager.GetActiveScene().name == gardenSceneName)
                {
                    if (Input.GetKeyDown(KeyCode.I)) // Todo: Make global
                    {
                        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
                    }
                }
            }
        }
    }

    public void SetInventoryVisible(bool visible)
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(visible);
        }
    }

    void GenerateInventorySlotsUI()
    {
        if (inventoryGridParent == null || inventorySlotPrefab == null)
        {
            Debug.LogError("InventoryManager: InventoryGridParent or InventorySlotPrefab is null!");
            return;
        }

        // Remove existing slots
        foreach (Transform child in inventoryGridParent)
        {
            Destroy(child.gameObject);
        }

        slotUIs.Clear();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, inventoryGridParent);
            InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.slotIndex = i;
                slotUI.inventoryManager = this;
                slotUIs.Add(slotUI);
                slotUI.UpdateSlotDisplay(inventorySlots[i]);
            }
            else
            {
                Debug.LogWarning("InventoryManager: InventorySlotPrefab is missing InventorySlotUI component!");
            }
        }
    }

    void SetupTabListeners()
    {
        if (tabButtons == null || tabButtons.Length != 4)
        {
            Debug.LogError($"InventoryManager: Tab Buttons array is not assigned or has incorrect size (expected 4) in InventoryManager but got {tabButtons.Length}!");
            return;
        }

        tabButtons[0].onClick.AddListener(() =>
            {
                SetFilter(ItemType.All);
                ResetTabColours();
                SetTabColour(0, 0);

            });
        tabButtons[1].onClick.AddListener(() =>
            {
                SetFilter(ItemType.Flower);
                ResetTabColours();
                SetTabColour(1, 1);
            });
        tabButtons[2].onClick.AddListener(() =>
            {
                SetFilter(ItemType.Vegetable);
                ResetTabColours();
                SetTabColour(2, 2);
            });
        tabButtons[3].onClick.AddListener(() =>
        {
            SetFilter(ItemType.Fruit);
            ResetTabColours();
            SetTabColour(3, 3);
        });
    }

    public void ResetTabColours()
    {
        SetTabColour(0, 4);
        SetTabColour(1, 4);
        SetTabColour(2, 4);
        SetTabColour(3, 4);
    }

    private void SetTabColour(int tab, int colour)
    {
        Color tc = tabActiveColours[colour];
        tabButtons[tab].transform.GetChild(0).GetComponent<Image>().color = tc;
    }

    public bool AddItem(ItemData itemData, int quantity)
    {
        if (itemData == null)
        {
            Debug.LogError("InventoryManager: Attempted to ItemData that is null!");
            return false;
        }

        if (quantity <= 0)
        {
            Debug.LogError($"InventoryManager: Attempted to add {itemData.itemName} that is non-positive");
            return false;
        }

        if (itemData.isStackable)
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                // Check if the slot contains the same item and it's stackable and not at max stack size
                if (inventorySlots[i].itemName == itemData.itemName &&
                    inventorySlots[i].itemType == itemData.itemType &&
                    inventorySlots[i].quantity < itemData.maxStackSize)
                {
                    int spaceLeft = itemData.maxStackSize - inventorySlots[i].quantity;
                    int amountToStack = Mathf.Min(quantity, spaceLeft);

                    InventoryItem tempItem = inventorySlots[i];
                    tempItem.quantity += amountToStack;
                    inventorySlots[i] = tempItem;

                    quantity -= amountToStack;

                    UpdateSlotUI(i);
                    Debug.Log($"Stacked {amountToStack} x {itemData.itemName}. Total: {inventorySlots[i].quantity}");

                    if (quantity == 0)
                    {
                        SetFilter(currentFilter);
                        return true; // All quantity added
                    }
                }
            }
        }

        while (quantity > 0)
        {
            int emptySlotIndex = -1;
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i].itemName))
                {
                    emptySlotIndex = i;
                    break;
                }
            }

            // Handle expanding the inventory
            if (emptySlotIndex == -1)
            {
                Debug.Log("InventoryManager: Inventory Full - Expanding Inventory...");
                ExpandInventory(slotsToExpandBy);
                emptySlotIndex = inventorySlots.Count - slotsToExpandBy;
            }

            int amountToAdd = quantity;
            if (itemData.isStackable)
            {
                amountToAdd = Mathf.Min(quantity, itemData.maxStackSize);
            }

            // Add the item
            inventorySlots[emptySlotIndex] = new InventoryItem(itemData.itemName, amountToAdd, itemData.itemType,
                itemData.icon != null ? itemData.icon.name : "", itemData.isStackable, itemData.maxStackSize);
            UpdateSlotUI(emptySlotIndex);
            Debug.Log($"InventoryManager: Added new item to slot {emptySlotIndex}: {itemData.itemName} x {amountToAdd}");
            quantity -= amountToAdd;

            if (quantity == 0)
            {
                SetFilter(currentFilter);
                return true;
            }
        }

        SetFilter(currentFilter);
        return true;
    }

    private void ExpandInventory(int slotsToExpandBy)
    {
        for (int i = 0; i < slotsToExpandBy; i++)
        {
            inventorySlots.Add(new InventoryItem());
        }

        GenerateInventorySlotsUI();
        Debug.Log($"InventoryManager: Inventory expanded by {slotsToExpandBy} slots. Total slots: {inventorySlots.Count}");
    }

    public bool RemoveItem(string itemName, int quantity = 1)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning($"InventoryManager: Cannot remove non-positive quantity of {itemName}.");
            return false;
        }

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].itemName == itemName)
            {
                if (inventorySlots[i].quantity >= quantity)
                {
                    InventoryItem tempItem = inventorySlots[i];
                    tempItem.quantity -= quantity;
                    inventorySlots[i] = tempItem;

                    Debug.Log($"InventoryManager: Removed {quantity} x {itemName}. Remaining: {inventorySlots[i].quantity}");
                    if (inventorySlots[i].quantity == 0)
                    {
                        inventorySlots[i] = new InventoryItem(); // Clear the slot
                        Debug.Log($"InventoryManager: {itemName} removed from inventory.");
                    }

                    UpdateSlotUI(i);
                    SetFilter(currentFilter);
                    return true;
                }
                else
                {
                    Debug.LogWarning(
                        $"InventoryManager: Not enough {itemName} to remove {quantity}. Only {inventorySlots[i].quantity} available.");
                    return false;
                }
            }
        }

        Debug.LogWarning($"InventoryManager: Item {itemName} not found in inventory.");
        return false;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            inventorySlots[i] = new InventoryItem(); // Set each slot to an empty item
        }
        GenerateInventorySlotsUI(); // Regenerate UI to reflect empty slots
        SetFilter(currentFilter); 
        Debug.Log("InventoryManager: Inventory cleared.");  
    }


    public InventoryItem GetItemInSlot(int index)
    {
        if (index >= 0 && index < inventorySlots.Count)
        {
            return inventorySlots[index];
        }

        return new InventoryItem(); // Invalid
    }

    public void SetItemInSlot(int index, InventoryItem item)
    {
        if (index >= 0 && index < inventorySlots.Count)
        {
            inventorySlots[index] = item;
            UpdateSlotUI(index);
        }
    }

    public void UpdateSlotUI(int index)
    {
        if (index >= 0 && index < slotUIs.Count)
        {
            slotUIs[index].UpdateSlotDisplay(inventorySlots[index]);
        }
    }

    public void SetFilter(ItemType filterType)
    {
        currentFilter = filterType;
        Debug.Log($"InventoryManager: Setting inventory filter to: {filterType}");

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventoryItem item = inventorySlots[i];
            InventorySlotUI slotUI = slotUIs[i];

            bool shouldDisplay = false;

            if (filterType == ItemType.All)
            {
                shouldDisplay = true; // Always true
            }
            else if (!string.IsNullOrEmpty(item.itemName) && item.itemType == filterType)
            {
                shouldDisplay = true; // Matches Filter
            }
            else if (string.IsNullOrEmpty(item.itemName) && filterType == ItemType.All)
            {
                shouldDisplay = true; // Show all slots
            }

            slotUI.gameObject.SetActive(shouldDisplay);
            slotUI.UpdateSlotDisplay(item);
        }
    }


    // Drag & Drop Callbacks

    public void OnItemDragStart(InventorySlotUI fromSlot, InventoryItem itemData)
    {
        draggedFromSlot = fromSlot;
        draggedItemData = itemData;

        if (draggedItemDisplayText != null)
        {
            draggedItemDisplayText.gameObject.SetActive(true);
            draggedItemDisplayText.text = $"{itemData.itemName} x {itemData.quantity}";

        }
    }

    public void OnItemDrag(Vector2 screenPosition)
    {
        if (draggedItemDisplayText != null)
        {
            draggedItemDisplayText.transform.position = screenPosition;
        }
    }

    public void ClearDragState()
    {
        if (draggedItemDisplayText != null)
        {
            draggedItemDisplayText.gameObject.SetActive(false);
        }
        if (draggedFromSlot != null)
        {
            // Ensure the source slot's display is refreshed in case it was hidden during drag
            draggedFromSlot.UpdateSlotDisplay(GetItemInSlot(draggedFromSlot.slotIndex));
        }
        draggedFromSlot = null;
        draggedItemData = new InventoryItem(); // Reset to default empty struct
    }

    public void HandleInventoryToInventoryDrop(InventorySlotUI fromSlot, InventorySlotUI toSlot, InventoryItem item)
    {
        InventoryItem itemInTargetSlot = GetItemInSlot(toSlot.slotIndex);

        if (itemInTargetSlot.itemName == item.itemName && itemInTargetSlot.itemType == item.itemType &&
            itemInTargetSlot.quantity > 0 && item.isStackable)
        {
            // Handle Stacking
            int spaceLeft = item.maxStackSize - itemInTargetSlot.quantity;
            int amountToStack = Mathf.Min(item.quantity, spaceLeft);

            itemInTargetSlot.quantity += amountToStack;
            item.quantity -= amountToStack;

            SetItemInSlot(toSlot.slotIndex, itemInTargetSlot);

            if (item.quantity == 0)
            {
                // All moved, clear source slot
                SetItemInSlot(fromSlot.slotIndex, new InventoryItem());
                Debug.Log($"InventoryManager: Stacked all {item.itemName} from slot {fromSlot.slotIndex} to {toSlot.slotIndex}.");
            }
            else
            {
                // Partial stack, update source slot with remaining quantity
                SetItemInSlot(fromSlot.slotIndex, item);
                Debug.Log(
                    $"InventoryManager: Partially stacked {item.itemName} from slot {fromSlot.slotIndex} to {toSlot.slotIndex}. Remaining in source: {item.quantity}");
            }
        }
        else if (string.IsNullOrEmpty(itemInTargetSlot.itemName))
        {
            // Handle empty slot
            SetItemInSlot(toSlot.slotIndex, item);
            SetItemInSlot(fromSlot.slotIndex, new InventoryItem());
            Debug.Log($"InventoryManager: Moved {item.itemName} from slot {fromSlot.slotIndex} to empty slot {toSlot.slotIndex}.");
        }
        else
        {
            // Swapping: If items are different, swap them
            SetItemInSlot(toSlot.slotIndex, item); // Move dragged item to target
            SetItemInSlot(fromSlot.slotIndex, itemInTargetSlot); // Move target item to source
            Debug.Log(
                $"InventoryManager: Swapped {item.itemName} (from {fromSlot.slotIndex}) with {itemInTargetSlot.itemName} (at {toSlot.slotIndex}).");
        }

        SetFilter(currentFilter);
    }


    public void HandleInventoryToWorldDrop(InventorySlotUI fromSlot, InventoryItem itemToDrop)
    {
        Debug.Log(
            $"InventoryManager: Dropped {itemToDrop.itemName} x {itemToDrop.quantity} into the world from slot {fromSlot.slotIndex}.");
        // TODO: Instantiate a world item here at mouse position or player position
        // Example: WorldItemManager.Instance.SpawnItem(itemToDrop, mouseWorldPosition);

        // Clear the item from the inventory slot
        SetItemInSlot(fromSlot.slotIndex, new InventoryItem());
        SetFilter(currentFilter); // Update UI
    }

    // Public method to handle picking up an item from the world into inventory
    // This would be called by a WorldItem script when clicked/interacted with
    public bool PickUpItemFromWorld(ItemData itemData, int quantity)
    {
        Debug.Log($"InventoryManager: Attempting to pick up {itemData.itemName} x {quantity} from the world.");
        bool added = AddItem(itemData, quantity);
        if (added)
        {
            Debug.Log($"InventoryManager: Successfully picked up {itemData.itemName}.");
            // TODO: Destroy the world item GameObject here
        }
        else
        {
            Debug.Log($"InventoryManager: Failed to pick up {itemData.itemName}. Inventory might be full.");
        }

        return added;
    }


    // Save System Integration
    [Serializable]
    public struct InventoryItem
    {
        public string itemName;
        public int quantity;
        public ItemType itemType;
        public string iconName;
        public bool isStackable;
        public int maxStackSize;
        public int requiredPotSize;
        public float growthTime;

        public InventoryItem(string name, int qty, ItemType type, string icon, bool stackable = true,
            int maxStack = 100, int potSize = 0, float growth = 0.5f)
        {
            itemName = name;
            quantity = qty;
            itemType = type;
            iconName = icon;
            isStackable = stackable;
            maxStackSize = maxStack;
            requiredPotSize = potSize;
            growthTime = growth;
        }
    }

    [Serializable]
    public class InventoryData
    {
        public List<InventoryItem> slots;

        public InventoryData()
        {
            slots = new List<InventoryItem>();
        }
    }

    public InventoryData GetInventoryData()
    {
        InventoryData data = new InventoryData();
        data.slots = new List<InventoryItem>(inventorySlots);
        return data;
    }

    public void SetInventoryData(InventoryData data)
    {
        if (data == null || data.slots == null)
        {
            // Handle no data
            inventorySlots = new List<InventoryItem>();
            for (int i = 0; i < startingSlotsCount; i++)
            {
                inventorySlots.Add(new InventoryItem());
            }

            Debug.LogWarning("InventoryManager: No valid save data found for inventory. Initializing empty.");
        }
        else
        {
            inventorySlots.Clear();
            foreach (var entry in data.slots)
            {
                inventorySlots.Add(entry);
            }

            while (inventorySlots.Count < startingSlotsCount)
            {
                inventorySlots.Add(new InventoryItem());
            }
        }

        GenerateInventorySlotsUI();
        SetFilter(currentFilter);
    }
}
    

