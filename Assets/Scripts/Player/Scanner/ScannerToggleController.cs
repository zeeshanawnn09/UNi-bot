using System;
using UnityEngine;

public class ScannerToggleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RayconeScanner scanner;
    [SerializeField] private GameObject scannerDecal;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;

    [Header("Audio")]
    [SerializeField] private AudioSource scannerAudioSource; // assign in Inspector (looping scan clip)

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

        HandleScannerAudio(on);

        OnScannerToggled?.Invoke(on);
    }

    private void HandleScannerAudio(bool on)
    {
        if (scannerAudioSource == null)
            return;

        if (on)
        {
            if (!scannerAudioSource.isPlaying)
                scannerAudioSource.Play();
        }
        else
        {
            if (scannerAudioSource.isPlaying)
                scannerAudioSource.Stop();
        }
    }
}
