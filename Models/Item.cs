// Item.cs
namespace TheAdventure.Models;

public class Item
{
    public enum ItemType
    {
        DamageUp,
        HealthUp,
        SpeedUp,
        BombCapacityUp
    }

    public ItemType Type { get; }
    public string Name { get; }
    public string Description { get; }
    public string AssetPath { get; }
    public int Value { get; }
    
    public int TextureId { get; set; } = -1;

    public Item(ItemType type, string name, string description, string assetPath, int value)
    {
        Type = type;
        Name = name;
        Description = description;
        AssetPath = assetPath;
        Value = value;
        
        
    }

    public void ApplyUpgrade(PlayerObject player)
    {
        switch (Type)
        {
            case ItemType.DamageUp:
                player.Damage += Value;
                break;
            case ItemType.HealthUp:
                player.MaxHealth += Value;
                player.Heal(Value);
                break;
            case ItemType.SpeedUp:
                player.Speed = (int)(player.Speed * (1 + Value / 100.0));
                Console.WriteLine($"Speed increased by {Value}. New speed: {player.Speed}");
                break;
            case ItemType.BombCapacityUp:
                break;
        }
    }
}