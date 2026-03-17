using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Room")]
    public RoomDefinition roomDefinition;

    [Tooltip("0 = base room, 1 = _1, 2 = _2 ...")]
    public int roomIndex = 0;

    public Color color = Color.green;

    public string GetRoomName()
    {
        if (roomDefinition == null)
            return "";

        return roomIndex == 0
            ? roomDefinition.roomName
            : $"{roomDefinition.roomName}_{roomIndex}";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}