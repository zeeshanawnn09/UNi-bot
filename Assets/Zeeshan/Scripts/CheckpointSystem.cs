using UnityEngine;
using UnityEngine.UI;

public class CheckpointSystem : MonoBehaviour
{
    [SerializeField] private Collider checkpoint;
    [SerializeField] private Text healthFullText;
    [SerializeField] private PlayerHealthSystem playerHealthSystem;
    [SerializeField] private string playerTag = "Player";

    [SerializeField] private float rechargeRatePerSecond = 1f; // percent per second

    private bool playerInCheckpoint = false;

    private void Start()
    {
        if (healthFullText != null)
        {
            healthFullText.text = "Hold 'E' to re-charge";
            healthFullText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInCheckpoint && Input.GetKey(KeyCode.E) && playerHealthSystem != null)
        {
            // Recharge continuously while E is held, scaled by deltaTime
            float amount = rechargeRatePerSecond * Time.deltaTime;
            bool canRecharge = playerHealthSystem.GetHealthPercentage() < 100f;
            if (canRecharge)
            {
                RechargePlayerHealth(amount);
            }
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        // Check if the colliding object is the player
        if (collision.CompareTag(playerTag) && checkpoint != null)
        {
            // Check if the player is within the checkpoint
            if (checkpoint.bounds.Contains(collision.bounds.center))
            {
                if (!playerInCheckpoint)
                {
                    playerInCheckpoint = true;

                    // Notify PlayerHealthSystem that player is in checkpoint
                    if (playerHealthSystem != null)
                    {
                        playerHealthSystem.SetCheckpointState(true);
                    }
                }

                if (playerHealthSystem != null)
                {
                    // Show prompt only if player can gain health
                    bool canRecharge = playerHealthSystem.GetHealthPercentage() < 100f;
                    if (healthFullText != null)
                    {
                        healthFullText.gameObject.SetActive(canRecharge);
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        // Check if the player left
        if (collision.CompareTag(playerTag))
        {
            playerInCheckpoint = false;
            
            // Notify PlayerHealthSystem that player left checkpoint
            if (playerHealthSystem != null)
            {
                playerHealthSystem.SetCheckpointState(false);
            }
            
            if (healthFullText != null)
            {
                healthFullText.gameObject.SetActive(false);
            }
        }
    }

    private void RechargePlayerHealth(float amount)
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.RechargeHealth(amount);
        }
    }
}
