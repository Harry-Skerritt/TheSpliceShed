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
                if(currentlySelectedPot.GetPotStatus() != PotStatus.Empty)
                {
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
        waterButton.onClick.AddListener( () =>
        {
            WaterPlants();
        });

        harvestButton.onClick.AddListener(() =>
        {
            HarvestPlants();
        });
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
            case 0: potSizeString = "Small"; break;
            case 1: potSizeString = "Medium"; break;
            case 2: potSizeString = "Large"; break;
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

        long harvestTimeSeconds = GetTimeUntilHarvest(pot);
        string formattedTime = FormatTime(harvestTimeSeconds);
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

    // Calculate the harvest time
    private long GetTimeUntilHarvest(Pot pot)
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogError("TimeManager is null. Cannot calculate harvest time.");
            return -1;
        }

        if (pot.GetPlantGrowing() == null)
        {
            return -1;
        }


        long absoluteBirthTimeS
            = (long)pot.GetBirthDay() * TimeManager.SECONDS_IN_A_DAY + (long)pot.GetBirthTime();
        
        long absoluteCurrentS = (long)TimeManager.Instance.CurrentDay * TimeManager.SECONDS_IN_A_DAY + (long)TimeManager.Instance.CurrentTimeInSeconds;
        
        long growthTimeS = (long)(pot.GetPlantGrowing().growthTime * TimeManager.SECONDS_IN_A_DAY);

        long timeRemainingS;
        
        if (absoluteBirthTimeS < growthTimeS)
        {
            // New Plant
            timeRemainingS = growthTimeS - absoluteCurrentS;
            return (long)Mathf.Max(0, timeRemainingS);
        }
        
        long absoluteLastHarvestS = (long)pot.GetLastHarvestD() * TimeManager.SECONDS_IN_A_DAY + (long)pot.GetLastHarvestS();
        long absoluteTargetHarvestS = absoluteLastHarvestS + growthTimeS;
        timeRemainingS = absoluteTargetHarvestS - absoluteCurrentS;
            
        return (long)Mathf.Max(0, timeRemainingS);
    }
    
    // Takes the time in seconds until harvest and formats it to "HH:MM"
    private string FormatTime(long totalSeconds)
    {
        if (totalSeconds < 0) return "Overdue!";
        if (totalSeconds == 0) return "Ready!";

        long days = totalSeconds / TimeManager.SECONDS_IN_A_DAY;
        long remainingSeconds = totalSeconds % TimeManager.SECONDS_IN_A_DAY;

        long hours = remainingSeconds / TimeManager.SECONDS_IN_AN_HOUR;
        remainingSeconds %= TimeManager.SECONDS_IN_AN_HOUR;

        long minutes = remainingSeconds / 60;
        
        string result = "";
        if (days > 0) result += $"{days}d ";
        result += $"{hours:00}:{minutes:00}"; // Always show HH:MM

        return result.Trim();
    }

    private void OnDestroy()
    {
        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(false);
        }
    }
}
