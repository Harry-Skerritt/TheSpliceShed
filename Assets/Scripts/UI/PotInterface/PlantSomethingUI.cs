using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlantSomethingUI : MonoBehaviour, IPointerClickHandler
{
    public static PlantSomethingUI Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject potInterfacePanel;
    [SerializeField] private Image selectedItemImage;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemGrowthTime;
    [SerializeField] private TextMeshProUGUI itemType;
    [SerializeField] private TextMeshProUGUI itemPotSize;
    [SerializeField] private Button plantButton;
    [SerializeField] private TextMeshProUGUI errorText;
    
    [SerializeField] private ScreenDimmer screenDimmer;
    
    [Header("Water")]
    [SerializeField] private Image[] waterStatus;
    [SerializeField] private Color waterEmpty;
    [SerializeField] private Color waterFull;
    
    [Header("Technical")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [Range(0.0f, 0.6f)][SerializeField] private float buttonDisabledAlpha;
    
    private Pot currentlySelectedPot;
    private UIFadeTransition uiFadeTransition;
    private ItemData plantedItemDataCandidate;
    private Color plantButtonColour;
    private bool plantButtonActive = false;

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
        
        if(potInterfacePanel == null) Debug.LogError("PlantSomethingUI: No UI Assigned!");
        if(selectedItemImage == null) Debug.LogError("PlantSomethingUI: Selected Item Image is not assigned!");
        if(plantButton == null) Debug.LogError("PlantSomethingUI: PlantButton is not assigned!");

        if (plantButton != null && plantButton.image != null)
        {
            plantButtonColour = plantButton.image.color;
        }
        
        uiFadeTransition = potInterfacePanel.GetComponent<UIFadeTransition>();
        potInterfacePanel.GetComponent<CanvasGroup>().alpha = 0;
        
    }
    
    private void Start()
    {
        if (potInterfacePanel != null)
        {
            potInterfacePanel.SetActive(false);
        }

        if (waterStatus != null)
        {
            foreach (Image droplet in waterStatus)
            {
                droplet.color = waterEmpty;
            }
        }
        
        ResetPanel();
        SetupButtonListeners();
    }
    
    private void Update()
    {
        if(potInterfacePanel.activeSelf)
        {
            if (Input.GetKeyDown(closeKey))
            {
                ClosePanel();
            }
            // Todo: Add some handling for when the plant button should be active
        }
    }
    
    void SetupButtonListeners()
    {
        plantButton.onClick.AddListener(PlantSelection);
    }
    
    void ResetPanel()
    {
        title.text = "Ceramic Pot";
        
        selectedItemImage.sprite = null;

        itemName.text = "-";
        itemName.gameObject.SetActive(false);
        itemGrowthTime.text = "-";
        itemGrowthTime.gameObject.SetActive(false);
        itemType.text = "-";
        itemType.gameObject.SetActive(false);
        itemPotSize.text = "-";
        itemPotSize.gameObject.SetActive(false);

        if(waterStatus != null) 
        {
            foreach (Image droplet in waterStatus)
            {
                droplet.color = waterEmpty;
            }
        }
        
        plantedItemDataCandidate = null;
        UpdateButtonState();
        DisplayMessage("");
    }

    private void ShowPanel()
    {
        // Show the panel
        potInterfacePanel.SetActive(true);
        
        // Fade the screen dimmer
        if (screenDimmer != null)
        {
            screenDimmer.FadeIn();
        }
        
        // Fade the UI
        if (uiFadeTransition != null)
        {
            uiFadeTransition.FadeIn();
        }
    }
    
    public void OnPlantSomethingRequested(Pot pot)
    {
        currentlySelectedPot = pot;
        ResetPanel();
        ShowPanel();
        
        // Update Title
        string potSizeString = "Ceramic Pot";
        switch (currentlySelectedPot.GetPotSize())
        {
            case 0: potSizeString = "Small"; break;
            case 1: potSizeString = "Medium"; break;
            case 2: potSizeString = "Large"; break;
        }
        title.text = $"{potSizeString} Ceramic Pot ({currentlySelectedPot.GetPotID()})";

        if(currentlySelectedPot.GetPotStatus() != PotStatus.Empty)
        {
            DisplayMessage("This pot is not empty!");
            plantButton.interactable = false;
            // Optionally, show the current plant's details instead of empty fields
            //PopulateWithExistingPlant(currentlySelectedPot);
        }
        else
        {
            DisplayMessage("Drag a seed or plant item here to plant.");
            UpdateButtonState();
        }
    }

    public void HandleItemDropIntoSlot(PointerEventData eventData)
    {
        DisplayMessage("");
        
        if (InventoryManager.Instance == null || InventoryManager.Instance.draggedItemData.quantity <= 0)
        {
            Debug.LogWarning("PlantSomethingUI: OnDrop called without any item selected!");
            return;
        }

        if(currentlySelectedPot.GetPotStatus() != PotStatus.Empty)
        {
            Debug.LogWarning($"PlantSomethingUI: {currentlySelectedPot.GetPotID()} is not empty");
            DisplayMessage("This pot already has a plant!");
            InventoryManager.Instance.ClearDragState();
            return;
        }

        InventoryManager.InventoryItem draggedItem = InventoryManager.Instance.draggedItemData;
        InventorySlotUI sourceSlot = InventoryManager.Instance.draggedFromSlot;

        ItemData droppedItemData = Resources.Load<ItemData>("Items/" + draggedItem.itemName);

        if (droppedItemData == null)
        {
            Debug.LogError($"PlantSomethingUI: Could not load ItemData for '{draggedItem.itemName}'. Make sure it's in a Resources folder.");
            DisplayMessage("Invalid item for planting.");
            InventoryManager.Instance.ClearDragState();
            return;
        }

        if (droppedItemData.itemType == ItemType.Misc)
        {
            DisplayMessage("Cannot plant miscellaneous items.");
            InventoryManager.Instance.ClearDragState();
            return;
        }
        
        if (droppedItemData.requiredPotSize == -1 || droppedItemData.requiredPotSize > currentlySelectedPot.GetPotSize())
        {
            string requiredSize = "Any";
            switch (droppedItemData.requiredPotSize)
            {
                case 0: requiredSize = "Small"; break;
                case 1: requiredSize = "Medium"; break;
                case 2: requiredSize = "Large"; break;
            }
            DisplayMessage($"This pot is too small! Try a {requiredSize} pot.");
            InventoryManager.Instance.ClearDragState();
            return;
        }
        
        if (plantedItemDataCandidate != null)
        {
            InventoryManager.Instance.AddItem(plantedItemDataCandidate, 1);
        }
        
        // Accept For Planting
        plantedItemDataCandidate = droppedItemData;
        selectedItemImage.sprite = plantedItemDataCandidate.icon;
        selectedItemImage.gameObject.SetActive(true);

        itemName.text = plantedItemDataCandidate.itemName;
        itemName.gameObject.SetActive(true);

        itemGrowthTime.text = plantedItemDataCandidate.growthTime + " days";
        itemGrowthTime.gameObject.SetActive(true);

        string typeDisplay = "-";
        switch (plantedItemDataCandidate.itemType)
        {
            case ItemType.Flower: typeDisplay = "Flower"; break;
            case ItemType.Fruit: typeDisplay = "Fruit"; break;
            case ItemType.Vegetable: typeDisplay = "Vegetable"; break;
        }
        itemType.text = typeDisplay;
        itemType.gameObject.SetActive(true);

        string potSizeDisplay = "-";
        switch (plantedItemDataCandidate.requiredPotSize)
        {
            case 0: potSizeDisplay = "S"; break;
            case 1: potSizeDisplay = "M"; break;
            case 2: potSizeDisplay = "L"; break;
        }
        itemPotSize.text = potSizeDisplay;
        itemPotSize.gameObject.SetActive(true);

        // Remove one from the inventory stack
        InventoryManager.Instance.RemoveItem(draggedItem.itemName, 1);
        
        // Clear the drag state in InventoryManager
        InventoryManager.Instance.ClearDragState();

        // Update button state (should now be enabled)
        UpdateButtonState();
        DisplayMessage($"Ready to plant {plantedItemDataCandidate.itemName}.");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && plantedItemDataCandidate != null)
        {
            DisplayMessage(""); // Clear previous message
            // Return the item to inventory
            if (InventoryManager.Instance.AddItem(plantedItemDataCandidate, 1))
            {
                Debug.Log($"PlantSomethingUI: Returned {plantedItemDataCandidate.itemName} to inventory.");
                plantedItemDataCandidate = null; // Clear the slot
                ResetPanel(); // Update UI to reflect empty slot
                DisplayMessage("Item returned to inventory. Drag a new item to plant.");
            }
            else
            {
                Debug.LogWarning("PlantSomethingUI: Could not return item to inventory. Inventory might be full.");
                DisplayMessage("Inventory full! Cannot return item.");
            }
            UpdateButtonState();
        }
    }
    
    private void UpdateButtonState()
    {
        bool canPlant = (plantedItemDataCandidate != null && 
                         plantedItemDataCandidate.itemType != ItemType.Misc && 
                         currentlySelectedPot != null && 
                         currentlySelectedPot.GetPotStatus() == PotStatus.Empty);

        plantButton.interactable = canPlant;
        if (plantButton.image != null)
        {
            Color targetColor = canPlant ? plantButtonColour : new Color(plantButtonColour.r, plantButtonColour.g, plantButtonColour.b, buttonDisabledAlpha);
            plantButton.image.color = targetColor;
        }
    }
    
    private void PlantSelection()
    {
        DisplayMessage(""); // Clear any previous messages

        if (plantButton.interactable && plantedItemDataCandidate != null && currentlySelectedPot != null)
        {
            if (currentlySelectedPot.PlantItem(plantedItemDataCandidate))
            {
                Debug.Log($"Successfully planted {plantedItemDataCandidate.itemName} in {currentlySelectedPot.GetPotID()}!");
                ClosePanel(); // Close UI after successful planting
            }
            else
            {
                Debug.LogWarning($"Failed to plant {plantedItemDataCandidate.itemName}. Pot might be full or incompatible.");
                DisplayMessage("Planting failed. Check pot compatibility.");
            }
        }
        else
        {
            Debug.LogWarning("PlantSomethingUI: Plant button clicked but conditions not met.");
            DisplayMessage("Cannot plant: Invalid item or pot occupied.");
        }
    }
    
    public void ClosePanel()
    {
        if (potInterfacePanel != null)
        {
            potInterfacePanel.GetComponent<CanvasGroup>().alpha = 0;
            potInterfacePanel.SetActive(false);
        }
        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(false); // Tell the pot to unselect its visual
            currentlySelectedPot = null; // Clear the reference
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetInventoryVisible(false);
        }
        
        if (plantedItemDataCandidate != null)
        {
            if (InventoryManager.Instance.AddItem(plantedItemDataCandidate, 1))
            {
                Debug.Log($"PlantSomethingUI: Returned {plantedItemDataCandidate.itemName} to inventory on close.");
            }
            else
            {
                Debug.LogWarning("PlantSomethingUI: Could not return item to inventory on close. Inventory might be full.");
                // Optionally, spawn the item in the world if inventory is full
            }
        }
        ResetPanel(); // Ensure all fields are reset
        
        if (screenDimmer != null)
        {
            screenDimmer.FadeOut();
        }
        
        Debug.Log("PlantSomethingUI: Panel closed.");
    }
    
    private void DisplayMessage(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
    
}
