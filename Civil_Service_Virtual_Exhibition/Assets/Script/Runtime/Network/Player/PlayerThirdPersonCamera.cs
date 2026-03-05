using UnityEngine;
using Fusion;
using Cinemachine;

public class PlayerThirdPersonCamera : NetworkBehaviour
{
    public Transform CameraRoot;
    public Transform LookAtPoint;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            var vcam = FindObjectOfType<CinemachineVirtualCamera>();

            if (vcam != null)
            {
                Debug.Log("[PlayerThirdPersonCamera] Setting camera follow and look at targets for player: " + Object.InputAuthority);
                vcam.Follow = CameraRoot;
                vcam.LookAt = LookAtPoint;
            }
        }

    }
}