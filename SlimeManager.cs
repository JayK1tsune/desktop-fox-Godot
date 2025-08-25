using Godot;
using System;

public partial class SlimeManager : Node2D
{
    [Export]
    ClickThrough clickThrough;
    [Export]
    public FoxDetection foxDetection;
    [Export]
    FoxPet foxPet;
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
            clickThrough = GetNode<ClickThrough>("/root/Base/ClickThrough");
        }
        foxDetection.FoxDetected += OnFoxDetected;
        foxPet = GetNode<FoxPet>("/root/Base/Fox");
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
        slimeScript._speed = 0;
        GD.Print("Slime movement stopped by Fox");
        foxPet.StopSlimeMovement -= OnStopSlimeMovement;
    }

}
