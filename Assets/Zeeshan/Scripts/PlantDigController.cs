using UnityEngine;

public class PlantDigController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private CameraController cameraController;

    [Header("Optional FX GameObjects")]
    [SerializeField] private GameObject diggingGameObject;
    [SerializeField] private GameObject plantingGameObject;

    [Header("Animator Setup")]
    [Tooltip("Name of the Animator state that plays when the Planting trigger is fired.")]
    [SerializeField] private string plantingStateName = "Planting";

    [Tooltip("Animator layer index where the planting state lives (usually 0).")]
    [SerializeField] private int layerIndex = 0;

    [Header("Cinematic Duration")]
    [Tooltip("If > 0, use this duration. If <= 0, use Animator state's length.")]
    [SerializeField] private float plantingCinematicDurationOverride = -1f;

    private int _plantingStateHash;
    private bool _cinematicActive = false;
    private float _cachedPlantingLength = 1f;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        cameraController = FindObjectOfType<CameraController>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (!animator)
        {
            Debug.LogError("PlantDigController: No Animator assigned or found.", this);
            enabled = false;
            return;
        }

        _plantingStateHash = Animator.StringToHash(plantingStateName);
        CachePlantingClipLength();
    }

    private void Update()
    {
        if (!animator || cameraController == null)
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool inPlanting =
            stateInfo.shortNameHash == _plantingStateHash ||
            stateInfo.IsName(plantingStateName);

        // Just entered Planting state
        if (inPlanting && !_cinematicActive)
        {
            _cinematicActive = true;

            float duration = plantingCinematicDurationOverride > 0f
                ? plantingCinematicDurationOverride
                : stateInfo.length > 0f ? stateInfo.length : _cachedPlantingLength;

            cameraController.StartCinematic(duration);

            if (diggingGameObject != null) diggingGameObject.SetActive(false);
            if (plantingGameObject != null) plantingGameObject.SetActive(true);
        }
        // Just exited Planting state
        else if (!inPlanting && _cinematicActive)
        {
            _cinematicActive = false;

            cameraController.EndCinematic();

            if (plantingGameObject != null) plantingGameObject.SetActive(false);
            if (diggingGameObject != null) diggingGameObject.SetActive(true);
        }
    }

    private void CachePlantingClipLength()
    {
        _cachedPlantingLength = 1f;

        if (animator.runtimeAnimatorController == null)
            return;

        var clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == plantingStateName)
            {
                _cachedPlantingLength = clips[i].length;
                break;
            }
        }
    }
}
