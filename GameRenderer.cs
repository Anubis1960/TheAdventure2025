using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;
using System.Drawing;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private Camera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;

    private Dictionary<char, int> _fontTextures = new();
    private int _fontTextureId = -1;
    private const int FontCharWidth = 16;
    private const int FontCharHeight = 16;
    
    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);
        
        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
        
        LoadFontTexture("Assets/font.png");
    }
    
    public void LoadFontTexture(string fileName)
    {
        _fontTextureId = LoadTexture(fileName, out _);
    
        // Simple font mapping (ASCII characters)
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!?.,:;()[]{}<>+-*/= ";
    
        for (int i = 0; i < chars.Length; i++)
        {
            _fontTextures[chars[i]] = i;
        }
    }
    
    
    public void RenderText(string text, int x, int y, byte r, byte g, byte b)
    {
        if (_fontTextureId == -1) return;
    
        _sdl.SetRenderDrawColor(_renderer, r, g, b, 255);
    
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (!_fontTextures.TryGetValue(c, out int charIndex)) continue;
        
            int srcX = (charIndex % 16) * FontCharWidth;
            int srcY = (charIndex / 16) * FontCharHeight;
        
            var srcRect = new Rectangle<int>(srcX, srcY, FontCharWidth, FontCharHeight);
            var dstRect = new Rectangle<int>(x + (i * FontCharWidth), y, FontCharWidth, FontCharHeight);
        
            _sdl.RenderCopy(_renderer, (Texture*)_texturePointers[_fontTextureId], srcRect, dstRect);
        }
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        _camera.SetWorldBounds(bounds);
    }

    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        using (var fStream = new FileStream(fileName, FileMode.Open))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData()
            {
                Width = image.Width,
                Height = image.Height
            };
            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());
            fixed (byte* data = imageRAWData)
            {
                var imageSurface = _sdl.CreateRGBSurfaceWithFormatFrom(data, textureInfo.Width,
                    textureInfo.Height, 8, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);
                if (imageSurface == null)
                {
                    throw new Exception("Failed to create surface from image data.");
                }
                
                var imageTexture = _sdl.CreateTextureFromSurface(_renderer, imageSurface);
                if (imageTexture == null)
                {
                    _sdl.FreeSurface(imageSurface);
                    throw new Exception("Failed to create texture from surface.");
                }
                
                _sdl.FreeSurface(imageSurface);
                
                _textureData[_textureId] = textureInfo;
                _texturePointers[_textureId] = (IntPtr)imageTexture;
            }
        }

        return _textureId++;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src,
                in translatedDst,
                angle,
                in center, flip);
        }
    }
    
    public void DrawHealthBar(int currentHealth, int maxHealth, int x, int y, int width, int height)
    {
        var rendererPtr = _renderer;
        
        // Draw the background of the health bar
        _sdl.SetRenderDrawColor(rendererPtr, 0, 0, 0, 255); // Black background
        _sdl.RenderFillRect(rendererPtr, new Rectangle<int>(x, y, width, height));
        // Calculate the width of the health portion
        var healthWidth = (int)((double)currentHealth / maxHealth * width);
        // Draw the health portion
        _sdl.SetRenderDrawColor(rendererPtr, 0, 255, 0, 255); // Green health
        _sdl.RenderFillRect(rendererPtr, new Rectangle<int>(x, y, healthWidth, height));
        
        RenderText($"{currentHealth}/{maxHealth}", x + 5, y + 5, 255, 255, 255);
        
        // Draw the border of the health bar
        _sdl.SetRenderDrawColor(rendererPtr, 255, 255, 255, 255); // White border
        _sdl.RenderDrawRect(rendererPtr, new Rectangle<int>(x, y, width, height));
    }
    
    public void DrawExperienceTracker(int currentLevel, int currentExperience, int experienceToNextLevel, int x, int y, int width, int height)
    {
        var rendererPtr = _renderer;
        
        // Draw the background of the experience tracker
        _sdl.SetRenderDrawColor(rendererPtr, 0, 0, 0, 255); // Black background
        _sdl.RenderFillRect(rendererPtr, new Rectangle<int>(x, y, width, height));
        
        // Calculate the width of the experience portion
        var experienceWidth = (int)((double)currentExperience / experienceToNextLevel * width);
        
        // Draw the experience portion
        _sdl.SetRenderDrawColor(rendererPtr, 0, 0, 255, 255); // Blue experience
        _sdl.RenderFillRect(rendererPtr, new Rectangle<int>(x, y, experienceWidth, height));
        
        RenderText($"Level: {currentLevel}", x + 10, y + 5, 255, 255, 255);
        
        // Draw the border of the experience tracker
        _sdl.SetRenderDrawColor(rendererPtr, 255, 255, 255, 255); // White border
        _sdl.RenderDrawRect(rendererPtr, new Rectangle<int>(x, y, width, height));
    }

    public Vector2D<int> ToWorldCoordinates(int x, int y)
    {
        return _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    }

    public void ClearScreen()
    {
        _sdl.RenderClear(_renderer);
    }

    public void PresentFrame()
    {
        _sdl.RenderPresent(_renderer);
    }
    
    public Rectangle<int> GetCameraBounds()
    {
        return new Rectangle<int>(
            _camera.X, _camera.Y, 
            _camera.Width, _camera.Height
        );
    }
    
    public void RenderUpgradeMenu(UpgradeMenu menu, int windowWidth, int windowHeight)
    {
        if (!menu.IsVisible) return;

        // Draw semi-transparent background
        _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 200);
        var backgroundRect = new Rectangle<int>(0, 0, windowWidth, windowHeight);
        _sdl.RenderFillRect(_renderer, backgroundRect);

        // Draw upgrade menu box
        var menuWidth = 600;
        var menuHeight = 400;
        var menuX = (windowWidth - menuWidth) / 2;
        var menuY = (windowHeight - menuHeight) / 2;

        _sdl.SetRenderDrawColor(_renderer, 50, 50, 50, 255);
        var menuRect = new Rectangle<int>(menuX, menuY, menuWidth, menuHeight);
        _sdl.RenderFillRect(_renderer, menuRect);

        _sdl.SetRenderDrawColor(_renderer, 255, 255, 255, 255);
        _sdl.RenderDrawRect(_renderer, menuRect);

        // Draw title
        RenderText("LEVEL UP! CHOOSE AN UPGRADE:", menuX + 20, menuY + 20, 255, 255, 255);

        // Draw upgrade options
        int optionY = menuY + 60;
        foreach (var upgrade in menu.AvailableUpgrades)
        {
            bool isSelected = upgrade == menu.SelectedUpgrade;
            
            // Draw selection highlight
            if (isSelected)
            {
                _sdl.SetRenderDrawColor(_renderer, 100, 100, 255, 255);
                _sdl.RenderFillRect(_renderer, new Rectangle<int>(menuX + 20, optionY - 5, menuWidth - 40, 80));
            }

            // Draw upgrade icon
            if (_texturePointers.ContainsKey(upgrade.TextureId))
            {
                var iconRect = new Rectangle<int>(menuX + 30, optionY, 64, 64);
                var srcRect = new Rectangle<int>(0, 0, 64, 64); // Assuming icons are 64x64
                _sdl.RenderCopy(_renderer, (Texture*)_texturePointers[upgrade.TextureId], srcRect, iconRect);
            }

            // Draw upgrade info
            RenderText(upgrade.Name, menuX + 110, optionY, 
                isSelected ? (byte)255 : (byte)200, 
                isSelected ? (byte)255 : (byte)200, 
                isSelected ? (byte)0 : (byte)200);
                
            RenderText(upgrade.Description, menuX + 110, optionY + 30, 200, 200, 200);

            optionY += 90;
        }
    }
}
