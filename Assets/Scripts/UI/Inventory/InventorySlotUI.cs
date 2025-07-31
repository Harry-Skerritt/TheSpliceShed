using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = System.Random;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    public InventoryManager inventoryManager;

    [Header("UI Elements")] 
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemQuantityText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [SerializeField] private Sprite[] slotBackgrounds;
    [SerializeField] private Image slotBackgroundImage;

    private InventoryManager.InventoryItem currentItem;


    private void Awake()
    {
        // Set random background
        int background = UnityEngine.Random.Range(0, slotBackgrounds.Length);
        slotBackgroundImage.sprite = slotBackgrounds[background];
    }

    private void OnEnable()
    {
        if (inventoryManager != null)
        {
            UpdateSlotDisplay(inventoryManager.GetItemInSlot(slotIndex));
        }
    }

    public void UpdateSlotDisplay(InventoryManager.InventoryItem item)
    {
        currentItem = item;

        if (itemIcon == null || itemQuantityText == null)
        {
            Debug.LogError("ItemIcon or ItemQuantityText not assigned");
            return;
        }

        if (!string.IsNullOrEmpty(item.itemName) && item.quantity > 0)
        {
            itemIcon.gameObject.SetActive(true);
            itemQuantityText.gameObject.SetActive(true);
            itemQuantityText.text = item.quantity.ToString();
            
            Sprite loadedIcon = Resources.Load<Sprite>("Item Sprites/" + item.iconName);
            if (loadedIcon != null)
            {
                itemIcon.sprite = loadedIcon;
            }
            else if (inventoryManager.defaultItemIcon != null)
            {
                itemIcon.sprite = inventoryManager.defaultItemIcon; // Use default if specific icon not found
                Debug.LogWarning($"Icon for '{item.itemName}' not found at Resources/Item Sprites/{item.iconName}. Using default icon.");
            }
            else
            {
                // Fallback if no default icon
                itemIcon.color = Color.gray; // Make it visible even without a sprite
                Debug.LogWarning($"No icon found for '{item.itemName}' and no default icon set.");
            }
        }
        else
        {
            // Slot empty
            itemIcon.gameObject.SetActive(false);
            itemQuantityText.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!string.IsNullOrEmpty(currentItem.itemName))
            {
                Debug.Log($"Clicked on {currentItem.itemName} in slot {slotIndex}");
                // Todo: Put the logic here to use items
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!string.IsNullOrEmpty(currentItem.itemName))
            {
                Debug.Log($"Right-Clicked on {currentItem.itemName} in slot {slotIndex}");
                //Todo: Put logic here for right click
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && !string.IsNullOrEmpty(currentItem.itemName))
        {
            inventoryManager.OnItemDragStart(this, currentItem);
            itemIcon.gameObject.SetActive(false);
            itemQuantityText.gameObject.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inventoryManager.draggedItemData.quantity > 0)
        {
            inventoryManager.OnItemDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inventoryManager != null && inventoryManager.draggedFromSlot == this)
        {
            // Item was dropped into the world (not on another inventory slot)
            inventoryManager.HandleInventoryToWorldDrop(this, inventoryManager.draggedItemData);
            inventoryManager.ClearDragState(); // Clear the drag state
        }
        else if (inventoryManager != null)
        {
            // If draggedFromSlot is not this, it means OnDrop on another slot already handled it
            // or the drag was cancelled without a valid drop. Just ensure this slot's display is updated.
            UpdateSlotDisplay(inventoryManager.GetItemInSlot(slotIndex));

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        // This method is called on the *target* slot when an item is dropped onto it.
        if (inventoryManager != null && inventoryManager.draggedItemData.quantity > 0)
        {
            // Item was dropped onto this slot from another inventory slot
            inventoryManager.HandleInventoryToInventoryDrop(inventoryManager.draggedFromSlot, this, inventoryManager.draggedItemData);
            inventoryManager.ClearDragState(); // Clear the drag state
        }
    }
}
