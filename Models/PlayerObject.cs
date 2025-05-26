using Silk.NET.Maths;

namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject
{
    private const int _speed = 128; // pixels per second
    public int MaxHealth = 1000;
    
    public int Experience { get;  set; } = 0;
    
    public int Level { get;  set; } = 1;
    
    public int ExperienceToNextLevel { get;  set; } = 1000; // Starting experience to next level
    
    public int Health { get;  set; } = 1000; // Starting health
    
    public int Damage { get;  set; } = 10;
    
    public int Speed { get;  set; } = _speed; // pixels per second
    
    private double _invincibilityTimer = 0;
    private const double InvincibilityDuration = 0.5; // seconds

    private UpgradeMenu _upgradeMenu;

    public UpgradeMenu UpgradeMenu => _upgradeMenu;

    public bool IsInvincible => _invincibilityTimer > 0;

    public enum PlayerStateDirection
    {
        None = 0,
        Down,
        Up,
        Left,
        Right,
    }

    public enum PlayerState
    {
        None = 0,
        Idle,
        Move,
        Attack,
        GameOver
    }

    public (PlayerState State, PlayerStateDirection Direction) State { get; private set; }

    public PlayerObject(SpriteSheet spriteSheet, int x, int y, GameRenderer renderer) : base(spriteSheet, (x, y))
    {
        _upgradeMenu = new UpgradeMenu(renderer);
        SetState(PlayerState.Idle, PlayerStateDirection.Down);
    }

    public void SetState(PlayerState state)
    {
        SetState(state, State.Direction);
    }

    public void SetState(PlayerState state, PlayerStateDirection direction)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        if (State.State == state && State.Direction == direction)
        {
            return;
        }

        if (state == PlayerState.None && direction == PlayerStateDirection.None)
        {
            SpriteSheet.ActivateAnimation(null);
        }

        else if (state == PlayerState.GameOver)
        {
            SpriteSheet.ActivateAnimation(Enum.GetName(state));
        }
        else
        {
            var animationName = Enum.GetName(state) + Enum.GetName(direction);
            SpriteSheet.ActivateAnimation(animationName);
        }

        State = (state, direction);
    }

    public void GameOver()
    {
        SetState(PlayerState.GameOver, PlayerStateDirection.None);
    }

    public void Attack()
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var direction = State.Direction;
        SetState(PlayerState.Attack, direction);
    }
    
    public void TakeDamage(int amount)
    {
        if (IsInvincible) return;

        Health -= amount;
        _invincibilityTimer = InvincibilityDuration;
    }
    
    public void Update(double deltaTimeInSeconds)
    {
        if (Health <= 0)
        {
            GameOver();
            return;
        }
        if (_invincibilityTimer > 0)
        {
            _invincibilityTimer -= deltaTimeInSeconds;
            if (_invincibilityTimer < 0) _invincibilityTimer = 0;
        }
    }

    public void UpdatePosition(double up, double down, double left, double right, int width, int height, double time)
    {
        if (State.State == PlayerState.GameOver)
        {
            return;
        }

        var pixelsToMove = _speed * (time / 1000.0);

        var x = Position.X + (int)(right * pixelsToMove);
        x -= (int)(left * pixelsToMove);

        var y = Position.Y + (int)(down * pixelsToMove);
        y -= (int)(up * pixelsToMove);

        var newState = State.State;
        var newDirection = State.Direction;

        if (x == Position.X && y == Position.Y)
        {
            if (State.State == PlayerState.Attack)
            {
                if (SpriteSheet.AnimationFinished)
                {
                    newState = PlayerState.Idle;
                }
            }
            else
            {
                newState = PlayerState.Idle;
            }
        }
        else
        {
            newState = PlayerState.Move;
            
            if (y < Position.Y && newDirection != PlayerStateDirection.Up)
            {
                newDirection = PlayerStateDirection.Up;
            }

            if (y > Position.Y && newDirection != PlayerStateDirection.Down)
            {
                newDirection = PlayerStateDirection.Down;
            }

            if (x < Position.X && newDirection != PlayerStateDirection.Left)
            {
                newDirection = PlayerStateDirection.Left;
            }

            if (x > Position.X && newDirection != PlayerStateDirection.Right)
            {
                newDirection = PlayerStateDirection.Right;
            }
        }

        if (newState != State.State || newDirection != State.Direction)
        {
            SetState(newState, newDirection);
        }

        Position = (x, y);
    }

    public void Heal(int value)
    {
        Health += value;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
    }

    public void GainExperience(int value)
    {
        if (State.State == PlayerState.GameOver) return;

        Experience += value;

        // Check for level up
        while (Experience >= ExperienceToNextLevel)
        {
            Level++;
            Experience -= ExperienceToNextLevel;
            ExperienceToNextLevel = (int)(ExperienceToNextLevel * 1.5); // Scale requirement
        
            // Show upgrade menu
            _upgradeMenu.Show();
        }
    }
}