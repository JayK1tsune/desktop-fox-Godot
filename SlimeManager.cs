using Godot;
using System;

public partial class SlimeManager : Node2D
{
    [Export]
    ClickThrough clickThrough;
    [Export]
    FoxDetection foxDetection;
    [Export]
    FoxPet foxPet;
    [Export]
    public AnimatedSprite2D slimePrefab;
    [Signal]
    public delegate void SlimeInRangeEventHandler();
    [Signal]
    public delegate void SlimeAttackedEventHandler();
    public ClickThrough ClickThrough
    {
        get => clickThrough;
        set => clickThrough = value;

    }

    public override void _Ready()
    {
        // Ensure ClickThrough is initialized
        if (clickThrough == null)
        {
            clickThrough = GetNode<ClickThrough>("/root/Base/ClickThrough");
        }
        foxDetection.FoxDetected += OnFoxDetected;
        foxPet = GetNode<FoxPet>("/root/Base/Fox");
        foxPet.SlimeAttacked += OnSlimeAttacked;
    }


    public void OnFoxDetected()
    {
        EmitSignal(nameof(SlimeInRange));
    }
    
    private void OnSlimeAttacked()
    {
        GD.Print("Slime attacked by Fox");
        EmitSignal(nameof(SlimeAttacked));
    }

}
