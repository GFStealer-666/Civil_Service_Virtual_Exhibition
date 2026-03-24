using UnityEngine;

public static class LocalPlayerResolver
{
    public static bool IsLocalPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        Player player = other.GetComponentInParent<Player>();
        return player != null && player.HasInputAuthority;
    }

    public static GameObject GetLocalPlayerByTag()
    {
        return GameObject.FindGameObjectWithTag("LocalPlayer");
    }
}