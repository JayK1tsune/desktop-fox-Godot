using Godot;
using System;

public partial class Slime : AnimatedSprite2D
{
    private FoxPet _foxPetScript;
    private Vector2 _slimeSprite;


    public override void _Ready()
    {
        _foxPetScript = GetNode<FoxPet>("/root/Base/Fox");
        var tex = SpriteFrames.GetFrameTexture(GetAnimation(), GetFrame());
        _slimeSprite = tex.GetSize();
        // Additional initialization code can go here
    }

    private void _Process(float delta)
    {
       UpdateSlimeLocation();
    }

    private void UpdateSlimeLocation()
    {
        Vector2 pos = Position;
        float groundY = _foxPetScript._workArea.Bottom - _slimeSprite.Y - _foxPetScript.BottomOffset;
        pos.Y = groundY;
        Position = new Vector2(Position.X, pos.Y);
        GD.Print("Slime position updated to: " + Position);
    }
}
