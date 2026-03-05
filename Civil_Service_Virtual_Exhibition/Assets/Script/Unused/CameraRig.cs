using UnityEngine;
using Cinemachine;

public class CameraRig : MonoBehaviour
{
    public static CameraRig I;
    public CinemachineVirtualCamera vcam;

    private void Awake() => I = this;
}