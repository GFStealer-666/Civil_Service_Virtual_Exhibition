using UnityEngine;
using Fusion;

public class PlayerCameraBinder : NetworkBehaviour
{
    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        if (Camera.main == null)
            return;

        ThirdPersonCameraFollow follow = Camera.main.GetComponent<ThirdPersonCameraFollow>();
        if (follow == null)
            return;

        Player player = GetComponent<Player>();
        if (player == null)
            return;

        follow.SetTarget(player);
    }
}