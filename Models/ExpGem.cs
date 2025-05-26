using Silk.NET.Maths;
using TheAdventure.Models.Data;

namespace TheAdventure.Models;

public class GemObject : RenderableGameObject
{
    public GemType Type { get; }
    public int Value { get; }

    public GemObject(SpriteSheet spriteSheet, (int X, int Y) position, GemType type, int value)
        : base(spriteSheet, position)
    {
        Type = type;
        Value = value;
    }

    public void ApplyEffect(PlayerObject player)
    {
        switch (Type)
        {
            case GemType.Health:
                player.Heal(Value);
                break;
            case GemType.Experience:
                player.GainExperience(Value);
                break;
        }
    }

    public void checkPlayerCollision(PlayerObject player, double msSinceLastFrame)
    {
        var playerPos = player.Position;
        var gemPos = Position;

        // Calculate distance between player and gem
        var dx = playerPos.X - gemPos.X;
        var dy = playerPos.Y - gemPos.Y;
        var distanceSquared = dx * dx + dy * dy;

        // Assuming a radius of 16 pixels for the gem
        const int radius = 16;
        if (distanceSquared <= radius * radius)
        {
            ApplyEffect(player);
        }
    }
}