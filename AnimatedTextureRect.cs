using Godot;
using System;


public partial class AnimatedTextureRect : TextureRect
{
    [Export] private SpriteFrames spriteFrames;
    [Export] private string animationName = "default";
    [Export] private float frameRate = 10.0f;
    [Export] private bool isPlaying = false;

    private float _timeAccumulator = 0.0f;
    private int _currentFrame = 0;

    public override void _Ready()
    {
        if (spriteFrames == null)
        {
            GD.PrintErr("SpriteFrames not assigned in AnimatedTextureRect.");
            return;
        }
        ShowFrame(0); // Show the first frame initially
    }

    public override void _Process(double delta)
    {
        if (spriteFrames == null) return;

        if (isPlaying)
        {
            _timeAccumulator += (float)delta;
            if (_timeAccumulator >= 1.0f / frameRate)
            {
                _timeAccumulator -= 1.0f / frameRate;
                _currentFrame = (_currentFrame + 1) % spriteFrames.GetFrameCount(animationName);
                Texture = spriteFrames.GetFrameTexture(animationName, _currentFrame);
            }
        }
    }

    private void PlayAnimation()
    {
        isPlaying = true;
        _currentFrame = 0;
        Texture = spriteFrames.GetFrameTexture(animationName, _currentFrame);
    }
    private void ShowFrame(int frameIndex)
    {
        if (spriteFrames == null || !spriteFrames.HasAnimation(animationName))
        {
            GD.PrintErr("SpriteFrames or animation not set.");
            return;
        }
        if (frameIndex < 0 || frameIndex >= spriteFrames.GetFrameCount(animationName))
        {
            GD.PrintErr($"Frame index {frameIndex} out of bounds for animation '{animationName}'.");
            return;
        }
        _currentFrame = frameIndex;
        Texture = spriteFrames.GetFrameTexture(animationName, _currentFrame);
    }
}
