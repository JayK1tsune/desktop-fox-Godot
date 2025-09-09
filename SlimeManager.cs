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
        //check to see if the connected signal is null
        if (foxDetection == null)
        {
            GD.Print("FoxDetection is null");
        }
    }


    public void OnFoxDetected()
    {
        //emit slime in range
        EmitSignal(nameof(SlimeInRange));
    }

    private void OnSlimeAttacked()
    {
        GD.Print("Slime attacked signal received by SlimeManager");
        foreach (var child in GetChildren())
        {
            if (child is Slime attackedSlime)
            {
                attackedSlime.SlimeAttacked();
                //unsubscribe from further attacks to prevent multiple attacks
                foxPet.SlimeAttacked -= OnSlimeAttacked;
                GD.Print("Slime attacked signal received by SlimeManager");
                break; // Attack only one slime
            }
            else
            {
                GD.Print("No slime found to attack");
            }
        }
        
    }

    private void OnStopSlimeMovement()
    {
        GD.Print("Stop slime movement signal received by SlimeManager");
        foreach (var child in GetChildren())
        {
            if (child is Slime attackedSlime)
            {
                attackedSlime._speed = 0;
                //flip the slime to face the fox
                attackedSlime.FlipH = foxPet.Position.X < attackedSlime.Position.X;
                foxPet.StopSlimeMovement -= OnStopSlimeMovement;
                GD.Print("Slime movement stopped");
            }
            else
            {
                GD.Print("No slime found to stop movement");
            }
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
