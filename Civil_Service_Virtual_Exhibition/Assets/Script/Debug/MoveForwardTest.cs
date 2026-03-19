using UnityEngine;

public class MoveForwardTest : MonoBehaviour
{
    [SerializeField] private float speed = 2f;

    private void Update()
    {
        transform.position += Vector3.forward * speed * Time.deltaTime;
    }
}