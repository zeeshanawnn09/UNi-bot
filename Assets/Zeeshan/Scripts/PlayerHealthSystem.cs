using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthSystem : MonoBehaviour
{
    [SerializeField] private Slider healthBarSlider;
    
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private float depletionInterval = 10f;
    private float timeSinceLastDepletion = 0f;
    private bool isInCheckpoint = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Only deplete health if not in checkpoint
        if (!isInCheckpoint)
        {
            timeSinceLastDepletion += Time.deltaTime;

            if (timeSinceLastDepletion >= depletionInterval)
            {
                DepleteHealth();
                timeSinceLastDepletion = 0f;
            }
        }

        UpdateHealthBar();
    }

    private void DepleteHealth()
    {
        currentHealth -= maxHealth * 0.1f; // Deplete by 10%
        currentHealth = Mathf.Max(0f, currentHealth); // Ensure health doesn't go below 0
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth / maxHealth;
        }
    }

    public float GetHealthPercentage()
    {
        return (currentHealth / maxHealth) * 100f;
    }

    public void RechargeHealth(float percentageAmount)
    {
        currentHealth += maxHealth * (percentageAmount / 100f); // Recharge by percentage
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Ensure health doesn't exceed max
    }

    public void SetCheckpointState(bool inCheckpoint)
    {
        isInCheckpoint = inCheckpoint;
        if (inCheckpoint)
        {
            timeSinceLastDepletion = 0f; // Reset depletion timer when entering checkpoint
        }
    }
}
