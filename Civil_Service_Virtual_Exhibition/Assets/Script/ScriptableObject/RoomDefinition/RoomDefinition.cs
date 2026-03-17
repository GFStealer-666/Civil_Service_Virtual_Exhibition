using UnityEngine;

[CreateAssetMenu(menuName = "Exhibition/Room Definition")]
public class RoomDefinition : ScriptableObject
{
    public string roomName;   
    public string sceneName; 

    public int maxPlayersPerRoom = 10;
    public int maxSubRooms       = 5;
}