using Silk.NET.Maths;

namespace TheAdventure.Models;

public class EnemyObject : RenderableGameObject
{
    public int Health { get; protected set; }
    public int MaxHealth { get; protected set; }
    public int Damage { get; protected set; }
    
    public bool IsAlive => Health > 0;
    
    public int Speed { get; protected set; } = 128; // pixels per second
    
    protected Func<(int X, int Y)> GetPlayerPosition;

    // Use floating point internally
    private Vector2D<float> _position;

    // Public int-based Position getter
    public (int X, int Y) Position => ((int)_position.X, (int)_position.Y);

    public EnemyObject(SpriteSheet spriteSheet, int x, int y, 
        Func<(int X, int Y)> getPlayerPosition,
        int maxHealth, int damage, int speed = 128)
        : base(spriteSheet, (x, y))
    {
        _position = new Vector2D<float>(x, y);
        GetPlayerPosition = getPlayerPosition;
        MaxHealth = maxHealth;
        Health = maxHealth;
        Damage = damage;
        Speed = speed;
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // Optionally do something when enemy dies (e.g., drop item, play sound)
        Health = 0;
    }
    
    public bool CheckCollision(int x, int y, int radius)
    {
        var enemyPos = Position;
        var dx = enemyPos.X - x;
        var dy = enemyPos.Y - y;
        var distanceSquared = dx * dx + dy * dy;
        return distanceSquared <= radius * radius;
    }

    public override void Update(double msSinceLastFrame)
    {
        base.Update(msSinceLastFrame); // For animations

        var playerPos = GetPlayerPosition();
        playerPos.X -= 15; // Center the player position
        playerPos.Y -= 22;
        float enemyX = _position.X;
        float enemyY = _position.Y;

        double dx = playerPos.X - enemyX;
        double dy = playerPos.Y - enemyY;

        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance > 0.01)
        {
            dx /= distance;
            dy /= distance;

            float moveAmount = Speed * (float)(msSinceLastFrame / 1000.0); // Convert ms to seconds

            // Clamp movement if we're close to the target
            if (distance <= 1.0f) // If already very close
            {
                _position = new Vector2D<float>(playerPos.X, playerPos.Y);
                Console.WriteLine("Enemy reached player exactly!");
            }
            else
            {
                _position += new Vector2D<float>((float)(dx * moveAmount), (float)(dy * moveAmount));
            }

            Console.WriteLine($"Enemy moved to ({(int)_position.X}, {(int)_position.Y})");
        }
    }

    public override void Render(GameRenderer renderer)
    {
        // Use integer position for rendering
        base.RenderAt(renderer, (int)_position.X, (int)_position.Y);
    }
}