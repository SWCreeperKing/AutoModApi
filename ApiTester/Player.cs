using AutoModApi;
using AutoModApi.Attributes.Documentation;

namespace ApiTester;

[Document("this is just to test auto-documentation")]
public class Player : ApiScript
{
    [EnumDoc, Document("Player's status")]
    public enum PlayerState
    {
        Healthy,
        Sick,
        Cursed
    }
    
    [Document("player hp")] public int hp;
    [Document("player's current status")] public PlayerState playerStat;
    [Document("player's current item")] public Item currentlyHeldItem;

    [Document("equip new item")]
    public void EquipItem(Item item)
    {
    }
}