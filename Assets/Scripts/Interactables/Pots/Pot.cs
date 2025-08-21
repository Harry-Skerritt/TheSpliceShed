using System;
using System.Collections;
using System.Net;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PotStatus
{
    Empty,
    Planted,
    ReadyToHarvest
}

public enum PotSize
{
    Small,
    Medium,
    Large
}

public enum GrowthStage
{
    None,
    Seedling,
    Vegetative,
    Flowering
}

public class Pot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerMoveHandler
{
    [Header("Pot Details")]
    //[SerializeField] [Range(0, 2)] private int potSize = 1;               // Pot Size: 0: Small, 1: Normal, 2: Large
    [SerializeField] private PotSize potSize = PotSize.Medium;
    [Tooltip("The itemData plant present")]
    [SerializeField] private ItemData plantGrowing;                         // ItemData of the planted plant
    [SerializeField] private PotStatus potStatus;                           // Enum holding the current status of the pot
    [Range(0.0f, 5.0f)][SerializeField] private float waterLevel;           // The water level of the plant
    [SerializeField] private int plantAge;                                  // Age of the plant (in h or d)
    [SerializeField] private bool ageInHours = true;                        // Age in Hours (T) or Days (F)
    [SerializeField] private int unlockLevel;                               // Level at which this pot unlocks
    [SerializeField] private GrowthStage growthStage = GrowthStage.None;    // Growth Stage of the pot

    [Header("UI & Visuals")]
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Canvas mainUICanvas;
    [SerializeField] private ParticleSystem readyToHarvestParticles;
    [SerializeField] private ParticleSystem noWaterParticles;
    [SerializeField] private ParticleSystem wateredParticles;
    [SerializeField] private SpriteRenderer growthRenderer;
    [SerializeField] private SpriteRenderer potCoverRenderer;
    
    [Header("Sorting Layers")]
    [SerializeField] private string initialSortingLayer;
    [SerializeField] private string selectedSortingLayer;
    [SerializeField] private string particleInitialSortingLayer;
    [SerializeField] private string particleSelectedSortingLayer;

    private RectTransform tooltipRect;
    private bool tooltipActiveOnThisPot;
    
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

    private Sprite[] currentGrowthStageSprite;                                // The sprite for the current growth stage

    private PotManager potManager;
    
    public PotSize GetPotSize() => potSize;
    public ItemData GetPlantGrowing() => plantGrowing;
    public PotStatus GetPotStatus() => potStatus;
    public float GetWaterLevel() => waterLevel;
    public int GetPlantAge() => plantAge;
    public bool GetAgeInHours() => ageInHours;
    public float GetBirthTime() => birthTime;
    public int GetBirthDay() => birthDay;
    public string GetPotID() => potId;
    public float GetLastHarvestS() => lastHarvestS;
    public int GetLastHarvestD() => lastHarvestD;
    public int GetUnlockLevel() => unlockLevel;
    public GrowthStage GetGrowthStage() => growthStage;



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
        SetSortingLayers(initialSortingLayer, particleInitialSortingLayer);
        
        if (tooltipText != null)
        {
            tooltipRect = tooltipText.GetComponent<RectTransform>();
            tooltipText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError($"{potId}: Tooltip TextMeshProUGUI is not assigned");
        }
    }

    private void SetSortingLayers(string layerName, string layerNameParticles)
    {
        spriteRenderer.sortingLayerName = layerName;
        noWaterParticles.GetComponent<ParticleSystemRenderer>().sortingLayerName = layerNameParticles;
        readyToHarvestParticles.GetComponent<ParticleSystemRenderer>().sortingLayerName = layerNameParticles;
    }

    private void Start()
    {
        if (readyToHarvestParticles != null) readyToHarvestParticles.Stop();
        if (noWaterParticles != null) noWaterParticles.Stop();
        if (wateredParticles != null) wateredParticles.Stop();
    }

    private void Update()
    {

        if ((potStatus == PotStatus.Empty) || TimeManager.Instance == null || plantGrowing == null)
        {
            UpdateParticles();
            UpdateGrowthRenderer();
            if (tooltipActiveOnThisPot)
            {
                tooltipText.text = GetTooltipText();
            }
            return;
        }

        UpdateParticles();
        
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

        if (potStatus == PotStatus.ReadyToHarvest || waterLevel <= 0.0f)
        {
            // Dont grow
            Debug.Log($"{potId}: Growth Halted, ReadyToHarvest is true or Water is at 0!");
        }
        else
        {
            // Drain Water
            if (TimeManager.Instance.CurrentHour != lastCalculatedHour)
            {
                waterLevel = Mathf.Max(0.0f, waterLevel - plantGrowing.drainRate);
                lastCalculatedHour = TimeManager.Instance.CurrentHour;
                Debug.Log($"{potId}: Water drained to {waterLevel}. Plant age: {plantAge} {(ageInHours ? "hours" : "days")}");
            }
            
            // Calc Growth
            long timeUntilHarvest = GetTimeUntilHarvest();
            long totalGrowthTime = (long)plantGrowing.growthTime * TimeManager.SECONDS_IN_A_DAY;

            if (timeUntilHarvest <= 0)
            {
                potStatus = PotStatus.ReadyToHarvest;
                growthStage = GrowthStage.Flowering;
            }
            else if (timeUntilHarvest < (totalGrowthTime / 3))
            {
                growthStage = GrowthStage.Flowering;
            }
            else if (timeUntilHarvest < (timeUntilHarvest / 2))
            {
                growthStage = GrowthStage.Vegetative;
            }
            else
            {
                growthStage = GrowthStage.Seedling;
            }
        }
        
        UpdateGrowthRenderer();

        if (tooltipActiveOnThisPot)
        {
            tooltipText.text = GetTooltipText();
        }
    }

    public void SetReadyToHarvest(bool ready)
    {
        if (ready)
        {
            potStatus = PotStatus.ReadyToHarvest;
            growthStage = GrowthStage.Flowering;
        }
        else
        {
            // Assume pot still has plant
            potStatus = PotStatus.Planted;
        }
        
        UpdateGrowthRenderer();
    }
    
    public bool PlantItem(ItemData plant)
    {
        if (potStatus != PotStatus.Empty)
        {
            Debug.LogWarning($"{potId}: Pot is not empty! Cannot plant new item!");
            return false;
        }

        if (plant == null)
        {
            Debug.LogWarning($"{potId}: Cannot plant empty item!");
            return false;
        }

        if (plant.requiredPotSize > potSize)
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

        currentGrowthStageSprite = plant.growthSprites;
        
        plantGrowing = plant;       // Set plant itemData
        potStatus = PotStatus.Planted;
        growthStage = GrowthStage.Seedling;
        UpdateGrowthRenderer();
        plantName = plant.itemName; // Set plant name
        plantType = plant.itemType; // Set plant type

        waterLevel = 5.0f;          // Reset water level to 5
        plantAge = 0;               // New plant so age 0
        ageInHours = true;          // Until plant a day old this is true

        Debug.Log($"{potId}: Successfully planted {plant.itemName}");
        return true;
    }

    public void WaterPlant()
    {

        float newWaterLevel = waterLevel + 1.0f;

        if (newWaterLevel != waterLevel)
        {
            wateredParticles.Emit(75);
        }
        
        waterLevel = Mathf.Clamp(newWaterLevel, 0.0f, 5.0f);
        UpdateParticles();
    }
    
    public void HarvestItem()
    {
        if (potStatus != PotStatus.ReadyToHarvest)
        {
            Debug.LogWarning($"{potId}: Plant not ready for harvest");
            return;
            // Shouldnt happen
        }
        
        // Set the last harvest time
        lastHarvestS = TimeManager.Instance.CurrentTimeInSeconds;
        lastHarvestD = TimeManager.Instance.CurrentDay;
        
        waterLevel = Mathf.Max(0.0f, waterLevel - 2.0f); // Reduce Water by 2 //Todo: Decide whether to keep this

        potStatus = PotStatus.Planted; // Not ready to harvest
        growthStage = GrowthStage.Seedling;
        UpdateGrowthRenderer();
        
        InventoryManager.Instance.AddItem(plantGrowing, 1); //Todo: Change quantity to be random per level of plant
    }

    public void UnplantItem() 
    { 
        if (potStatus == PotStatus.ReadyToHarvest)
        {
            HarvestItem();
        }
        else if (potStatus == PotStatus.Planted && plantGrowing != null)
        {
            InventoryManager.Instance.AddItem(plantGrowing, 1);
        }
        
        lastHarvestS = 0;
        lastHarvestD = 0;
        potStatus = PotStatus.Empty;
        birthDay = 0;
        birthTime = 0;
        plantAge = 0;
        ageInHours = true;
        plantName = "";
        plantType = ItemType.Misc;
        plantGrowing = null;
        growthStage = GrowthStage.None;
        UpdateGrowthRenderer();
        
        readyToHarvestParticles.Stop();
        noWaterParticles.Stop();
    }
    
    // Interaction Handling
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isCurrentlySelected)
        {
            if(hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(LerpColour(hoverColour));
            ShowTooltip();
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (tooltipRect != null && mainUICanvas != null)
        {
            Vector2 mouseLocalPos;
            Camera uiCamera = (mainUICanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainUICanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainUICanvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                uiCamera,
                out mouseLocalPos);

            tooltipRect.anchoredPosition = new Vector2(mouseLocalPos.x, mouseLocalPos.y + 20);
        }

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isCurrentlySelected)
        {
            if(hoverCoroutine != null) StopCoroutine(hoverCoroutine);
            hoverCoroutine = StartCoroutine(LerpColour(baseColour));
            HideTooltip();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"{potId}: Pot Clicked!");
            if ((potStatus == PotStatus.Empty))
            {
                if (PlantSomethingUI.Instance != null)
                {
                    PlantSomethingUI.Instance.OnPotClicked(this);
                    if (InventoryManager.Instance != null)
                    {
                        InventoryManager.Instance.SetInventoryVisible(true);
                    }
                    else
                    {
                        Debug.LogWarning($"{potId}: InventoryManager.Instance is null");
                    }
                }
                else
                {
                    Debug.LogWarning($"{potId}: PlantSomethingUI.Instance is null");
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

            if (potStatus == PotStatus.ReadyToHarvest)
            {
                HarvestItem();
            } else if (potStatus == PotStatus.Planted)
            {
                WaterPlant();
            }
            
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
            potCoverRenderer.color = selectedColour;
            SetSortingLayers(selectedSortingLayer, particleSelectedSortingLayer);
            tooltipText.gameObject.SetActive(false);
            CameraZoomController.Instance.ZoomIn(gameObject);
        }
        else
        {
            spriteRenderer.color = baseColour;
            potCoverRenderer.color = baseColour;
            SetSortingLayers(initialSortingLayer, particleInitialSortingLayer);
            CameraZoomController.Instance.ZoomOut();
        }
    }

    private IEnumerator LerpColour(Color colour)
    {
        Color startColour = spriteRenderer.color;
        float elapsedTime = 0.0f;
        
        while (elapsedTime < colourChangeDuration)
        {
            spriteRenderer.color = Color.Lerp(startColour, colour, elapsedTime / colourChangeDuration);
            potCoverRenderer.color = Color.Lerp(startColour, colour, elapsedTime / colourChangeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = colour;
        potCoverRenderer.color = colour;
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

    public void SetupTooltip(Canvas canvas, TextMeshProUGUI tooltip)
    {
        mainUICanvas = canvas;
        tooltipText = tooltip;
        
        if (tooltipText != null)
        {
            tooltipRect = tooltipText.GetComponent<RectTransform>();
            tooltipActiveOnThisPot = false;
        }
    }

    public void SetPotSize(PotSize size)
    {
        potSize = size;
    }

    public void SetPotUnlockLevel(int level)
    {
        unlockLevel = level;
    }


    private void ShowTooltip()
    {
        if (tooltipText != null)
        {
            tooltipText.text = GetTooltipText();
            tooltipText.gameObject.SetActive(true);
            tooltipActiveOnThisPot = true;
        }
        else
        {
            Debug.LogWarning($"{potId}: Tooltip TextMeshProUGUI is null");
        }
    }

    private void HideTooltip()
    {
        if (tooltipText != null)
        {
            tooltipText.gameObject.SetActive(false);
            tooltipActiveOnThisPot = false;
        }
        else
        {
            Debug.LogWarning($"{potId}: Tooltip TextMeshProUGUI is null");
        }
    }

    private string GetTooltipText()
    {
        if (potStatus == PotStatus.Empty)
        {
            return "Empty Pot";
        }
        
        if (plantGrowing == null)
        {
            return "Error: No Plant Data"; 
        }
        
        if (waterLevel <= 0.0f)
        {
            return $"No water! | {plantName}";
        }
        
        if (potStatus == PotStatus.ReadyToHarvest)
        {
            return $"Harvest {plantName}";
        }
        
        if (potStatus == PotStatus.Planted)
        {
            return $"{plantName} | {GetFormattedTime()}";
        }

        return "Unknown Status";
    }

    private void UpdateParticles()
    { 
        bool isPlantedAndNotWatered = potStatus == PotStatus.Planted && waterLevel == 0.0f;

        if (potStatus == PotStatus.Empty)
        {
            if (readyToHarvestParticles != null && readyToHarvestParticles.isPlaying) readyToHarvestParticles.Stop();
            if (noWaterParticles != null && noWaterParticles.isPlaying) noWaterParticles.Stop();
            return;
        }
        
        if (readyToHarvestParticles != null)
        {
            if (potStatus == PotStatus.ReadyToHarvest)
            {
                if (!readyToHarvestParticles.isPlaying) readyToHarvestParticles.Play();
            }
            else
            {
                if (readyToHarvestParticles.isPlaying) readyToHarvestParticles.Stop();
            }
        }
        
        if (noWaterParticles != null)
        {
            if (isPlantedAndNotWatered)
            {
                if (!noWaterParticles.isPlaying) noWaterParticles.Play();
            }
            else
            {
                if (noWaterParticles.isPlaying) noWaterParticles.Stop();
            }
        }
    }

    void UpdateGrowthRenderer()
    {
        if (currentGrowthStageSprite == null || growthRenderer == null)
        {
            if(growthRenderer != null) growthRenderer.sprite = null;
            return;
        }
        
        switch (growthStage)
        {
            case GrowthStage.None:
                growthRenderer.sprite = null;
                break;
            case GrowthStage.Seedling:
                if (currentGrowthStageSprite.Length > 0)
                    growthRenderer.sprite = currentGrowthStageSprite[0];
                break;
            case GrowthStage.Vegetative:
                if (currentGrowthStageSprite.Length > 1)
                    growthRenderer.sprite = currentGrowthStageSprite[1];
                break;
            case GrowthStage.Flowering:
                if (currentGrowthStageSprite.Length > 2)
                    growthRenderer.sprite = currentGrowthStageSprite[2];
                break;
        }
    }
    
    // Calculate the harvest time
    public long GetTimeUntilHarvest()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogError("TimeManager is null. Cannot calculate harvest time.");
            return -1;
        }

        long absoluteBirthTimeS
            = (long)birthDay * TimeManager.SECONDS_IN_A_DAY + (long)birthTime;
        
        long absoluteCurrentS = (long)TimeManager.Instance.CurrentDay * TimeManager.SECONDS_IN_A_DAY + (long)TimeManager.Instance.CurrentTimeInSeconds;
        
        long growthTimeS = (long)(plantGrowing.growthTime * TimeManager.SECONDS_IN_A_DAY);

        long timeRemainingS;
        
        if (absoluteBirthTimeS < growthTimeS)
        {
            // New Plant
            timeRemainingS = growthTimeS - absoluteCurrentS;
            return (long)Mathf.Max(0, timeRemainingS);
        }
        
        long absoluteLastHarvestS = (long)lastHarvestD * TimeManager.SECONDS_IN_A_DAY + (long)lastHarvestS;
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

    public string GetFormattedTime()
    {
        return FormatTime(GetTimeUntilHarvest());
    }


    // Save System Integrations
    [Serializable]
    public struct PotState
    {
        public string potId;
        public PotSize potSize;
        public string plantGrowingName;
        public PotStatus potStatus;
        public float waterLevel;
        public int plantAge;
        public bool ageInHours;
        public float birthTime;
        public int birthDay;
        public string plantName;
        public ItemType plantType;
        public float lastHarvestS;
        public int lastHarvestD;
        public int lastCalculatedHour;
        public int unlockLevel;
        public GrowthStage growthStage;

        public float positionX, positionY, positionZ;
        public float rotationY;

        public PotState(Pot pot)
        {
            potId = pot.potId;
            potSize = pot.potSize;
            plantGrowingName = (pot.plantGrowing != null) ? pot.plantGrowing.itemName : "";
            potStatus = pot.potStatus;
            waterLevel = pot.waterLevel;
            plantAge = pot.plantAge;
            ageInHours = pot.ageInHours;
            birthTime = pot.birthTime;
            birthDay = pot.birthDay;
            plantName = pot.plantName;
            plantType = pot.plantType;
            lastHarvestS = pot.lastHarvestS;
            lastHarvestD = pot.lastHarvestD;
            lastCalculatedHour = pot.lastCalculatedHour;
            unlockLevel = pot.unlockLevel;
            growthStage = pot.growthStage;

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
        potStatus = state.potStatus;
        waterLevel = state.waterLevel;
        plantAge = state.plantAge;
        ageInHours = state.ageInHours;
        birthTime = state.birthTime;
        birthDay = state.birthDay;
        plantName = state.plantName;
        plantType = state.plantType;
        lastHarvestS = state.lastHarvestS;
        lastHarvestD = state.lastHarvestD;
        lastCalculatedHour = state.lastCalculatedHour;
        unlockLevel = state.unlockLevel;
        growthStage = state.growthStage;

        // Load ItemData if a plant was growing
        if (!string.IsNullOrEmpty(state.plantGrowingName))
        {
            plantGrowing = Resources.Load<ItemData>("Items/" + state.plantGrowingName);
            if (plantGrowing == null)
            { 
                Debug.LogWarning($"{potId}: Could not load ItemData '{state.plantGrowingName}'. Plant will be empty.");
                potStatus = PotStatus.Empty;
            }
            else
            {
                currentGrowthStageSprite = plantGrowing.growthSprites;
                UpdateGrowthRenderer();
                Debug.Log($"{potId}: Pot loaded with plant '{state.plantGrowingName}.");
            }
        }
        else
        {
            plantGrowing = null;
            potStatus = PotStatus.Empty;
        }

        
        this.transform.position = new Vector3(state.positionX, state.positionY, state.positionZ);
        this.transform.rotation = Quaternion.Euler(0, state.rotationY, 0);
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColour; 
        }
        
        UpdateParticles();
        if(tooltipActiveOnThisPot) ShowTooltip();
    }
}
