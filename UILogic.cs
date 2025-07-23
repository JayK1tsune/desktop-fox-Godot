using Godot;
using System;

public partial class UILogic : Control
{
    [Signal]
    public delegate void SpawnMorePressedEventHandler();

    public void _on_button_pressed()
    {
        EmitSignal("SpawnMorePressed");
    }
}
