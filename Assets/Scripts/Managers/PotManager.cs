using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PotManager : MonoBehaviour
{
    public static PotManager Instance { get; private set; }

    [Header("Pot Colours")]
    [SerializeField] private Color hoverColour;
    [SerializeField] private Color selectedColour;

    [Header("Tooltip")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private TextMeshProUGUI potTooltip;
    
    [Header("Pot Info")]
    [SerializeField] private GameObject potPrefab;
    //[SerializeField] private Transform[] potLocations;
    //[SerializeField] private int potsToCreate;

    private List<Pot> potList = new List<Pot>();
    private Dictionary<string, Pot> potDict = new Dictionary<string, Pot>();
    
    
    // Temp - Replace with the level manager
    private int currentLevel = 0;



    public void Awake()
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

        if (potPrefab == null)
        {
            Debug.LogError("PotManager: PotManager needs a PotPrefab!");
        }
    }

    private void InitalisePots()
    {
        ClearAllPotsInScene(); // Make this keep their data
        
       PotSpawnPoint[] spawnPoints = FindObjectsByType<PotSpawnPoint>(FindObjectsSortMode.None);
       Debug.Log(spawnPoints.Length);

       foreach (PotSpawnPoint point in spawnPoints)
       {
           if (point == null) continue;

           // Only spawn if player’s current level is high enough
           if (currentLevel < point.GetUnlockLevel)
           {
               Debug.Log($"PotManager: Skipping pot at {point.transform.position} - UnlockLevel {point.GetUnlockLevel} not reached.");
               continue;
           }

           GameObject potGO = Instantiate(potPrefab, point.transform.position, Quaternion.identity);
           
           // Refine this
           switch (point.GetPotSize)
           {
               case PotSize.Small:
                   potGO.transform.localScale = Vector3.one * 0.15f;
                   break;
               case PotSize.Medium:
                   potGO.transform.localScale = Vector3.one * 0.2f;
                   break;
               case PotSize.Large:
                   potGO.transform.localScale = Vector3.one * 0.25f;
                   break;
           }

           Pot potComponent = potGO.GetComponent<Pot>();
           if (potComponent != null)
           {
               string newPotID = "Pot_" + Guid.NewGuid().ToString().Substring(0, 8);
               potGO.name = newPotID;
               potComponent.SetPotID(newPotID);
               potComponent.SetPotColours(selectedColour, hoverColour);
               potComponent.SetupTooltip(mainCanvas, potTooltip);
               potComponent.SetPotSize(point.GetPotSize);
               potComponent.SetPotUnlockLevel(point.GetUnlockLevel);

               potList.Add(potComponent);
               potDict.Add(newPotID, potComponent);

               Debug.Log($"PotManager: {potGO.name} created at {point.transform.position} (Size {point.GetPotSize}, Unlock {point.GetUnlockLevel}).");
           }
           else
           {
               Debug.LogError($"PotManager: PotPrefab missing 'Pot' component! Destroying object.");
               Destroy(potGO);
           }
       }
    }

    private void ClearAllPotsInScene()
    {
        foreach (Pot pot in potList)
        {
            if (pot != null && pot.gameObject != null)
            {
                Destroy(pot.gameObject);
            }
        }

        potList.Clear();
        potDict.Clear();
    }

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentGameData != null)
        {
            SetPotData(SaveManager.Instance.CurrentGameData.potData);
            Debug.Log("PotManager: Loaded pot data from SaveManager");
        }
        else
        {
            Debug.LogWarning("PotManager: No pot save data found! Using intialised pots");
            InitalisePots();
        }
    }


    // Save System Integration
    [Serializable]
    public class PotData
    {
        public List<Pot.PotState> pots;

        public PotData()
        {
            pots = new List<Pot.PotState>();
        }
    }

    public PotData GetPotData()
    {
        PotData data = new PotData();
        foreach (Pot pot in potList)
        {
            if (pot != null)
            {
                data.pots.Add(pot.GetPotState());
            }
        }
        Debug.Log($"PotManager: Collected {data.pots.Count} pots to save");
        return data;
    }

    public void SetPotData(PotData data)
    {
        if (data == null || data.pots == null || data.pots.Count == 0)
        {
            Debug.LogWarning("PotManager: Attempted to set pot data will null or empty data. Using default pots");
            InitalisePots();
            return;
        }
        
        ClearAllPotsInScene();

        foreach (Pot.PotState loadedPotState in data.pots)
        {
            Vector3 loadedPosition =
                new Vector3(loadedPotState.positionX, loadedPotState.positionY, loadedPotState.positionZ);
            Quaternion loadedRotation = Quaternion.Euler(0, loadedPotState.rotationY, 0);

            GameObject potGO = Instantiate(potPrefab, loadedPosition, loadedRotation);
            
            switch (loadedPotState.potSize)
            {
                case PotSize.Small:
                    potGO.transform.localScale = Vector3.one * 0.15f;
                    break;
                case PotSize.Medium:
                    potGO.transform.localScale = Vector3.one * 0.2f;
                    break;
                case PotSize.Large:
                    potGO.transform.localScale = Vector3.one * 0.25f;
                    break;
            }
            
            Pot potComponent = potGO.GetComponent<Pot>();

            if (potComponent != null)
            {
                potGO.name = loadedPotState.potId;
                potComponent.SetPotID(loadedPotState.potId);
                potComponent.SetPotColours(selectedColour, hoverColour);
                potComponent.SetPotState(loadedPotState);
                potComponent.SetupTooltip(mainCanvas, potTooltip);
                potComponent.SetPotSize(loadedPotState.potSize);
                potComponent.SetPotUnlockLevel(loadedPotState.unlockLevel);
                
                potList.Add(potComponent);
                potDict.Add(loadedPotState.potId, potComponent);
                Debug.Log($"PotManager: Loaded pot '{loadedPotState.potId}' at {loadedPosition}.");
            }
            else
            {
                Debug.LogError($"PotManager: PotPrefab is missing 'Pot' component!");
                Destroy(potGO);
            }
        }
    }
}
