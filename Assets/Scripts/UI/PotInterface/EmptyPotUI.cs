using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmptyPotUI : MonoBehaviour
{
    public static EmptyPotUI  Instance { get; private set;  }

    [Header("UI References")] 
    [SerializeField] private GameObject potInterfacePanel;
    [SerializeField] private Button plantSomethingButton;
    [SerializeField] private TextMeshProUGUI title;
    
    [Header("Technical")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private Pot currentlySelectedPot;

    private void Awake()
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
        
        if(potInterfacePanel == null) Debug.LogError("EmptyPotUI: No UI Assigned!");
    }

    void Start()
    {
        if (potInterfacePanel != null)
        {
            potInterfacePanel.SetActive(false);
        }

        title.text = "Ceramic Pot";
        SetupButtonListeners();
    }

    private void Update()
    {
        if (potInterfacePanel.activeSelf)
        {
            if (Input.GetKeyDown(closeKey))
            {
                ClosePanel();
            }
        }
    }

    void SetupButtonListeners()
    {
        plantSomethingButton.onClick.AddListener(() =>
        {
            PlantSomething();
        });
    }

    public void OnEmptyPotClicked(Pot clickedPot)
    {
        if (currentlySelectedPot != null && currentlySelectedPot != clickedPot)
        {
            currentlySelectedPot.SetSelectedVisual(false);
        }

        currentlySelectedPot = clickedPot;

        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(true);
            OpenPanel(currentlySelectedPot);
        }
        else
        {
            ClosePanel();
        }
    }

    private void OpenPanel(Pot pot)
    {
        title.text = "Ceramic Pot";
        
        if (potInterfacePanel != null)
        {
            potInterfacePanel.SetActive(true); 
        }
        
        // Title Size
        string potSizeString = "Ceramic Pot";
        switch (pot.GetPotSize())
        {
            case 0: potSizeString = "Small"; break;
            case 1: potSizeString = "Medium"; break;
            case 2: potSizeString = "Large"; break;
        }

        title.text = $"{potSizeString} Ceramic Pot";
    }

    private void ClosePanel()
    {
        if (potInterfacePanel != null)
        {
            potInterfacePanel.SetActive(false);
        }

        if (currentlySelectedPot != null)
        {
            currentlySelectedPot.SetSelectedVisual(false);
            currentlySelectedPot = null;
        }
        title.text = "Ceramic Pot";
    }

    private void PlantSomething()
    {
        Debug.Log($"{currentlySelectedPot.GetPotID()}: Plant Something Called");
        
        if (PlantSomethingUI.Instance != null)
        {
            PlantSomethingUI.Instance.OnPlantSomethingRequested(currentlySelectedPot);
        }
        else
        {
            Debug.LogError("EmptyPotUI: PlantSomethingUI.Instance is null!");
        }
        
        ClosePanel();
    }
}

