using Godot;
using System;

public partial class SlimeManager : Node2D
{
    private ClickThrough clickThrough;
    private FoxPet foxPet;

    [Export] public FoxDetection foxDetection;
    [Export] public AnimatedSprite2D slimePrefab;
    [Export] public Slime slimeScript;

    [Signal] public delegate void SlimeInRangeEventHandler();
    [Signal] public delegate void SlimeAttackedEventHandler(Node2D slime);
    [Signal] public delegate void StopSlimeMovementEventHandler(Node2D slime);

    [Export] public Area2D foxDetectionArea;

    public ClickThrough ClickThrough
    {
        get => clickThrough;
        set => clickThrough = value;
    }

    public override void _Ready()
    {
        if (clickThrough == null)
            clickThrough = GetNode<ClickThrough>("/root/Window/ClickThrough");

        if (foxDetection == null)
            GD.Print("FoxDetection is null");

        foxDetection.FoxDetected += OnFoxDetected;
        foxPet = GetNode<FoxPet>("/root/Window/Fox");

        // Subscribe to fox signals
        foxPet.SlimeAttacked += OnSlimeAttacked;
        foxPet.StopSlimeMovement += OnStopSlimeMovement;
    }

    private void OnFoxDetected()
    {
        EmitSignal(nameof(SlimeInRange));
    }

    // Called when the fox attacks a slime
    private void OnSlimeAttacked(Node2D slime)
    {
        if (slime == null)
            return;

        // Get the AnimatedSprite2D child that has the Slime.cs script
        var animatedSprite = slime.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (animatedSprite is Slime slimeScript)
        {
            slimeScript.SlimeAttacked();
            //disconmnect the signal after attacking
            foxPet.SlimeAttacked -= OnSlimeAttacked;
            GD.Print($"Slime {slime.Name} attacked!");
        }
    }

    // Called when the fox tells a slime to stop moving
    private void OnStopSlimeMovement(Node2D slime)
    {
        if (slime == null)
            return;

        var animatedSprite = slime.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (animatedSprite is Slime slimeScript)
        {
            slimeScript.FlipH = foxPet.Position.X < slimeScript.Position.X;
            slimeScript._speed = 0;
            //disconnect the signal after stopping movement
            foxPet.StopSlimeMovement -= OnStopSlimeMovement;
            GD.Print($"Slime {slime.Name} movement stopped!");
        }
    }

    public override void _ExitTree()
    {
        if (foxPet != null)
        {
            foxPet.SlimeAttacked -= OnSlimeAttacked;
            foxPet.StopSlimeMovement -= OnStopSlimeMovement;
        }
    }
}
