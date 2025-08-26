using Godot;
using System;

public partial class SlimeManager : Node2D
{
    private ClickThrough clickThrough;
    private FoxPet foxPet;
    [Export]
    public FoxDetection foxDetection;
    [Export]
    public AnimatedSprite2D slimePrefab;
    [Export]
    public Slime slimeScript;
    [Signal]
    public delegate void SlimeInRangeEventHandler();
    [Signal]
    public delegate void SlimeAttackedEventHandler();
    [Export]
    public Area2D foxDetectionArea;
    public ClickThrough ClickThrough
    {
        get => clickThrough;
        set => clickThrough = value;

    }

    public override void _Ready()
    {
        if (clickThrough == null)
        {
            clickThrough = GetNode<ClickThrough>("/root/Window/ClickThrough");
        }
        foxDetection.FoxDetected += OnFoxDetected;
        foxPet = GetNode<FoxPet>("/root/Window/Fox");
        foxPet.SlimeAttacked += OnSlimeAttacked;
        foxPet.StopSlimeMovement += OnStopSlimeMovement;
    }


    public void OnFoxDetected()
    {
        //emit slime in range
        EmitSignal(nameof(SlimeInRange));
    }

    private void OnSlimeAttacked()
    {
        GD.Print("Slime attacked by Fox");
        EmitSignal(nameof(SlimeAttacked));
    }

    private void OnStopSlimeMovement()
    {
        //flipH slime to face fox
        slimeScript.FlipH = foxPet.GlobalPosition.X < slimeScript.GlobalPosition.X;
        slimeScript._speed = 0;
        GD.Print("Slime movement stopped by Fox");
        foxPet.StopSlimeMovement -= OnStopSlimeMovement;
    }

}
