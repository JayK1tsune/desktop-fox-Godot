using Godot;
using System;


public partial class FoxButtons_UI : Button
{

    [Signal]
    public delegate void CloseUiEventHandler();

   

    public override void _Ready()
    {
        Connect("pressed", new Callable(this, nameof(_on_button_pressed)));
    }


    
    public void _on_button_pressed()
	{
        if(SlimeContainer.Instance.GetChildCount() >= 1)
        {
            GD.Print("Max Slimes Reached");
            return;
        }
		GD.Print("Spawn More Button Pressed");
        SlimeContainer.Instance.SpawnSlime();
        GD.Print("Slimes in container: " + SlimeContainer.Instance.GetSlimes().Count);

		EmitSignal(SignalName.CloseUi);
	}
}
