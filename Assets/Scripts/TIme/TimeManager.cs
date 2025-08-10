using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    
    [Header("Time Settings")] 
    [Tooltip("Real world seconds per in game minute")] 
    [Range(0.1f, 10.0f)] [SerializeField] private float baseTimeScale = 1.0f;
    
    [Tooltip("When hour when the day starts")]
    [Range(0, 23)][SerializeField] private int startHour = 6;

    private int startMinute = 0; // Always start at 0600
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timeTextObject;
    [SerializeField] private Button factor1xButton;
    [SerializeField] private Button factor2xButton;
    [SerializeField] private Button factor3xButton;
    [SerializeField] private Color selectedColor;
    [SerializeField] private int currentScale = 1; // Default 1

    private float currentTimeInSeconds;
    private int currentHour;
    private int currentMinute;
    private int currentDay = 1; // Start Day
    
    // Allows getting of time values
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;
    public int CurrentDay => currentDay;
    public float CurrentTimeInSeconds => currentTimeInSeconds;
    
    // Events
    public delegate void OnHourChanged(int hour);
    public static event OnHourChanged onHourChanged;

    public delegate void OnDayChanged(int day);
    public static event OnDayChanged onDayChanged;
    
    public const int SECONDS_IN_A_DAY = 86400;
    public const int SECONDS_IN_AN_HOUR = 3600;

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
        // Load time data from a save
        if (SaveManager.Instance != null && SaveManager.Instance.LoadGame())
        {
            SetTimeData(SaveManager.Instance.CurrentGameData.timeData);
            Debug.Log("TimeManager: Loaded time data");
        }
        else
        {
            Debug.Log("TimeManager: Initializing new time data");
            // Init time to the set values
            currentTimeInSeconds = (startHour * 3600f) + (startMinute * 60f);
            currentHour = startHour;
            currentMinute = startMinute;
        }

        // Init and update UI
        SetupScaleButtons();
        UpdateScaleButtons();
        UpdateTimeDisplay();
        
        // Make sure starting time is sent
        onHourChanged?.Invoke(currentHour);
        onDayChanged?.Invoke(currentDay);
    }

    void Update()
    {
        currentTimeInSeconds += Time.deltaTime * (60f * (baseTimeScale * currentScale));
        
        int oldHour = currentHour;
        int oldMinute = currentMinute;

        currentHour = (int)(currentTimeInSeconds / 3600f) % 24; 
        currentMinute = (int)(currentTimeInSeconds / 60f) % 60;

        // Check for a new day
        if (currentTimeInSeconds >= 24f * 3600f)
        {
            currentDay++;
            currentTimeInSeconds = 0;
            Debug.Log($"TimeManager: New Day! Days: {currentDay}");
            onDayChanged?.Invoke(currentDay);
        }

        // Check for a new hour
        if (oldHour != currentHour)
        {
            Debug.Log($"TimeManager: Hour changed! Hour: {currentHour}");
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
            timeTextObject.text = $"Day {currentDay} {displayHour:00}:{currentMinute:00} {ampm}";
        }
    }

    void SetupScaleButtons()
    {
        factor3xButton.onClick.AddListener(() =>
        {
            currentScale = 3;
            UpdateScaleButtons();
        });

        factor2xButton.onClick.AddListener(() =>
        {
            currentScale = 2;
            UpdateScaleButtons();
        });

        factor1xButton.onClick.AddListener(() =>
        {
            currentScale = 1;
            UpdateScaleButtons();
        });
    }

    void UpdateScaleButtons()
    {
        factor1xButton.image.color = Color.white;
        factor2xButton.image.color = Color.white;
        factor3xButton.image.color = Color.white;
        
        Debug.Log($"TimeManager: Scale: {currentScale}");
        
        if (currentScale == 1)
        {
            factor1xButton.image.color = selectedColor;
        } 
        else if (currentScale == 2)
        {
            factor2xButton.image.color = selectedColor;
        } 
        else if (currentScale == 3)
        {
            factor3xButton.image.color = selectedColor;
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
            Debug.LogWarning("TimeManager: Days to skip must be a positive number.");
            return;
        }

        for (int i = 0; i < daysToSkip; i++)
        {
            currentDay++;
            Debug.Log($"TimeManager: Skipped to Day: {currentDay}");
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
        public float td_currentTimeInSeconds;
        public int td_currentHour;
        public int td_currentMinute;
        public int td_currentDay;
    }

    public TimeData GetTimeData()
    {
        return new TimeData()
        {
            td_currentTimeInSeconds = this.currentTimeInSeconds,
            td_currentHour = this.currentHour,
            td_currentMinute = this.currentMinute,
            td_currentDay = this.currentDay
        };
    }

    public void SetTimeData(TimeData data)
    {
        if (data == null)
        {
            Debug.LogWarning("TimeManager: SetTimeData: data is null.");
            Debug.LogError($"TimeManager: {data}");
            return;
        }
        
        
        this.currentTimeInSeconds = data.td_currentTimeInSeconds;
        this.currentHour = data.td_currentHour;
        this.currentMinute = data.td_currentMinute;
        this.currentDay = data.td_currentDay;
        
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
