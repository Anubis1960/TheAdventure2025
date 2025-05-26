using Silk.NET.SDL;

namespace TheAdventure.Models.Data;

public class GemObject : RenderableGameObject
{
    public int amount;
    
    public GemObject(SpriteSheet spriteSheet, (int X, int Y) position, int amount) : base(spriteSheet, position)
    {
        this.amount = amount;
    }
}