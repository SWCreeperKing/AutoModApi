namespace AutoModApi.Attributes.Documentation;

public class DocumentAttribute : Attribute
{
    public string documentation;

    public DocumentAttribute(string documentation)
    {
        this.documentation = documentation;
    }
}