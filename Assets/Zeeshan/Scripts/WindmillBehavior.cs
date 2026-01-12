using UnityEngine;

public class WindmillBehavior : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        //transform.Rotate(0f, 20f * Time.deltaTime, 0f, Space.Self);
        transform.Rotate(0f, 10f * Time.deltaTime, 0f, Space.Self);

    }
}
