using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // Game Data to save to a file
    [Serializable]
    public class GameData
    {
        // Save the current day and time
        public TimeManager.TimeData timeData;
        public InventoryManager.InventoryData inventoryData;
        public PotManager.PotData potData;


        public GameData()
        {
            // Creates defaults is there is no actual data
            timeData = new TimeManager.TimeData();
            inventoryData = new InventoryManager.InventoryData();
            potData = new PotManager.PotData();
        }
    }
    
    public GameData CurrentGameData { get; private set; }

    private string saveFileName = "gamesave.json"; // Make it use current time and date (real world)
    private string savePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            Debug.Log($"Save path: {savePath}");
            
            CurrentGameData = new GameData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame()
    {
        Debug.Log("SaveManager: Attempting to save game...");
        
        // Get time data
        if (TimeManager.Instance != null)
        {
            CurrentGameData.timeData = TimeManager.Instance.GetTimeData();
        }
        else
        {
            Debug.LogWarning("SaveManager: No TimeManager found - Time data NOT saved!");
        }

        // Get inventory data
        if (InventoryManager.Instance != null)
        {
            CurrentGameData.inventoryData = InventoryManager.Instance.GetInventoryData();
        }
        else
        {
            Debug.LogWarning("SaveManager: No InventoryManager found - Inventory data NOT saved!");    
        }
        
        // Get pot data
        if (PotManager.Instance != null)
        {
            CurrentGameData.potData = PotManager.Instance.GetPotData();
        }
        else
        {
            Debug.LogWarning("SaveManager: No PotManager found - Pot data NOT saved!");    
        }
        

        try
        {
            string json = JsonUtility.ToJson(CurrentGameData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"SaveManager: Game saved to: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: Failed to save the game: {e.Message}");
        }
    }

    public bool LoadGame()
    {
        Debug.Log("SaveManager: Loading game...");
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                CurrentGameData = JsonUtility.FromJson<GameData>(json);
                Debug.Log($"SaveManager: Game loaded successfully from: {savePath}");
                Debug.Log($"SaveManager: Loaded Time Data - Day: {CurrentGameData.timeData.td_currentDay}, Hour: {CurrentGameData.timeData.td_currentHour}");
                Debug.Log($"SaveManager: Loaded Inventory Slots Count: {CurrentGameData.inventoryData.slots.Count}");
                Debug.Log($"SaveManager: Loaded Pot Data Slots Count: {CurrentGameData.potData.pots.Count}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SaveManager: Failed to load the game: {e.Message}");
                CurrentGameData = new GameData();
                return false;
            }
        }
        else
        {
            Debug.LogWarning("SaveManager: No save file found! Starting new game!");
            CurrentGameData = new GameData();
            return false;
        }
    }

    public void DeleteSaveGame()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"SaveManager: Game deleted from: {savePath}");
            CurrentGameData = new GameData();
        }
        else
        {
            Debug.LogWarning("SaveManager: No save file found!");
        }
    }
}
