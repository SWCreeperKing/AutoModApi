using AutoModApi;
using AutoModApi.Attributes.Api;
using AutoModApi.Attributes.Documentation;

namespace ApiTester;

[Api("item"), Document("Foundation of all items")]
public class Item : ApiScript
{
    [Document("Name of item")] public string name = "Unknown Item";
    [Document("test var")] public int i;
    public int hardness;

    [Api("use"), Document("Called when item is used")]
    public void OnUse() => Execute("use", new UseArgs(this));

    [Document("When player decides to jump")]
    public void OnPlayerJump(float height, int block)
    {
        Execute("OnPlayerJump", new JumpArguments(this, height, block));
    }

    [Document("Interaction with another item"), ArgumentOverride]
    public void Interact([Document("item interacted with")] Item item)
    {
    }

    [ApiArgument("use")] public record UseArgs(Item This);

    [ApiArgument("OnPlayerJump")]
    public record JumpArguments(Item This, float Height, [Document("Block jumped from")] int Block);
}