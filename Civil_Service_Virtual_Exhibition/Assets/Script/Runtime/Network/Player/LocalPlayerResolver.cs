using UnityEngine;

public static class LocalPlayerResolver
{
    public static bool IsOwnedByLocalClient(Player player)
    {
        if (player == null)
            return false;

        // If your Shared Mode setup uses HasStateAuthority instead,
        // change this line and keep the rest of the project the same.
        return player.HasInputAuthority;
    }

    public static Player GetLocalPlayer()
    {
        Player[] players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];

            if (IsOwnedByLocalClient(player))
                return player;
        }

        return null;
    }

    public static bool IsLocalPlayer(GameObject obj)
    {
        if (obj == null)
            return false;

        Player player = obj.GetComponentInParent<Player>();
        return IsOwnedByLocalClient(player);
    }

    public static bool IsLocalPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        Player player = other.GetComponentInParent<Player>();
        return IsOwnedByLocalClient(player);
    }
}