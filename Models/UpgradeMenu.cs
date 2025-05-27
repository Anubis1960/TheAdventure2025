// UpgradeMenu.cs
using Silk.NET.Maths;
using System.Collections.Generic;

namespace TheAdventure.Models;

public class UpgradeMenu
{
    private readonly List<Item> _allUpgrades;
    private readonly Random _random = new();
    private const int MaxDisplayedUpgrades = 4;

    private List<Item?> _availableUpgrades;
    private Item? _selectedUpgrade;
    private bool _isVisible;

    public bool IsVisible => _isVisible;
    public IReadOnlyList<Item?> AvailableUpgrades => _availableUpgrades;
    public Item? SelectedUpgrade => _selectedUpgrade;

    public UpgradeMenu(GameRenderer renderer)
    {
        _allUpgrades = new List<Item>
        {
            // Damage
            new(ItemType.DamageUp, "Sharpened Blade", "Increase damage by 10", "Assets/upgrade_damage.png", 10)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_damage.png", out _),
                MaxCount = -1
            },
            // Health
            new(ItemType.HealthUp, "Vitality Helmet", "Increase max health by 50", "Assets/upgrade_health.png", 50)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_health.png", out _),
                MaxCount = -1
            },
            // Speed
            new(ItemType.SpeedUp, "Swift Boots", "Increase speed by 20%", "Assets/upgrade_speed.png", 20)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_speed.png", out _),
                MaxCount = -1
            },
            // Bomb Capacity
            new(ItemType.BombCapacityUp, "Extra Pouches", "Place +1 extra bomb", "Assets/upgrade_bomb.png", 1)
            {
                TextureId = renderer.LoadTexture("Assets/upgrade_bomb.png", out _),
                MaxCount = -1
            },
            // XP Boost
            new(ItemType.ExperienceGainBoost, "Lapis", "Blue gems get you more xp", "Assets/exp_gem.png", 20)
            {
                TextureId = renderer.LoadTexture("Assets/exp_gem.png", out _),
                MaxCount = 1
            },
            // Health Regen
            new(ItemType.HealthGainBoost, "Ruby", "Red gems retores morea health", "Assets/health_gem.png", 1)
            {
                TextureId = renderer.LoadTexture("Assets/health_gem.png", out _),
                MaxCount = 1
            }
        };
        
        // Initialize displayed upgrades
        _availableUpgrades = new List<Item?>();
        GenerateRandomUpgrades();
    }
    
    private void GenerateRandomUpgrades()
    {
        var shuffled = _allUpgrades
            .Where(u => u.MaxCount != 0) // Skip if already maxed
            .OrderBy(_ => _random.Next())
            .Take(MaxDisplayedUpgrades)
            .ToList();

        _availableUpgrades.Clear();
        foreach (var item in shuffled)
        {
            _availableUpgrades.Add(item);
        }

        // Fill remaining slots with nulls if needed
        while (_availableUpgrades.Count < MaxDisplayedUpgrades)
        {
            _availableUpgrades.Add(null);
        }

        _selectedUpgrade = _availableUpgrades.FirstOrDefault(u => u != null);
    }
    public void Show()
    {
        _isVisible = true;
        GenerateRandomUpgrades(); // Refresh each time menu opens
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

        // Decrease usage count
        if (_selectedUpgrade.MaxCount > 0)
        {
            _selectedUpgrade.MaxCount--;
            if (_selectedUpgrade.MaxCount <= 0)
            {
                _allUpgrades.Remove(_selectedUpgrade); // Prevent future selection
            }
        }

        GenerateRandomUpgrades(); // Refresh the shop
        Hide();
    }
}