using System.Collections;
using UnityEngine;
using Fusion;

public class PlayerCameraBinder : NetworkBehaviour
{
    [Header("Initial Camera Angle")]
    [Tooltip("Yaw (degrees) to force on first bind")]
    public float initialYaw = 146.061f;

    [Tooltip("Pitch (degrees) to force on first bind")]
    public float initialPitch = -8.2f;

    [Tooltip("Whether to force this orientation once after spawn")]
    public bool forceInitialAngleOnSpawn = true;

    private bool _initialAngleApplied;

    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        StartCoroutine(BindWhenCameraReady());
    }

    private IEnumerator BindWhenCameraReady()
    {
        float timeout = 3f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                ThirdPersonCameraFollow follow = mainCamera.GetComponent<ThirdPersonCameraFollow>();
                if (follow != null)
                {
                    Player player = GetComponent<Player>();
                    if (player != null)
                    {
                        follow.SetTarget(player);

                        if (!_initialAngleApplied && forceInitialAngleOnSpawn)
                        {
                            follow.ForceYawPitch(initialYaw, initialPitch);
                            _initialAngleApplied = true;
                        }

                        yield break;
                    }
                }
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[PlayerCameraBinder] Could not bind camera to local player.");
    }
}