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
    
    [Header("Water")]
    [SerializeField] private Image[] waterStatus;
    [SerializeField] private Color waterEmpty;
    [SerializeField] private Color waterFull;
    
    [Header("Technical")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;
    [Range(0.0f, 0.6f)][SerializeField] private float buttonDisabledAlpha;
    
    private Pot currentlySelectedPot;
    
    // Buttons
    private Color waterButtonColour;
    private Color harvestButtonColour;
    private bool waterButtonActive = false;
    private bool harvestButtonActive = false;

    
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

            if (currentlySelectedPot != null)
            {
                if (!currentlySelectedPot.GetPotEmpty())
                {
                    if (currentlySelectedPot.GetWaterLevel() < waterStatus.Length)
                    {
                        waterButtonActive = true;
                        waterButton.GetComponent<Image>().color = waterButtonColour;
                    }
                    else
                    {
                        waterButtonActive = false;
                        Color buttonDisabledColour = waterButtonColour;
                        buttonDisabledColour.a = buttonDisabledAlpha;
                        waterButton.GetComponent<Image>().color = buttonDisabledColour;
                    }

                    if (currentlySelectedPot.GetReadyToHarvest())
                    {
                        harvestButtonActive = true;
                        harvestButton.GetComponent<Image>().color = harvestButtonColour;
                    }
                    else
                    {
                        harvestButtonActive = false;
                        Color buttonDisabledColour = harvestButtonColour;
                        buttonDisabledColour.a = buttonDisabledAlpha;
                        harvestButton.GetComponent<Image>().color = buttonDisabledColour;
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
    private void PopulatePanel(Pot pot)
    {
       ResetPanel();
       
       potInterfacePanel.SetActive(true);
       
        
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
        if (!pot.GetPotEmpty() && pot.GetPlantGrowing() != null)
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
            potInterfacePanel.SetActive(false);
        }
        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(false); // Tell the pot to unselect its visual
            currentlySelectedPot = null; // Clear the reference
        }
        ResetPanel(); // Ensure all fields are reset
    }

    private void WaterPlants()
    {
        if (waterButtonActive)
        {
            currentlySelectedPot.WaterPlant();
        }
    }

    private void HarvestPlants()
    {
        if (harvestButtonActive)
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
        
        long absoluteLastHarvestS =
            (long)pot.GetLastHarvestD() * TimeManager.SECONDS_IN_A_DAY + (long)pot.GetLastHarvestS();
            
        long absoluteCurrentS = (long)TimeManager.Instance.CurrentDay * TimeManager.SECONDS_IN_A_DAY + (long)TimeManager.Instance.CurrentTimeInSeconds;
        
        long growthTimeS = (long)(pot.GetPlantGrowing().growthTime * TimeManager.SECONDS_IN_A_DAY);

        long absoluteTargetHarvestS = absoluteLastHarvestS + growthTimeS;
        long timeRemainingS = absoluteTargetHarvestS - absoluteCurrentS;
            
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
}
