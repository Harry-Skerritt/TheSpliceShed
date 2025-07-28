using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")] 
    [Tooltip("Real world seconds per in game minute")] 
    [Range(0.1f, 10.0f)] [SerializeField] private float timeScale = 1.0f;
    
    [Tooltip("When hour when the day starts")]
    [Range(0, 23)][SerializeField] private int startHour = 6;

    private int startMinute = 0; // Always start at 0600
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeTextObject;

    private float currentTimeInSeconds;
    private int currentHour;
    private int currentMinute;
    private int currentDay = 1; // Start Day
    
    // Allows getting of time values
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;
    public int CurrentDay => currentDay;
    
    // Events
    public delegate void OnHourChanged(int hour);
    public static event OnHourChanged onHourChanged;

    public delegate void OnDayChanged(int day);
    public static event OnDayChanged onDayChanged;

    void Awake()
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
    }

    void Start()
    {
        // Init time to the set values
        currentTimeInSeconds = (startHour * 3600f) + (startMinute * 60f);
        currentHour = startHour;
        currentMinute = startMinute;

        // Init and update UI
        UpdateTimeDisplay();
        
        // Make sure starting time is sent
        onHourChanged?.Invoke(currentHour);
        onDayChanged?.Invoke(currentDay);
    }

    void Update()
    {
        currentTimeInSeconds += Time.deltaTime * (60f * timeScale);
        
        int oldHour = currentHour;
        int oldMinute = currentMinute;

        currentHour = (int)(currentTimeInSeconds / 3600f) % 24; 
        currentMinute = (int)(currentTimeInSeconds / 60f) % 60;

        // Check for a new day
        if (currentTimeInSeconds >= 24f * 3600f)
        {
            currentDay++;
            currentTimeInSeconds = 0;
            Debug.Log($"New Day! Days: {currentDay}");
            onDayChanged?.Invoke(currentDay);
        }

        // Check for a new hour
        if (oldHour != currentHour)
        {
            Debug.Log($"Hour changed! Hour: {currentHour}");
            onHourChanged?.Invoke(currentHour);
        }

        UpdateTimeDisplay();
    }

    void UpdateTimeDisplay()
    {
        if (timeTextObject != null)
        {
            string ampm = "";
            int displayHour = currentHour;
            
            // Convert to 12-hour format with AM/PM
            if (displayHour >= 12)
            {
                ampm = "PM";
                if (displayHour > 12)
                {
                    displayHour -= 12;
                }
            }
            else
            {
                ampm = "AM";
                if (displayHour == 0) // Special case for 12 AM (midnight)
                {
                    displayHour = 12;
                }
            }

            // Use string formatting to ensure two digits for hour and minute (e.g., 09:05)
            timeTextObject.text = $"Day {currentDay}\n{displayHour:00}:{currentMinute:00} {ampm}";
        }
    }

    public void SetTime(int hour, int minute)
    {
        hour = Mathf.Clamp(hour, 0, 23);
        minute = Mathf.Clamp(minute, 0, 59);

        currentHour = hour;
        currentMinute = minute;
        currentTimeInSeconds = (hour * 3600f) + (minute * 60f);
        
        UpdateTimeDisplay();
        onHourChanged?.Invoke(currentHour);
    }

    public void SetDay(int day)
    {
        if (currentDay >= 0)
        {
            currentDay = day;
        }
    }
    
    public void SkipDays(int daysToSkip)
    {
        if (daysToSkip <= 0)
        {
            Debug.LogWarning("Days to skip must be a positive number.");
            return;
        }

        for (int i = 0; i < daysToSkip; i++)
        {
            currentDay++;
            Debug.Log($"Skipped to Day: {currentDay}");
            onDayChanged?.Invoke(currentDay);
        }

        // After skipping days, ensure time is set to the start of the new day
        currentTimeInSeconds = (startHour * 3600f) + (startMinute * 60f);
        currentHour = startHour;
        currentMinute = startMinute;

        UpdateTimeDisplay(); 
        onHourChanged?.Invoke(currentHour); 
    }
    
    
    // Save System
    [Serializable]
    public class TimeData
    {
        public float currentTimeInSeconds;
        public int currentHour;
        public int currentMinute;
        public int currentDay;
    }

    public TimeData GetTimeData()
    {
        return new TimeData()
        {
            currentTimeInSeconds = this.currentTimeInSeconds,
            currentHour = this.currentHour,
            currentMinute = this.currentMinute,
            currentDay = this.currentDay
        };
    }

    public void SetTimeData(TimeData data)
    {
        if (data == null) return;
        
        this.currentTimeInSeconds = data.currentTimeInSeconds;
        this.currentHour = data.currentHour;
        this.currentMinute = data.currentMinute;
        this.currentDay = data.currentDay;
        
        UpdateTimeDisplay();
        onHourChanged?.Invoke(currentHour);
        onDayChanged?.Invoke(currentDay);
    }

    // Saves the game on quit 
    // Todo: Move this from this file
    private void OnApplicationQuit()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
    }
}
