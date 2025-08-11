using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PotUI : MonoBehaviour
{
    
    public static PotUI Instance { get; private set; }

    [Header("UI References")] 
    [SerializeField] private GameObject potInterfacePanel;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private Image currentlyGrowingIcon;
    [SerializeField] private TextMeshProUGUI growingItemName;
    [SerializeField] private TextMeshProUGUI growingItemType;
    [SerializeField] private TextMeshProUGUI growingItemAge;
    [SerializeField] private TextMeshProUGUI timeUntilHarvest;
    [SerializeField] private Button waterButton;
    [SerializeField] private Button harvestButton;
    [SerializeField] private Button unplantButton;

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
    
    
    // Buttons
    private Color waterButtonColour;
    private Color harvestButtonColour;

    
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
        
        if(potInterfacePanel == null) Debug.LogError("PotUI: No UI Assigned!");
        if(waterButton == null) Debug.LogError("PotUI: No Water Button Assigned!");
        if(harvestButton == null) Debug.LogError("PotUI: No Harvest Button Assigned!");
        if(unplantButton == null) Debug.LogError("PotUI: No Unplant Button Assigned!");

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
        
        waterButtonColour = waterButton.image.color;
        harvestButtonColour = harvestButton.image.color;
        
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

            if (currentlySelectedPot != null)
            {
                // Hide unplant button unless pot not empty
                if (unplantButton != null)
                {
                    unplantButton.gameObject.SetActive(false);
                }
                
                if(currentlySelectedPot.GetPotStatus() != PotStatus.Empty)
                {
                    if (unplantButton != null)
                    {
                        unplantButton.gameObject.SetActive(true);
                    }
                    
                    if (currentlySelectedPot.GetWaterLevel() < 5.0f)
                    {
                        waterButton.interactable = true;
                        waterButton.image.color = waterButtonColour;
                    }
                    else
                    {
                        waterButton.interactable = false;
                        Color buttonDisabledColour = waterButtonColour;
                        buttonDisabledColour.a = buttonDisabledAlpha;
                        waterButton.image.color = buttonDisabledColour;
                    }

                    if(currentlySelectedPot.GetPotStatus() == PotStatus.ReadyToHarvest)
                    {
                        harvestButton.interactable = true;
                        harvestButton.image.color = harvestButtonColour;
                    }
                    else
                    {
                        harvestButton.interactable = false;
                        Color buttonDisabledColour = harvestButtonColour;
                        buttonDisabledColour.a = buttonDisabledAlpha;
                        harvestButton.image.color = buttonDisabledColour;
                    }
                    
                    CalculateWaterLevel(currentlySelectedPot);
                    CalculateHarvestTime(currentlySelectedPot);
                }
            }
        }
    }

    void SetupButtonListeners()
    {
        waterButton.onClick.AddListener(WaterPlants);

        harvestButton.onClick.AddListener(HarvestPlants);
        
        unplantButton.onClick.AddListener(UnplantPlant);
    }
    
    void ResetPanel()
    {
        title.text = "Ceramic Pot";
        currentlyGrowingIcon.sprite = null;
        currentlyGrowingIcon.gameObject.SetActive(false);

        growingItemName.text = "";
        growingItemName.gameObject.SetActive(false);
        growingItemType.text = "";
        growingItemType.gameObject.SetActive(false);
        growingItemAge.text = "";
        growingItemAge.gameObject.SetActive(false);
        
        timeUntilHarvest.text = "";
        timeUntilHarvest.gameObject.SetActive(false);
        
        foreach (Image droplet in waterStatus)
        {
            droplet.color = waterEmpty;
        }
    }

    public void OnPotClicked(Pot clickedPot)
    {
        if (currentlySelectedPot != null && currentlySelectedPot != clickedPot)
        {
            currentlySelectedPot.SetSelectedVisual(false);
        }

        currentlySelectedPot = clickedPot;

        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(true);
            PopulatePanel(currentlySelectedPot);
        }
        else
        {
            ClosePanel();
        }

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
    
    private void PopulatePanel(Pot pot)
    {
       ResetPanel();
       ShowPanel();
        
        // Title Size
        string potSizeString = "Ceramic Pot";
        switch (pot.GetPotSize())
        {
            case PotSize.Small: potSizeString = "Small"; break;
            case PotSize.Medium: potSizeString = "Medium"; break;
            case PotSize.Large: potSizeString = "Large"; break;
        }

        title.text = $"{potSizeString} Ceramic Pot";
        
        // Plant Details
        if((currentlySelectedPot.GetPotStatus() != PotStatus.Empty) && pot.GetPlantGrowing() != null)
        {
            // Icon
            currentlyGrowingIcon.sprite = pot.GetPlantGrowing().icon;
            currentlyGrowingIcon.gameObject.SetActive(true);
            
            // Plant Name
            growingItemName.text = pot.GetPlantGrowing().itemName;
            growingItemName.gameObject.SetActive(true);
            
            // Plant Type
            string typeText = "Generic";
            switch (pot.GetPlantGrowing().itemType)
            {
                case ItemType.Flower: typeText = "Flower"; break;
                case ItemType.Fruit: typeText = "Fruit"; break;
                case ItemType.Vegetable: typeText = "Vegetable"; break;
                case ItemType.Misc: typeText = "Misc"; break;
            }
            growingItemType.text = typeText;
            growingItemType.gameObject.SetActive(true);
            
            // Age
            string ageString = pot.GetPlantAge().ToString();
            if (pot.GetAgeInHours())
            {
                ageString += " hours";
            }
            else
            {
                ageString += " days";
            }
            growingItemAge.text = ageString;
            growingItemAge.gameObject.SetActive(true);
            
            // Harvest Time
            CalculateHarvestTime(pot);
            timeUntilHarvest.gameObject.SetActive(true);
            
            // Water
            CalculateWaterLevel(pot);

        }
        else
        {
            // Empty Pot
            currentlyGrowingIcon.gameObject.SetActive(false);
            growingItemName.text = "Empty";
            growingItemName.gameObject.SetActive(true);
            growingItemType.gameObject.SetActive(false);
            growingItemAge.gameObject.SetActive(false);
            timeUntilHarvest.gameObject.SetActive(false);
            // Reset water visuals
            foreach (Image droplet in waterStatus)
            {
                droplet.color = waterEmpty;
            }
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
        ResetPanel(); // Ensure all fields are reset
        
        if (screenDimmer != null)
        {
            screenDimmer.FadeOut();
        }
    }

    private void WaterPlants()
    {
        if (waterButton.interactable)
        {
            currentlySelectedPot.WaterPlant();
        }
    }

    private void HarvestPlants()
    {
        if (harvestButton.interactable)
        {
            currentlySelectedPot.HarvestItem();
        }
    }

    private void UnplantPlant()
    {
        if (unplantButton.gameObject.activeSelf)
        {
            currentlySelectedPot.UnplantItem();
            ClosePanel();
        }
    }


    void CalculateWaterLevel(Pot pot)
    {
        int waterToFill = ConvertWaterValue(pot.GetWaterLevel());
        
        for (int i = 0; i < waterStatus.Length; i++)
        {
            if (waterStatus[i] != null)
            {
                waterStatus[i].color = (i < waterToFill) ? waterFull : waterEmpty;
            }
        }

    }

    void CalculateHarvestTime(Pot pot)
    {
        if (pot.GetPlantGrowing() == null)
        {
            timeUntilHarvest.text = "N/A";
            return;
        }
        
        string formattedTime = pot.GetFormattedTime();
        if (formattedTime == "Ready!")
        {
            pot.SetReadyToHarvest(true);
        }
        else
        {
            pot.SetReadyToHarvest(false);
        }
        timeUntilHarvest.text = formattedTime;

    }
    
    
    
    // HELPER FUNCS
    // Converts the float water value to an int within 0-5
    private int ConvertWaterValue(float value)
    {
        int roundedValue = Mathf.CeilToInt(value);
        int clampedValue = Mathf.Clamp(roundedValue, 0, waterStatus.Length);
        return clampedValue;
    }
    

    private void OnDestroy()
    {
        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(false);
        }
    }
}
