namespace TheAdventure.Models;

public class Kobold : EnemyObject
{
    public Kobold(SpriteSheet spriteSheet, int x, int y, 
        Func<(int X, int Y)> getPlayerPosition, int maxHealth = 50, int damage = 1, int speed = 50)
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