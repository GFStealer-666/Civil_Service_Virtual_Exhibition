using System.Collections;
using UnityEngine;

public class LocalizationBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return LocalizationService.Initialize();
    }
}