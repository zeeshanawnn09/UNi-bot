using UnityEngine;

public class ScanableObject : MonoBehaviour, IScannable
{
    
    public void OnScanEnter(object scanner)
    {
        Debug.Log($"{gameObject.name} scanned by {scanner}");
    }
    public void OnScanStay(object scanner, float dt)
    {
        Debug.Log($"{gameObject.name} is being scanned by {scanner} for {dt} seconds");
    }
    public void OnScanExit(object scanner)
    {
        Debug.Log($"{gameObject.name} scan exited by {scanner}");
    }

}
