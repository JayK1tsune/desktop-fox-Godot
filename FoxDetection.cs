using Godot;
using System;

public partial class FoxDetection : Area2D
{
    private Area2D _foxPetScript;
    [Export]
    private Area2D _detectionArea;
    [Signal]
    public delegate void FoxDetectedEventHandler();

    public override void _Ready()
    {
        _detectionArea = this;
        _foxPetScript = GetNode<Area2D>("/root/Base/Fox/ClickLogic");
        _detectionArea.Connect("body_entered", new Callable(this, nameof(DetectFox)));

    }
    public override void _Process(double delta)
    {
        // Check for fox detection every frame
        DetectFox();
    }

    public void DetectFox()
    {
        if (_detectionArea.GetOverlappingAreas().Contains(_foxPetScript))
        {
            EmitSignal(nameof(FoxDetected));
        }
    }

}
