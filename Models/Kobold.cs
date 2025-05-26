namespace TheAdventure.Models;

public class KoboldWeak : EnemyObject
{
    public KoboldWeak(SpriteSheet spriteSheet, int x, int y, 
        Func<(int X, int Y)> getPlayerPosition)
        : base(spriteSheet, x, y, getPlayerPosition, maxHealth: 50, damage: 10, speed: 50)
    {
        spriteSheet.ActivateAnimation("Walk");
        spriteSheet.ActivateAnimation("Idle");
    }
}