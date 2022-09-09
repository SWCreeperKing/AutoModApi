namespace AutoModApi.Attributes.Api;

public class ApiAttribute : Attribute
{
    public string overriderName;

    public ApiAttribute(string overriderName = "")
    {
        this.overriderName = overriderName;
    }
}