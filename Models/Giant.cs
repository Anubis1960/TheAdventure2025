namespace TheAdventure.Models;

public class Giant: EnemyObject
{
    public Giant(SpriteSheet spriteSheet, int x, int y, 
        Func<(int X, int Y)> getPlayerPosition, int maxHealth = 400, int damage = 10, int speed = 90)
        : base(spriteSheet, x, y, getPlayerPosition, maxHealth, damage, speed)
    {
        spriteSheet.ActivateAnimation("Walk");
        spriteSheet.ActivateAnimation("Idle");
    }
    
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }
}