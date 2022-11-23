using AutoModApi;
using AutoModApi.Attributes.Api;
using AutoModApi.Attributes.Documentation;

namespace ApiTester;

[Api("item"), Document("Foundation of all items")]
public class Item : ApiScript
{
    [Document("test generics")] public Dictionary<string, List<Item>> dict = new();

    [Document("prop test 1")] public string prop1 { get; set; }
    [Document("prop test 2")] public string prop2 { private get; set; }
    [Document("prop test 3")] public string prop3 { get; private set; }
    [Document("prop test 4")] private string prop4 { get; set; }

    [Document("Name of item")] public string name = "Unknown Item";
    [Document("test var")] public int i;
    [DocIgnore] public int hardness;

    [Document("documentary type check")] public long l;
    [Document("documentary type check")] public float f;
    [Document("documentary type check")] public bool b;
    [Document("documentary type check")] public byte bt;
    [Document("documentary type check")] public decimal dc;
    [Document("documentary type check")] public double d;

    [Api("use"), Document("Called when item is used")]
    public void OnUse() => Execute("use", new UseArgs(this));

    [DocIgnore]
    public void Ignored()
    {
        
    }
    
    public void Empty()
    {
    }
    
    [Document("When player decides to jump")]
    public void OnPlayerJump(Player player, float height, int block)
    {
        Execute("OnPlayerJump", new JumpArguments(this, player, height, block));
    }

    [Document("Interaction with another item")]
    public void Interact([Document("item interacted with")] Item item)
    {
    }

    public void MiscMethod(int i, Item ii, params string[] sArr)
    {
    }

    [ApiArgument("use")] public record UseArgs(Item This);

    [ApiArgument("OnPlayerJump")]
    public record JumpArguments(Item This, Player Player, float Height, [Document("Block jumped from")] int Block);
}