using Fusion;
using UnityEngine;
using Cinemachine;

public class BindLocalCamera : NetworkBehaviour
{
    [SerializeField] private Transform cameraTarget;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) return;

        var rig = CameraRig.I;
        if (rig == null || rig.vcam == null)
        {
            Debug.LogError("[BindLocalCamera] CameraRig/vcam missing.");
            return;
        }

        rig.vcam.Follow = cameraTarget;
        rig.vcam.LookAt = cameraTarget;

        // Force Cinemachine to snap immediately to new target
        // Without this it lerps from the old position and swings wildly
        rig.vcam.OnTargetObjectWarped(cameraTarget, 
            cameraTarget.position - rig.vcam.transform.position);

        // Also force the brain to cut to this camera instantly
        var brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
            brain.ActiveBlend?.Duration.Equals(0f); // disable current blend

        Debug.Log($"[BindLocalCamera] Bound vcam to {cameraTarget.name}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!Object.HasInputAuthority) return;

        var rig = CameraRig.I;
        if (rig?.vcam != null)
        {
            rig.vcam.Follow = null;
            rig.vcam.LookAt = null;
        }
    }
}
