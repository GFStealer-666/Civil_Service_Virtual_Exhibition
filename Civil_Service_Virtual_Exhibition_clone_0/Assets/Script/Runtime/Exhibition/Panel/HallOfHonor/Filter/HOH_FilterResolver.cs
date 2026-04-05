using System;
using UnityEngine;

public class HOH_FilterResolver : MonoBehaviour
{
    public bool TryResolve(string rawValue, out HOH_FilterOption option)
    {
        string normalized = Normalize(rawValue);

        switch (normalized)
        {
            case "เศรษฐกิจ":
            case "economy":
                option = HOH_FilterOption.Economy;
                return true;

            case "สังคม":
            case "society":
                option = HOH_FilterOption.Social;
                return true;

            case "ความมั่นคง":
            case "security":
                option = HOH_FilterOption.Security;
                return true;

            case "โครงสร้างพื้นฐาน":
            case "infrastructure":
                option = HOH_FilterOption.Infrastructure;
                return true;

            case "บริการ":
            case "service":
                option = HOH_FilterOption.Service;
                return true;

            case "เหนือ":
            case "north":
                option = HOH_FilterOption.North;
                return true;

            case "ตะวันออกเฉียงเหนือ":
            case "northeast":
                option = HOH_FilterOption.Northeast;
                return true;

            case "กลาง":
            case "central":
                option = HOH_FilterOption.Central;
                return true;

            case "ตะวันออก":
            case "east":
                option = HOH_FilterOption.East;
                return true;

            case "ตะวันตก":
            case "west":
                option = HOH_FilterOption.West;
                return true;

            case "ใต้":
            case "south":
                option = HOH_FilterOption.South;
                return true;
        }

        option = HOH_FilterOption.All;
        return false;
    }

    public bool Matches(HOH_UnitDto unit, HOH_FilterOption filter)
    {
        if (filter == HOH_FilterOption.All)
            return true;

        if (unit == null)
            return false;

        return unit.runtimeFilter == filter;
    }

    private string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}