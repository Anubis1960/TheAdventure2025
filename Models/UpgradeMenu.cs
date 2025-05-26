// UpgradeMenu.cs
using Silk.NET.Maths;
using System.Collections.Generic;

namespace TheAdventure.Models;

public class UpgradeMenu
{
    private readonly List<Item> _availableUpgrades;
    private Item _selectedUpgrade;
    private bool _isVisible;
    
    public bool IsVisible => _isVisible;
    public IReadOnlyList<Item> AvailableUpgrades => _availableUpgrades;
    public Item SelectedUpgrade => _selectedUpgrade;

    public UpgradeMenu(GameRenderer renderer)
    {
        _availableUpgrades = new List<Item>
        {
            new Item(Item.ItemType.DamageUp, "Sharpened Blade", "Increase damage by 10", "Assets/upgrade_damage.png", 10)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_damage.png", out _)
            },
            new Item(Item.ItemType.HealthUp, "Vitality Helmet", "Increase max health by 50", "Assets/upgrade_health.png", 50)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_health.png", out _)
            },
            new Item(Item.ItemType.SpeedUp, "Swift Boots", "Increase speed by 20%", "Assets/upgrade_speed.png", (int) (0.2))
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_speed.png", out _)
            },
            new Item(Item.ItemType.BombCapacityUp, "Extra Pouches", "Carry +1 bomb", "Assets/upgrade_bomb.png", 1)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_bomb.png", out _)
            }
        };
    }

    public void Show()
    {
        _isVisible = true;
        // Select first upgrade by default
        if (_availableUpgrades.Count > 0)
        {
            _selectedUpgrade = _availableUpgrades[0];
        }
    }

    public void Hide()
    {
        _isVisible = false;
    }

    public void SelectNextUpgrade()
    {
        if (!_isVisible || _availableUpgrades.Count == 0) return;
        
        var currentIndex = _availableUpgrades.IndexOf(_selectedUpgrade);
        var nextIndex = (currentIndex + 1) % _availableUpgrades.Count;
        _selectedUpgrade = _availableUpgrades[nextIndex];
    }

    public void SelectPreviousUpgrade()
    {
        if (!_isVisible || _availableUpgrades.Count == 0) return;
        
        var currentIndex = _availableUpgrades.IndexOf(_selectedUpgrade);
        var previousIndex = (currentIndex - 1 + _availableUpgrades.Count) % _availableUpgrades.Count;
        _selectedUpgrade = _availableUpgrades[previousIndex];
    }

    public void ApplySelectedUpgrade(PlayerObject player)
    {
        if (!_isVisible || _selectedUpgrade == null) return;
        
        _selectedUpgrade.ApplyUpgrade(player);
        Hide();
    }
}