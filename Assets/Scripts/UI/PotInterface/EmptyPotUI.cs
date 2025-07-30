using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmptyPotUI : MonoBehaviour
{
    public static EmptyPotUI  Instance { get; private set;  }

    [Header("UI References")] 
    [SerializeField] private GameObject potInterfacePanel;
    [SerializeField] private Button plantSomethingButton;
    
    [Header("Technical")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
