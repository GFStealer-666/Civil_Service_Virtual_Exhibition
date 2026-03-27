using System;
using UnityEngine;

public class PSC_FilterResolver : MonoBehaviour
{
    public bool TryResolve(string rawValue, out PSC_MinistryCategory option)
    {
        string normalized = Normalize(rawValue);

        switch (normalized)
        {
            case "เศรษฐกิจ":
            case "economy":
                option = PSC_MinistryCategory.Economy;
                return true;

            case "สังคม":
            case "social":
            case "society":
                option = PSC_MinistryCategory.Society;
                return true;

            case "ความมั่นคง":
            case "security":
                option = PSC_MinistryCategory.Security;
                return true;

            case "โครงสร้างพื้นฐาน":
            case "infrastructure":
                option = PSC_MinistryCategory.Infrastructure;
                return true;

            case "บริการ":
            case "service":
                option = PSC_MinistryCategory.Service;
                return true;
        }

        option = PSC_MinistryCategory.All;
        return false;
    }

    public bool Matches(PSC_ServiceMinistryDto ministry, PSC_MinistryCategory filter)
    {
        if (filter == PSC_MinistryCategory.All)
            return true;

        if (ministry == null)
            return false;

        return ministry.runtimeFilter == filter;
    }

    private string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}