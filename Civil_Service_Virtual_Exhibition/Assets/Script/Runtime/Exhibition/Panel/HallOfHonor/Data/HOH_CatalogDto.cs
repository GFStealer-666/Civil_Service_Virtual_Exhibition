using System;
using System.Collections.Generic;

public enum HOH_CategoryKind
{
    Unknown = 0,
    Ministry = 1,
    Province = 2,
    University = 3
}

[Serializable]
public class HOH_ResponseDto
{
    public bool success;
    public List<HOH_CategoryDto> data = new();
}

[Serializable]
public class HOH_CategoryDto
{
    public string type;
    public List<HOH_UnitDto> units = new();

    [NonSerialized] public string runtimeId;

    public HOH_CategoryKind GetCategoryKind()
    {
        return type switch
        {
            "กรม" => HOH_CategoryKind.Ministry,
            "จังหวัด" => HOH_CategoryKind.Province,
            "มหาวิทยาลัย" => HOH_CategoryKind.University,
            _ => HOH_CategoryKind.Unknown
        };
    }
}

[Serializable]
public class HOH_UnitDto
{
    public string unit;
    public string unitEn;
    public string ministry;
    public string ministryEn;
    public string logoUrl;
    public List<HOH_PersonDto> persons = new();

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string parentCategoryId;
}

[Serializable]
public class HOH_PersonDto
{
    public string id;
    public string type;
    public int groupNumber;

    public string prefix;
    public string prefixOther;
    public string firstName;
    public string lastName;

    public string serviceDuration;
    public string position;
    public string positionLevel;
    public string division;
    public string department;
    public string unit;
    public string ministry;

    public string education;
    public string institution;
    public string outstandingWork;
    public string photoUrl;

    public string prefixEn;
    public string firstNameEn;
    public string lastNameEn;

    public string serviceDurationEn;
    public string educationEn;
    public string institutionEn;
    public string positionEn;
    public string positionLevelEn;
    public string divisionEn;
    public string departmentEn;
    public string unitEn;
    public string ministryEn;
    public string outstandingWorkEn;

    public string createdAt;
    public string logoUrl;

    [NonSerialized] public string runtimeId;
    [NonSerialized] public string parentUnitId;

    public string GetFullNameTh()
    {
        string safePrefix = !string.IsNullOrWhiteSpace(prefixOther) ? prefixOther : prefix;
        return $"{safePrefix} {firstName} {lastName}".Trim();
    }

    public string GetFullNameEn()
    {
        return $"{prefixEn} {firstNameEn} {lastNameEn}".Trim();
    }
}