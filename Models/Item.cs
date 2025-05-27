// Item.cs
namespace TheAdventure.Models;

public enum ItemType
{
    DamageUp,
    HealthUp,
    SpeedUp,
    BombCapacityUp,
    ExperienceGainBoost,
    HealthGainBoost
}

public class Item
{

    public ItemType Type { get; }
    public string Name { get; }
    public string Description { get; }
    public string AssetPath { get; }
    public int Value { get; }
    
    public int MaxCount { get; set; } // -1 means unlimited
    
    public int TextureId { get; set; } = -1;

    public Item(ItemType type, string name, string description, string assetPath, int value, int maxCount = -1)
    {
        Type = type;
        Name = name;
        Description = description;
        AssetPath = assetPath;
        Value = value;
        MaxCount = maxCount;
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
                break;
            case ItemType.BombCapacityUp:
                player.ExtraBombCount += Value;
                break;
            case ItemType.ExperienceGainBoost:
                player.ExperienceMultiplier += Value / 100.0; // Assuming ExperienceMultiplier is a property in PlayerObject
                break;
            case ItemType.HealthGainBoost:
                player.HealthMultiplier += Value / 100.0; // Assuming HealthMultiplier is a property in PlayerObject
                break;
        }
        
        if (MaxCount > 0)
        {
            MaxCount--;
        }
    }
}