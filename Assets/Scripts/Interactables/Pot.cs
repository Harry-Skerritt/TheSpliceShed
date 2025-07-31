using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Pot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Pot Details")] 
    [SerializeField] [Range(0, 2)] private int potSize = 1;                 // Pot Size: 0: Small, 1: Normal, 2: Large
    [Tooltip("The itemData plant present")]
    [SerializeField] private ItemData plantGrowing;                         // ItemData of the planted plant
    [SerializeField] private bool potEmpty = true;                          // Is the pot empty
    [SerializeField] private bool readyToHarvest = false;                   // Is the pot ready for harvest
    [Range(0.0f, 5.0f)][SerializeField] private float waterLevel = 0.0f;    // The water level of the plant
    [SerializeField] private int plantAge;                                  // Age of the plant (in h or d)
    [SerializeField] private bool ageInHours = true;                        // Age in Hours (T) or Days (F)
    [Range(0.0f, 1.0f)][SerializeField] private float drainRate = 0.5f;     // Rate at which pots drain water
    
    private string potId = "PotNone";                                       // Unique Pot ID
    private string plantName;                                               // Name of plant
    private ItemType plantType;                                             // Type of plant
    private float birthTime;                                                // Time in seconds plant was potted
    private int birthDay;                                                   // Day the plant was potted

    private float lastHarvestS;                                             // Time in seconds the last harvest happened
    private int lastHarvestD;                                               // Day last harvest happened
    
    private Color baseColour = Color.white;                              // Base colour of the pot (Always White)
    private Color selectedColour;                                           // Colour when pot selected
    private Color hoverColour;                                              // Colour when pot is hovered
    private float colourChangeDuration = 0.1f;                              // Speed of colour transition
    
    private SpriteRenderer spriteRenderer;                                  // Pots SpriteRenderer
    private Coroutine hoverCoroutine;                                       // Manages smooth colour transitions
    private bool isCurrentlySelected = false;                               // Tracks if the pot is selected

    private int lastCalculatedHour = -1;                                    // Tracks when age/water was calculated

    private PotManager potManager;
    
    public int GetPotSize() => potSize;
    public ItemData GetPlantGrowing() => plantGrowing;
    public bool GetPotEmpty() => potEmpty;
    public bool GetReadyToHarvest() => readyToHarvest;
    public float GetWaterLevel() => waterLevel;
    public int GetPlantAge() => plantAge;
    public bool GetAgeInHours() => ageInHours;
    public float GetBirthTime() => birthTime;
    public int GetBirthDay() => birthDay;
    public string GetPotID() => potId;
    public float GetLastHarvestS() => lastHarvestS;
    public int GetLastHarvestD() => lastHarvestD;



    private void Awake()
    {
        if (PotManager.Instance != null)
        {
            potManager = PotManager.Instance;
        }
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"{potId} SpriteRenderer is null");
                return;
            }
        }

        if (GetComponent<BoxCollider2D>() == null)
        {
            Debug.LogWarning($"{potId}: No BoxCollider2D found! Adding one. ");
            gameObject.AddComponent<BoxCollider2D>();
        }
        
        spriteRenderer.color = baseColour;
    }

    private void Update()
    {
        if (potEmpty || TimeManager.Instance == null) return;

        // Calc age
        long currentAbsoluteHours = (long)TimeManager.Instance.CurrentDay * 24 + TimeManager.Instance.CurrentHour;
        long birthAbsoluteHours = (long)birthDay * 24 + (long)(birthTime / TimeManager.SECONDS_IN_AN_HOUR);
        
        long elapsedHours = currentAbsoluteHours - birthAbsoluteHours;

        if (elapsedHours >= 24)
        {
            plantAge = (int)(elapsedHours / 24);
            ageInHours = false;
        }
        else
        {
            plantAge = (int)elapsedHours;
            ageInHours = true;
        }
        
        // Drain Water
        if (TimeManager.Instance.CurrentHour != lastCalculatedHour)
        {
            waterLevel = Mathf.Max(0.0f, waterLevel - drainRate);
            lastCalculatedHour = TimeManager.Instance.CurrentHour;
            Debug.Log($"{potId}: Water drained to {waterLevel}. Plant age: {plantAge} {(ageInHours ? "hours" : "days")}");
        }
    }

    public void SetReadyToHarvest(bool ready)
    {
        readyToHarvest = ready;
    }
    
    // Interaction Handling
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isCurrentlySelected)
        {
            if(hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(LerpColour(hoverColour));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isCurrentlySelected)
        {
            if(hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(LerpColour(baseColour));
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"{potId}: Pot Clicked!");
            if (potEmpty)
            {
                if (EmptyPotUI.Instance != null)
                {
                    EmptyPotUI.Instance.OnEmptyPotClicked(this);
                }
                else
                {
                    Debug.LogWarning($"{potId}: EmptyPotUI.Instance is null");
                }
            }
            else
            {
                if (PotUI.Instance != null)
                {
                    PotUI.Instance.OnPotClicked(this);
                }
                else
                {
                    Debug.LogWarning($"{potId}: PotUI.Instance is null");
                }
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right) 
        { 
            Debug.Log($"{potId}: Pot was right Clicked");   
        }
    }
    
    // State Control
    public void SetSelectedVisual(bool selected)
    {
        isCurrentlySelected = selected;
        if(hoverCoroutine != null) StopCoroutine(hoverCoroutine);

        if (selected)
        {
            spriteRenderer.color = selectedColour;
        }
        else
        {
            spriteRenderer.color = baseColour;
        }
    }

    private IEnumerator LerpColour(Color colour)
    {
        Color startColour = spriteRenderer.color;
        float elapsedTime = 0.0f;
        
        while (elapsedTime < colourChangeDuration)
        {
            spriteRenderer.color = Color.Lerp(startColour, colour, elapsedTime / colourChangeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = colour;
        hoverCoroutine = null;
    }

    public void SetPotColours(Color selected, Color hovered)
    {
        selectedColour = selected;
        hoverColour = hovered;
    }

    public void SetPotID(string id)
    {
        potId = id;
        gameObject.name = potId;
    }
    
    public bool PlantItem(ItemData plant)
    {
        if (!potEmpty)
        {
            Debug.LogWarning($"{potId}: Pot is not empty! Cannot plant new item!");
            return false;
        }

        if (plant == null)
        {
            Debug.LogWarning($"{potId}: Cannot plant empty item!");
            return false;
        }

        if (plant.requiredPotSize == -1 || plant.requiredPotSize > potSize)
        {
            Debug.LogWarning($"{potId}: Pot is too small for this Item!");
            return false;
        }

        if (TimeManager.Instance != null)
        {
            // Set the plants birthday
            birthTime = TimeManager.Instance.CurrentTimeInSeconds;
            birthDay = TimeManager.Instance.CurrentDay;
        }
        else
        {
            Debug.LogWarning($"{potId}: Unable to set plants birthday. Aborting!");
            return false;
        }
        
        plantGrowing = plant;       // Set plant itemData
        potEmpty = false;
        plantName = plant.itemName; // Set plant name
        plantType = plant.itemType; // Set plant type

        waterLevel = 0.0f;          // Reset water level to 0 (might remove)
        plantAge = 0;               // New plant so age 0
        ageInHours = true;          // Until plant a day old this is true

        Debug.Log($"{potId}: Successfully planted {plant.itemName}");
        return true;
    }

    public void WaterPlant()
    {
        float newWaterLevel = waterLevel + 1.0f;
        waterLevel = Mathf.Clamp(newWaterLevel, 0.0f, 5.0f);
    }
    
    public void HarvestItem()
    {
        // Set the last harvest time
        lastHarvestS = TimeManager.Instance.CurrentTimeInSeconds;
        lastHarvestD = TimeManager.Instance.CurrentDay;
        
        waterLevel = 0.0f; // Set water to 0

        InventoryManager.Instance.AddItem(plantGrowing, 1); //Todo: Change quantity to be random per level of plant

        // Todo: See what else needs to be in here
    }
    
    // Save System Integrations
    [Serializable]
    public struct PotState
    {
        public string potId;
        public int potSize;
        public string plantGrowingName;
        public bool potEmpty;
        public bool readyToHarvest;
        public float waterLevel;
        public int plantAge;
        public bool ageInHours;
        public float birthTime;
        public int birthDay;
        public string plantName;
        public ItemType plantType;
        public float lastHarvestS;
        public int lastHarvestD;
        public float drainRate;
        public int lastCalculatedHour;

        public float positionX, positionY, positionZ;
        public float rotationY;

        public PotState(Pot pot)
        {
            potId = pot.potId;
            potSize = pot.potSize;
            plantGrowingName = (pot.plantGrowing != null) ? pot.plantGrowing.itemName : "";
            potEmpty = pot.potEmpty;
            readyToHarvest = pot.readyToHarvest;
            waterLevel = pot.waterLevel;
            plantAge = pot.plantAge;
            ageInHours = pot.ageInHours;
            birthTime = pot.birthTime;
            birthDay = pot.birthDay;
            plantName = pot.plantName;
            plantType = pot.plantType;
            lastHarvestS = pot.lastHarvestS;
            lastHarvestD = pot.lastHarvestD;
            drainRate = pot.drainRate;
            lastCalculatedHour = pot.lastCalculatedHour;

            positionX = pot.transform.position.x;
            positionY = pot.transform.position.y;
            positionZ = pot.transform.position.z;
            rotationY = pot.transform.rotation.eulerAngles.y;
        }
    }

    public PotState GetPotState()
    {
        return new PotState(this);
    }

    public void SetPotState(PotState state)
    {
        potId = state.potId;
        potSize = state.potSize;
        potEmpty = state.potEmpty;
        readyToHarvest = state.readyToHarvest;
        waterLevel = state.waterLevel;
        plantAge = state.plantAge;
        ageInHours = state.ageInHours;
        birthTime = state.birthTime;
        birthDay = state.birthDay;
        plantName = state.plantName;
        plantType = state.plantType;
        lastHarvestS = state.lastHarvestS;
        lastHarvestD = state.lastHarvestD;
        drainRate = state.drainRate;
        lastCalculatedHour = state.lastCalculatedHour;

        // Load ItemData if a plant was growing
        if (!string.IsNullOrEmpty(state.plantGrowingName))
        {
            plantGrowing = Resources.Load<ItemData>("Items/" + state.plantGrowingName);
            if (plantGrowing == null)
            {
                Debug.LogWarning($"{potId}: Could not load ItemData '{state.plantGrowingName}'. Plant will be empty.");
                potEmpty = true; // Mark as empty if item data can't be found
                readyToHarvest = false;
            }
            else
            {
                Debug.Log($"{potId}: Pot loaded with plant '{state.plantGrowingName}.");
            }
        }
        else
        {
            plantGrowing = null;
            potEmpty = true;
            readyToHarvest = false;
        }
        
        // Might be unnecessary 
        this.transform.position = new Vector3(state.positionX, state.positionY, state.positionZ);
        this.transform.rotation = Quaternion.Euler(0, state.rotationY, 0); // Apply Y-axis rotation

        // Update visual state if necessary
        // For now, just ensure the base color is applied.
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColour; 
        }
    }
}
