using Fusion;
using UnityEngine;

public enum PlayerGender
{
    Male = 0,
    Female = 1,
}

public class PlayerProfile : NetworkBehaviour
{
    [Networked, Capacity(24)]
    public string PlayerName { get => default; set { } }

    [Networked]
    public PlayerGender Gender { get; set; }

    [Networked]
    public NetworkBool ProfileReady { get; set; }

    public void ApplyProfile(string playerName, PlayerGender gender)
    {
        if (!HasStateAuthority)
            return;

        PlayerName = playerName;
        Gender = gender;
        ProfileReady = true;
    }
}