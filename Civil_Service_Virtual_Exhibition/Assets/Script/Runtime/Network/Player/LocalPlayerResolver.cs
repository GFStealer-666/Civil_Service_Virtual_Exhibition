using UnityEngine;

public static class LocalInteractorUtility
{
    public static bool IsLocalPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        Transform root = other.transform.root;
        return root.CompareTag("LocalPlayer");
    }

    public static GameObject GetLocalPlayer()
    {
        return GameObject.FindGameObjectWithTag("LocalPlayer");
    }
}