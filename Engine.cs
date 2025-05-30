using System.Reflection;
using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Models;
using TheAdventure.Models.Data;
using TheAdventure.Scripting;
using GemObject = TheAdventure.Models.GemObject;

namespace TheAdventure;

public class Engine
{
    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly ScriptEngine _scriptEngine = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _loadedTileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();

    private Level _currentLevel = new();
    private PlayerObject? _player;

    private DateTimeOffset _lastUpdate = DateTimeOffset.Now;
    
    private DateTimeOffset _lastSpawnTime = DateTimeOffset.Now;
    private readonly Random _random = new();
    private const double SpawnInterval = 1000; // milliseconds between spawns
    private const int MaxEnemies = 30;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, coords) => AddBomb(coords.x, coords.y);
    }

    public void SetupWorld()
    {
        _player = new(SpriteSheet.Load(_renderer, "Player.json", "Assets"), 100, 100);

        var levelContent = File.ReadAllText(Path.Combine("Assets", "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(levelContent);
        if (level == null)
        {
            throw new Exception("Failed to load level");
        }

        foreach (var tileSetRef in level.TileSets)
        {
            var tileSetContent = File.ReadAllText(Path.Combine("Assets", tileSetRef.Source));
            var tileSet = JsonSerializer.Deserialize<TileSet>(tileSetContent);
            if (tileSet == null)
            {
                throw new Exception("Failed to load tile set");
            }

            foreach (var tile in tileSet.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine("Assets", tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }

            _loadedTileSets.Add(tileSet.Name, tileSet);
        }

        if (level.Width == null || level.Height == null)
        {
            throw new Exception("Invalid level dimensions");
        }

        if (level.TileWidth == null || level.TileHeight == null)
        {
            throw new Exception("Invalid tile dimensions");
        }

        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, level.Width.Value * level.TileWidth.Value,
            level.Height.Value * level.TileHeight.Value));
        
        _currentLevel = level;

        _scriptEngine.LoadAll(Path.Combine("Assets", "Scripts"));
    }

    public void ProcessFrame()
    {
        var currentTime = DateTimeOffset.Now;
        var msSinceLastFrame = (currentTime - _lastUpdate).TotalMilliseconds;
        _lastUpdate = currentTime;

        if (_player == null)
        {
            return;
        }

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;
        bool isAttacking = _input.IsKeyAPressed() && (up + down + left + right <= 1);
        bool addBomb = _input.IsKeyBPressed();

        _player.UpdatePosition(up, down, left, right, 48, 48, msSinceLastFrame);
        if (isAttacking)
        {
            _player.Attack();
            CheckPlayerAttackHit();
        }
        
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is EnemyObject enemy)
            {
                enemy.Update(msSinceLastFrame);
                enemy.CheckPlayerCollision(_player, msSinceLastFrame);
            }

            if (gameObject is GemObject gem)
            {
                bool expired = gem.CheckPlayerCollision(_player, msSinceLastFrame);
                if (expired)
                {
                    _gameObjects.Remove(gem.Id);
                }
            }
        }
        
        if ((currentTime - _lastSpawnTime).TotalMilliseconds >= SpawnInterval && 
            _gameObjects.Values.Count(obj => obj is EnemyObject) < MaxEnemies)
        {
            SpawnEnemyOutsideView();
            _lastSpawnTime = currentTime;
        }
        
        _player.Update(msSinceLastFrame);
        
        _scriptEngine.ExecuteAll(this);

        if (addBomb)
        {
            AddBomb(_player.Position.X, _player.Position.Y, false);
        }
    }
    
    private void SpawnEnemyOutsideView()
    {
        if (_player == null) return;

        var spriteSheet = SpriteSheet.Load(_renderer, "Kobold.json", "Assets");
    
        // Get camera/view bounds
        var cameraBounds = _renderer.GetCameraBounds();
        var padding = 100; // padding to ensure enemy spawns well outside view
    
        // Possible spawn areas (left, right, top, bottom of screen)
        var spawnAreas = new List<Rectangle<int>>()
        {
            new Rectangle<int>(
                cameraBounds.Origin.X - 200 - padding, 
                cameraBounds.Origin.Y, 
                200, 
                cameraBounds.Size.Y), // Left
            new Rectangle<int>(
                cameraBounds.Origin.X + cameraBounds.Size.X + padding, 
                cameraBounds.Origin.Y, 
                200, 
                cameraBounds.Size.Y), // Right
            new Rectangle<int>(
                cameraBounds.Origin.X, 
                cameraBounds.Origin.Y - 200 - padding, 
                cameraBounds.Size.X, 
                200), // Top
            new Rectangle<int>(
                cameraBounds.Origin.X, 
                cameraBounds.Origin.Y + cameraBounds.Size.Y + padding, 
                cameraBounds.Size.X, 
                200) // Bottom
        };
    
        // Pick a random spawn area
        var spawnArea = spawnAreas[_random.Next(spawnAreas.Count)];
    
        // Random position within the spawn area
        int x = _random.Next(spawnArea.Origin.X, spawnArea.Origin.X + spawnArea.Size.X);
        int y = _random.Next(spawnArea.Origin.Y, spawnArea.Origin.Y + spawnArea.Size.Y);
    
        // Ensure position is within level bounds
        x = Math.Clamp(x, 0, _currentLevel.Width.Value * _currentLevel.TileWidth.Value);
        y = Math.Clamp(y, 0, _currentLevel.Height.Value * _currentLevel.TileHeight.Value);
    
        var enemy = new Kobold(
            spriteSheet,
            x,
            y,
            () => _player?.Position ?? (0, 0)
        );
    
        _gameObjects.Add(enemy.Id, enemy);
    }


    private void CheckPlayerAttackHit()
    {
        if (_player == null) return;

        var attackRange = 50; // Adjust as needed
        var playerPos = _player.Position;
        
        var gemsToAdd = new List<GemObject>();

        foreach (var gameObject in _gameObjects.Values.ToList())
        {
            if (gameObject is EnemyObject enemy && enemy.IsAlive)
            {
                if (enemy.CheckCollision(playerPos.X, playerPos.Y, attackRange))
                {
                    enemy.TakeDamage(_player.Damage);
                    Console.WriteLine($"Enemy {enemy.Id} hit by player attack. Remaining health: {enemy.Health}");
                    if (!enemy.IsAlive)
                    {
                        GemType gemType = (GemType)_random.Next(0, Enum.GetValues(typeof(GemType)).Length);
                        int value = _random.Next(50, 100); // Random amount of gems
                        if (gemType == GemType.Health)
                        {
                            var gem = new GemObject(
                                SpriteSheet.Load(_renderer, "HealthGem.json", "Assets"),
                                (enemy.Position.X, enemy.Position.Y), gemType, value);
                            gemsToAdd.Add(gem); // Add to temporary list
                        }
                        else if (gemType == GemType.Experience)
                        {
                            var gem = new GemObject(
                                SpriteSheet.Load(_renderer, "ExperienceGem.json", "Assets"),
                                (enemy.Position.X, enemy.Position.Y), gemType, value);
                            gemsToAdd.Add(gem);
                        }
                    }
                }
            }
        }
        
        // Add gems to the game objects
        foreach (var gem in gemsToAdd)
        {
            _gameObjects.Add(gem.Id, gem);
        }
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        var playerPosition = _player!.Position;
        _renderer.CameraLookAt(playerPosition.X, playerPosition.Y);

        RenderTerrain();
        RenderAllObjects();
        
        _renderer.DrawHealthBar(_player.Health, 1000, 10, 10, 200, 20);
        
        _renderer.DrawExperienceTracker(_player.Experience, _player.ExperienceToNextLevel, 430, 10, 200, 20);

        _renderer.PresentFrame();
    }

    public void RenderAllObjects()
    {
        var toRemove = new List<int>();
        var gemsToAdd = new List<GemObject>();
        foreach (var gameObject in GetRenderables())
        {
            gameObject.Render(_renderer);

            if (gameObject is TemporaryGameObject { IsExpired: true } tempGameObject)
            {
                // Check damage to player
                if (_player != null)
                {
                    var deltaX = Math.Abs(_player.Position.X - tempGameObject.Position.X);
                    var deltaY = Math.Abs(_player.Position.Y - tempGameObject.Position.Y);
                    if (deltaX < 32 && deltaY < 32)
                    {
                        _player.TakeDamage(10);
                    }
                }

                // Check damage to enemies
                var explosionRadius = 50;
                foreach (var enemy in _gameObjects.Values.OfType<EnemyObject>().ToList()) // Use .ToList() to avoid modification during iteration
                {
                    if (enemy.CheckCollision(tempGameObject.Position.X, tempGameObject.Position.Y, explosionRadius))
                    {
                        if (_player != null) enemy.TakeDamage(20 + _player.Damage);
                        if (!enemy.IsAlive)
                        {
                            // Determine gem type and value
                            GemType gemType = (GemType)_random.Next(0, Enum.GetValues(typeof(GemType)).Length);
                            int value = _random.Next(50, 100);

                            string gemFile = gemType == GemType.Health ? "HealthGem.json" : "ExperienceGem.json";
                            var spriteSheet = SpriteSheet.Load(_renderer, gemFile, "Assets");

                            var gem = new GemObject(
                                spriteSheet,
                                (tempGameObject.Position.X, tempGameObject.Position.Y),
                                gemType,
                                value
                            );

                            gemsToAdd.Add(gem); // Add to temporary list
                        }
                    }
                }

                toRemove.Add(tempGameObject.Id);
            }
        }
        
        // Add gems to the game objects
        foreach (var gem in gemsToAdd)
        {
            _gameObjects.Add(gem.Id, gem);
        }

        foreach (var id in toRemove)
        {
            _gameObjects.Remove(id);
        }

        // Remove dead enemies
        var deadEnemies = _gameObjects.Values
            .OfType<EnemyObject>()
            .Where(e => !e.IsAlive)
            .Select(e => e.Id)
            .ToList();
            
        foreach (var id in deadEnemies)
        {
            _gameObjects.Remove(id);
        }

        _player?.Render(_renderer);
    }

    public void RenderTerrain()
    {
        foreach (var currentLayer in _currentLevel.Layers)
        {
            for (int i = 0; i < _currentLevel.Width; ++i)
            {
                for (int j = 0; j < _currentLevel.Height; ++j)
                {
                    int? dataIndex = j * currentLayer.Width + i;
                    if (dataIndex == null)
                    {
                        continue;
                    }

                    var currentTileId = currentLayer.Data[dataIndex.Value] - 1;
                    if (currentTileId == null)
                    {
                        continue;
                    }

                    var currentTile = _tileIdMap[currentTileId.Value];

                    var tileWidth = currentTile.ImageWidth ?? 0;
                    var tileHeight = currentTile.ImageHeight ?? 0;

                    var sourceRect = new Rectangle<int>(0, 0, tileWidth, tileHeight);
                    var destRect = new Rectangle<int>(i * tileWidth, j * tileHeight, tileWidth, tileHeight);
                    _renderer.RenderTexture(currentTile.TextureId, sourceRect, destRect);
                }
            }
        }
    }

    public IEnumerable<RenderableGameObject> GetRenderables()
    {
        foreach (var gameObject in _gameObjects.Values)
        {
            if (gameObject is RenderableGameObject renderableGameObject)
            {
                yield return renderableGameObject;
            }
        }
    }

    public (int X, int Y) GetPlayerPosition()
    {
        return _player!.Position;
    }

    public void AddBomb(int X, int Y, bool translateCoordinates = true)
    {
        var worldCoords = translateCoordinates ? _renderer.ToWorldCoordinates(X, Y) : new Vector2D<int>(X, Y);

        SpriteSheet spriteSheet = SpriteSheet.Load(_renderer, "BombExploding.json", "Assets");
        spriteSheet.ActivateAnimation("Explode");

        TemporaryGameObject bomb = new(spriteSheet, 2.1, (worldCoords.X, worldCoords.Y));
        _gameObjects.Add(bomb.Id, bomb);
    }
    
}