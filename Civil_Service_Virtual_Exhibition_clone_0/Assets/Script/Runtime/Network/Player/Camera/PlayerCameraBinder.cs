using System.Collections;
using UnityEngine;
using Fusion;

public class PlayerCameraBinder : NetworkBehaviour
{
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