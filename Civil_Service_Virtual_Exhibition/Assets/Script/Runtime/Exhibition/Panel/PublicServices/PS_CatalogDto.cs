using System;

[Serializable]
public class PS_ServiceResponseDto
{
    public bool success;
    public PS_ServiceCategoryDto[] data;
    public int total;
}

[Serializable]
public class PS_ServiceCategoryDto
{
    public string category;
    public string categoryEn;
    public PS_ServiceActivityDto[] activities;
}

[Serializable]
public class PS_ServiceActivityDto
{
    public string id;
    public string department;
    public string category;
    public string activityName;
    public string activityDate;
    public string departmentEn;
    public string categoryEn;
    public string activityNameEn;
    public string activityDateEn;
    public string createdAt;
}