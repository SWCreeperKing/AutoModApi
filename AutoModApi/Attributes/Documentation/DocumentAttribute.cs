namespace AutoModApi.Attributes.Documentation;

public class DocumentAttribute : Attribute
{
    public readonly string documentation;

    public DocumentAttribute(string documentation)
    {
        this.documentation = documentation;
    }
}