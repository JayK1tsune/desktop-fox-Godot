using Godot;
using System;
using Godot.Collections;

using System.Runtime.Versioning;

public partial class Slime : AnimatedSprite2D
{
    private FoxPet _foxPetScript;
    private Vector2 _slimeSprite;
    private ClickThrough _clickThrough;

    enum SpriteAnimations{idle, BlowUp, Moving, Jumping}
    private SpriteAnimations _currentAnimation;

    [Export]
    private float _offsetY;
    [Export]
    private float _offsetX;
    [Export]
    private int _speed;


    public override void _Ready()
    {
        //use the getter to get the click through node
        _clickThrough = GetParent<SlimeManager>().ClickThrough;
        _foxPetScript = GetNode<FoxPet>("/root/Base/Fox");
        var tex = SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame());
        _slimeSprite = tex.GetSize();
        CallDeferred(nameof(UpdateSlimeLocation));
        var body = GetNode<Area2D>("ClickLogic");
        body.InputPickable = true;
        body.Connect("input_event", new Callable(this, nameof(OnInputEvent)));

        // Additional initialization code can go here
    }

    public override void _Process(double delta)
    {

        var _mousePosition = GetViewport().GetMousePosition();
        bool hoveringSprite = IsMouseOverOpaquePixelOnly(
            SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame()),
            _mousePosition,
            Position,
            _slimeSprite
        );
        if (hoveringSprite)
        {
            _clickThrough.SetClickThrough(false);
        }
        SlimeMovement((float)delta);


    }

    private void SlimeMovement(float delta)
    {
        Vector2 pos = Position;
        // kist move the slime left and right within the work area
        pos.X += _speed * (float)delta;
        GD.Print("Slime Position: " + pos);
        if (pos.X > _foxPetScript._workArea.Right - _slimeSprite.X / 2 || pos.X < _foxPetScript._workArea.Left + _slimeSprite.X / 2)
        {
            _speed = -_speed;
            //flip the sprite
            FlipH = !FlipH;
        }
        Position = new Vector2(pos.X, pos.Y);
        _currentAnimation = SpriteAnimations.Moving;
        HandleSlimeAnimations();
    }
    

    private void UpdateSlimeLocation()
    {
        Vector2 pos = Position;
        float groundY = _foxPetScript._workArea.Bottom - _slimeSprite.Y - _offsetY;
        pos.Y = groundY;
        pos.X = _offsetX;
        Position = new Vector2(pos.X, pos.Y);
    }



    private void HandleSlimeAnimations()
    {
        switch (_currentAnimation)
        {
            case SpriteAnimations.idle:
                Play("idle");
                break;
            case SpriteAnimations.BlowUp:
                Play("BlowUp");
                break;
            case SpriteAnimations.Moving:
                Play("Moving");
                break;
            case SpriteAnimations.Jumping:
                Play("Jumping");
                break;
        }
    }

    private void OnInputEvent(Node viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            GD.Print("Slime clicked!");
            _currentAnimation = SpriteAnimations.BlowUp;
            HandleSlimeAnimations();
            //destroy the slime after the animation is done
            GetTree().CreateTimer(1.0f).Connect("timeout", new Callable(this, nameof(OnSlimeAnimationEnd)));
        }
    }

    private void OnSlimeAnimationEnd()
    {
        QueueFree();
    }
    
    private bool IsMouseOverOpaquePixelOnly(Texture2D texture, Vector2 mousePos, Vector2 spritePos, Vector2 spriteSize, float alphaThreshold = 0.5f)
    {
        if (_foxPetScript._uiActive)  // Check if click-through is enabled
        {
            return false; // Ignore if click-through is enabled
        }
        Vector2 localPos = ToLocal(mousePos);

        int x = Mathf.Clamp((int)(localPos.X + spriteSize.X / 2), 0, (int)spriteSize.X - 1);
        int y = Mathf.Clamp((int)(localPos.Y + spriteSize.Y / 2), 0, (int)spriteSize.Y - 1);

        Image image = texture.GetImage();

        if (image != null)
        {
            Color pixelColor = image.GetPixel(x, y);
            return pixelColor.A >= alphaThreshold;
        }

        return false;
    }


}
