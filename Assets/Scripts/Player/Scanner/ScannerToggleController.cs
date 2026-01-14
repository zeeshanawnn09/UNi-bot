using System;
using UnityEngine;

public class ScannerToggleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RayconeScanner scanner;
    [SerializeField] private GameObject scannerDecal;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    public bool IsScannerOn { get; private set; }

    // Broadcast other scripts can subscribe to this
    public static event Action<bool> OnScannerToggled;

    private void Start()
    {
        // Ensure scanner starts off
        SetScanner(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetScanner(!IsScannerOn);
        }
    }

    private void SetScanner(bool on)
    {
        IsScannerOn = on;

        if (scanner != null)
            scanner.enabled = on;

        if (scannerDecal != null)
            scannerDecal.SetActive(on);

        OnScannerToggled?.Invoke(on);
    }
}
