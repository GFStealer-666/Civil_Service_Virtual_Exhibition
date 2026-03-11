using Fusion;
using UnityEngine;

public class ExhibitionBootstrap : FusionBootstrap
{
    protected override void Start()
    {
#if UNITY_SERVER
        AutoStartAs = GameMode.Server;
        StartMode   = StartModes.Automatic;
        Debug.Log("[ExhibitionBootstrap] Starting as Dedicated Server");
#elif UNITY_WEBGL
        AutoStartAs = GameMode.Client;
        StartMode   = StartModes.Automatic;
        Debug.Log("[ExhibitionBootstrap] Starting as WebGL Client");
#else
        // Editor — keep whatever is set in Inspector for testing
        Debug.Log($"[ExhibitionBootstrap] Editor/Window mode: {AutoStartAs}");
#endif
        base.Start();
    }
}