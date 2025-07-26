using Godot;
using System;

public partial class SlimeManager : Node2D
{
    [Export]
    ClickThrough clickThrough;
    [Export]
    FoxDetection foxDetection;
    [Signal]
    public delegate void SlimeInRangeEventHandler();
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
    }


    public void OnFoxDetected()
    {
        GD.Print("Fox detected by SlimeManager");
        EmitSignal(nameof(SlimeInRange));
    }

}
