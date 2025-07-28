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


        public GameData()
        {
            // Creates defaults is there is no actual data
            timeData = new TimeManager.TimeData();
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
        Debug.Log("Saving game...");
        
        // Get time data
        if (TimeManager.Instance != null)
        {
            CurrentGameData.timeData = TimeManager.Instance.GetTimeData();
        }
        else
        {
            Debug.LogWarning("No time manager found - Time data NOT saved!");
        }

        try
        {
            string json = JsonUtility.ToJson(CurrentGameData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Game saved to: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save the game: {e.Message}");
        }
    }

    public bool LoadGame()
    {
        Debug.Log("Loading game...");
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                CurrentGameData = JsonUtility.FromJson<GameData>(json);
                Debug.Log($"Game loaded from: {savePath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load the game: {e.Message}");
                CurrentGameData = new GameData();
                return false;
            }
        }
        else
        {
            Debug.LogWarning("No save file found! Starting new game!");
            CurrentGameData = new GameData();
            return false;
        }
    }

    public void DeleteSaveGame()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"Game deleted from: {savePath}");
            CurrentGameData = new GameData();
        }
        else
        {
            Debug.LogWarning("No save file found!");
        }
    }
}
