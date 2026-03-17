using UnityEngine;

public class PortalInteractionService : MonoBehaviour
{
    public RoomPortal CurrentPortal { get; private set; }
    public GameObject LocalPlayer { get; private set; }

    private void Update()
    {
        if (LocalPlayer == null)
            LocalPlayer = GameObject.FindGameObjectWithTag("LocalPlayer");

        CurrentPortal = FindBestPortalInRange();
    }

    private RoomPortal FindBestPortalInRange()
    {
        if (LocalPlayer == null)
            return null;

        RoomPortal[] portals = FindObjectsByType<RoomPortal>(FindObjectsSortMode.None);

        RoomPortal best = null;
        float bestDistance = float.MaxValue;

        foreach (var portal in portals)
        {
            if (!portal.IsLocalPlayerInRange())
                continue;

            float distance = Vector3.Distance(LocalPlayer.transform.position, portal.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = portal;
            }
        }

        return best;
    }
}