using UnityEngine;
using System.Collections.Generic;

public class ScannableDevcaTarget : MonoBehaviour
{
    [Header("Button3D Link")]
    [SerializeField] private Button3D button3D;  // Plant Button Holder
    [SerializeField] private int buttonIndex;    // index in Button3D.buttonObjects

    public bool IsScannable { get; private set; } = true;

    // Global list – lets the player prefab find all scannables
    public static readonly List<ScannableDevcaTarget> Instances = new List<ScannableDevcaTarget>();

    private void OnEnable()
    {
        Instances.Add(this);

        if (button3D != null)
            button3D.onButtonPressed.AddListener(OnButtonPressed);
    }

    private void OnDisable()
    {
        Instances.Remove(this);

        if (button3D != null)
            button3D.onButtonPressed.RemoveListener(OnButtonPressed);
    }

    private void OnButtonPressed(int index)
    {
        // If this index was pressed with E,
        // this target stops affecting the scanner decal.
        if (index == buttonIndex)
        {
            IsScannable = false;
        }
    }
}
