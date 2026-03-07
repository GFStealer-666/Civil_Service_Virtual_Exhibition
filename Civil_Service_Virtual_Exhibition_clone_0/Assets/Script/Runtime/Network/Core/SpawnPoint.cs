using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public Color color;
    public void OnDrawGizmos()
    {
        Gizmos.color = this.color;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
