using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevConsole : MonoBehaviour
{
    public static DevConsole Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("The GameObject that holds the the console UI")] [SerializeField]
    private GameObject consolePanel;
    [Tooltip("The InputField where commands are typed")] [SerializeField]
    private TMP_InputField consoleInputField;
    [Tooltip("The Text element where console output is displayed")] [SerializeField]
    private TextMeshProUGUI consoleOutputText;

    private bool consoleVisible;
    
    public bool ConsoleVisible => consoleVisible;
    
    private const int MAX_LINES = 12;

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
        
        if (consoleInputField == null) Debug.LogError("DevConsole: Console Input Field is not assigned!");
        if (consoleOutputText == null) Debug.LogError("DevConsole: Console Output Text is not assigned!");
    }

    void Start()
    {
        if (consolePanel != null)
        {
            consolePanel.SetActive(false);
            consoleVisible = false;
        }

        consoleOutputText.text = "";
        DevLog("Dev Console Ready. Press ` to toggle");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Debug.Log("` Pressed");
            ToggleConsole();
        }
    }

    public void ToggleConsole()
    {
        if (consolePanel != null)
        {
            bool isActive = !consolePanel.activeSelf;
            consolePanel.SetActive(isActive);
            consoleVisible = isActive;

            if (isActive)
            {
                consoleInputField.ActivateInputField();
                consoleInputField.Select();
            }
            else
            {
                consoleInputField.DeactivateInputField();
                consoleInputField.text = "";
            }
        }
    }

    public void HandleInput(string inputFromEvent)
    {
        string actualInput = string.IsNullOrWhiteSpace(inputFromEvent) ? consoleInputField.text : inputFromEvent;
        
        if (string.IsNullOrWhiteSpace(actualInput))
        {
            consoleInputField.text = "";
            Debug.Log("DevConsole: Console Input Field is empty!");
            return;
        }
        
        Debug.Log("DevConsole: " + actualInput);
        DevLog("> " + actualInput);
        ParseCommand(actualInput);
        consoleInputField.text = "";
        consoleInputField.ActivateInputField();
    }

    public void ParseCommand(string command)
    {
        string[] parts = command.Trim().Split(' ');
        string cmd = parts[0].ToLower();

        if (cmd.StartsWith("/"))
        {
            cmd = cmd.Substring(1);
        }

        switch (cmd)
        {
            case "give":
                HandleGiveCommand(parts);
                break;
            case "clear":
                HandleClearCommand();
                break;
            default:
                DevLog($"Unknown command: '{cmd}'.");
                break;
        }
    }

    private void HandleGiveCommand(string[] parts)
    {
        if (parts.Length < 3)
        {
            DevLog("Usage: /give [itemName] [quantity]");
            return;
        }
        
        string itemName = parts[1];
        int quantity;

        if (!int.TryParse(parts[2], out quantity))
        {
            DevLog("Error: Quantity must be a number.");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            DevLog("Error: Inventory Manager is null.");
            return;
        }
        
        ItemData itemData = Resources.Load<ItemData>("Items/" + itemName);

        if (itemData == null)
        {
            DevLog($"Error: ItemData '{itemName}' not found!");
            return;
        }

        if (InventoryManager.Instance.AddItem(itemData, quantity))
        {
            DevLog($"Gave {quantity} x {itemData.name}.");
        }
        else
        {
            DevLog($"Failed to give {quantity} x {itemData.name}.");
        }
    }

    private void HandleClearCommand()
    {
        if (InventoryManager.Instance == null)
        {
            DevLog("Error: Inventory Manager is null.");
            return;
        }

        InventoryManager.Instance.ClearInventory();
        DevLog("Inventory cleared.");
    }

    public void DevLog(string message)
    {
        if (consoleOutputText == null) return;
        
        consoleOutputText.text += message + "\n";
        
        string[] lines = consoleOutputText.text.Split('\n');
        if (lines.Length > MAX_LINES)
        {
            consoleOutputText.text = string.Join("\n", lines.Skip(lines.Length - MAX_LINES).ToArray());
        }
    }
}
