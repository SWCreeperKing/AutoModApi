namespace AutoModApi.Attributes.Api;

public class ApiArgumentAttribute : Attribute
{
    public string[] methodNames;

    public ApiArgumentAttribute(string methodName)
    {
        methodNames = new[] { methodName };
    }
    
    public ApiArgumentAttribute(params string[] methodNames)
    {
        this.methodNames = methodNames;
    }
}