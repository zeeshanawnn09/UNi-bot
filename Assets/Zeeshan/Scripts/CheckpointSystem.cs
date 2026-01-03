using UnityEngine;
using UnityEngine.UI;

public class CheckpointSystem : MonoBehaviour
{
    [SerializeField] private Collider checkpoint;
    [SerializeField] private Text healthFullText;
    [SerializeField] private PlayerHealthSystem playerHealthSystem;
    [SerializeField] private string playerTag = "Player";
    
    private float rechargeInterval = 5f;
    private float timeSinceLastRecharge = 0f;
    private bool playerInCheckpoint = false;

    private void Start()
    {
        if (healthFullText != null)
        {
            healthFullText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInCheckpoint)
        {
            timeSinceLastRecharge += Time.deltaTime;

            if (timeSinceLastRecharge >= rechargeInterval)
            {
                RechargePlayerHealth();
                timeSinceLastRecharge = 0f;
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
                    timeSinceLastRecharge = 0f;
                    
                    // Notify PlayerHealthSystem that player is in checkpoint
                    if (playerHealthSystem != null)
                    {
                        playerHealthSystem.SetCheckpointState(true);
                    }
                }
                
                if (playerHealthSystem != null)
                {
                    // Check if health is full
                    if (playerHealthSystem.GetHealthPercentage() >= 100f)
                    {
                        if (healthFullText != null)
                        {
                            healthFullText.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        if (healthFullText != null)
                        {
                            healthFullText.gameObject.SetActive(false);
                        }
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
            timeSinceLastRecharge = 0f;
            
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

    private bool IsPlayerInCheckpoint(Collider playerCollider)
    {
        // Check if player is within the checkpoint
        if (checkpoint != null && checkpoint.bounds.Contains(playerCollider.bounds.center))
        {
            return true;
        }
        return false;
    }

    private void RechargePlayerHealth()
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.RechargeHealth(5f); // Recharge by 5%
        }
    }
}
