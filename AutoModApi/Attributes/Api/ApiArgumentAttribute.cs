namespace AutoModApi.Attributes.Api;

public class ApiArgumentAttribute : Attribute
{
    public string methodName;

    public ApiArgumentAttribute(string methodName)
    {
        this.methodName = methodName;
    }
}