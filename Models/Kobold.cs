namespace TheAdventure.Models;

public class Kobold : EnemyObject
{
    public Kobold(SpriteSheet spriteSheet, int x, int y, 
        Func<(int X, int Y)> getPlayerPosition)
        : base(spriteSheet, x, y, getPlayerPosition, maxHealth: 50, damage: 1, speed: 50)
    {
        spriteSheet.ActivateAnimation("Walk");
        spriteSheet.ActivateAnimation("Idle");
    }
}