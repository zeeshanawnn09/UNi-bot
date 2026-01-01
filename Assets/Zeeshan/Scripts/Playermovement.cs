using UnityEngine;

public class Playermovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        // Capture WASD / arrow input and normalize to keep diagonal speed consistent
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

        // Move in world space so forward is always Z+; adjust to camera space if needed later
        if (input.sqrMagnitude > 0f)
        {
            transform.Translate(input * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
