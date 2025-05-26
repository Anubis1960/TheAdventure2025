namespace TheAdventure.Models;

public class Giant: EnemyObject
{
    public Giant(SpriteSheet spriteSheet, int x, int y, 
        Func<(int X, int Y)> getPlayerPosition)
        : base(spriteSheet, x, y, getPlayerPosition, maxHealth: 400, damage: 10, speed: 90)
    {
        spriteSheet.ActivateAnimation("Walk");
        spriteSheet.ActivateAnimation("Idle");
    }
    
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }
}