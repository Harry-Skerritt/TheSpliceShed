using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pot : MonoBehaviour
{
    [Header("Pot Details")] 
    [SerializeField] [Range(0, 2)] private int potSize = 1;                 // Pot Size: 0: Small, 1: Normal, 2: Large
    [Tooltip("The itemData plant present")]
    [SerializeField] private ItemData plantGrowing;                         // ItemData of the planted plant
    [SerializeField] private bool potEmpty = true;                          // Is the pot empty
    [Range(0.0f, 5.0f)][SerializeField] private float waterLevel = 0.0f;    // The water level of the plant
    [SerializeField] private int plantAge;                                  // Age of the plant (in h or d)
    [SerializeField] private bool ageInHours = true;                        // Age in Hours (T) or Days (F)

    
    private string potId = "PotNone";                                                   // Unique Pot ID
    private string plantName;                                               // Name of plant
    private ItemType plantType;                                             // Type of plant
    private float birthTime;                                                // Time in seconds plant was potted
    private int birthDay;                                                   // Day the plant was potted
    
    private Color baseColour = Color.white;                              // Base colour of the pot (Always White)
    private Color selectedColour;                                           // Colour when pot selected
    private Color hoverColour;                                              // Colour when pot is hovered
    private SpriteRenderer spriteRenderer;                                  // Pots SpriteRenderer

    private PotManager potManager;

    public string GetPotID() => potId;


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
        
        spriteRenderer.color = baseColour;
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
    
    
    // Save System Integrations
    [Serializable]
    public struct PotState
    {
        public string potId;
        public int potSize;
        public string plantGrowingName;
        public bool potEmpty;
        public float waterLevel;
        public int plantAge;
        public bool ageInHours;
        public float birthTime;
        public int birthDay;
        public string plantName;
        public ItemType plantType;

        public float positionX, positionY, positionZ;
        public float rotationY;

        public PotState(Pot pot)
        {
            potId = pot.potId;
            potSize = pot.potSize;
            plantGrowingName = (pot.plantGrowing != null) ? pot.plantGrowing.itemName : "";
            potEmpty = pot.potEmpty;
            waterLevel = pot.waterLevel;
            plantAge = pot.plantAge;
            ageInHours = pot.ageInHours;
            birthTime = pot.birthTime;
            birthDay = pot.birthDay;
            plantName = pot.plantName;
            plantType = pot.plantType;

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
        waterLevel = state.waterLevel;
        plantAge = state.plantAge;
        ageInHours = state.ageInHours;
        birthTime = state.birthTime;
        birthDay = state.birthDay;
        plantName = state.plantName;
        plantType = state.plantType;

        // Load ItemData if a plant was growing
        if (!string.IsNullOrEmpty(state.plantGrowingName))
        {
            plantGrowing = Resources.Load<ItemData>(state.plantGrowingName);
            if (plantGrowing == null)
            {
                Debug.LogWarning($"Pot {potId}: Could not load ItemData '{state.plantGrowingName}'. Plant will be empty.");
                potEmpty = true; // Mark as empty if item data can't be found
            }
        }
        else
        {
            plantGrowing = null;
            potEmpty = true;
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
