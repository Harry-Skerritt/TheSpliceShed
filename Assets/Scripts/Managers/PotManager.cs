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
    [SerializeField] private Transform[] potLocations;
    [SerializeField] private int potsToCreate;

    private List<Pot> potList = new List<Pot>();
    private Dictionary<string, Pot> potDict = new Dictionary<string, Pot>();



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
        
        InitalisePots();
    }

    private void InitalisePots()
    {
        ClearAllPotsInScene(); // Make this keep their data

        int actualPotsToCreate = Mathf.Min(potsToCreate, potLocations.Length);

        for (int i = 0; i < actualPotsToCreate; i++)
        {
            Transform location = potLocations[i];
            if (location != null)
            {
                GameObject potGO = Instantiate(potPrefab, location.position, Quaternion.identity);
                Pot potComponent = potGO.GetComponent<Pot>();

                if (potComponent != null)
                {
                    string newPotID = "Pot_" + System.Guid.NewGuid().ToString().Substring(0, 8);
                    potGO.name = newPotID;
                    potComponent.SetPotID(newPotID);
                    potComponent.SetPotColours(selectedColour, hoverColour);
                    potComponent.SetupTooltip(mainCanvas, potTooltip);
                    potList.Add(potComponent);
                    potDict.Add(newPotID, potComponent);
                    Debug.Log($"PotManager: {potGO.name} created at {location.position} successfully!");
                }
                else
                {
                    Debug.LogError($"PotManager: Prefab is missing a 'Pot' component! Pot at {location.position} not initialised!");
                    Destroy(potGO);
                }
            }
            else
            {
                Debug.LogWarning($"PotManager: Unable to create pot at index {i} as location is null!");
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
        if (data == null || data.pots == null)
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
            Pot potComponent = potGO.GetComponent<Pot>();

            if (potComponent != null)
            {
                potGO.name = loadedPotState.potId;
                potComponent.SetPotID(loadedPotState.potId);
                potComponent.SetPotColours(selectedColour, hoverColour);
                potComponent.SetPotState(loadedPotState);
                potComponent.SetupTooltip(mainCanvas, potTooltip);
                
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
        
        int potsToAdd = potsToCreate - potList.Count;
        if (potsToAdd > 0)
        {
            Debug.Log($"PotManager: Loaded {potList.Count} pots, need to add {potsToAdd} more to reach potsToCreate minimum.");
            for (int i = 0; i < potsToAdd; i++)
            {
                // Find an available potLocation or just instantiate at origin if all are used
                Transform location = (i < potLocations.Length) ? potLocations[i] : null;
                Vector3 spawnPos = (location != null) ? location.position : Vector3.zero;

                GameObject potGO = Instantiate(potPrefab, spawnPos, Quaternion.identity, this.transform);
                Pot potComponent = potGO.GetComponent<Pot>();

                if (potComponent != null)
                {
                    string newPotID = "Pot_" + System.Guid.NewGuid().ToString().Substring(0, 8);
                    potGO.name = newPotID;
                    potComponent.SetPotID(newPotID);
                    potComponent.SetPotColours(selectedColour, hoverColour);
                    potComponent.SetupTooltip(mainCanvas, potTooltip);
                    // Pot will be initialized as empty by default
                    potList.Add(potComponent);
                    potDict.Add(newPotID, potComponent);
                    Debug.Log($"PotManager: Added new default pot '{newPotID}'.");
                }
                else
                {
                    Debug.LogError($"PotManager: PotPrefab is missing a 'Pot' component! Cannot add default pot.");
                    Destroy(potGO);
                }
            }
        }
    }
}
