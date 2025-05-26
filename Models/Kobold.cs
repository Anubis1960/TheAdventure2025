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
    
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if (!IsAlive)
        {
            // Drop random gem on death
            var random = new Random();
            var gemType = random.NextDouble() < 0.5 ? GemType.Health : GemType.Experience;
            var gemValue = random.Next(10, 26); // Adjust values as needed
            string gemFileName = gemType == GemType.Health ? "HealthGem.json" : "ExpGem.json";

            var spriteSheet = SpriteSheet.Load(_renderer, gemFileName, "Assets");
            var gem = new GemObject(spriteSheet, Position, gemType, gemValue);
            Engine.AddGameObject(gem);
        }
    }
}