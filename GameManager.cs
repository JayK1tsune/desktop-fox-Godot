using Godot;
using System;

public partial class GameManager : Node2D
{
    PackedScene slimeScene = (PackedScene)ResourceLoader.Load("res://Scenes/Slime.tscn");



    private void SpawnSlime()
    {
        if (slimeScene == null)
        {
            GD.PrintErr("Slime scene not loaded.");
            return;
        }

        Node2D slimeInstance = (Node2D)slimeScene.Instantiate();
        if (slimeInstance != null)
        {
            AddChild(slimeInstance);
            slimeInstance.Position = new Vector2(100, 100); // Set initial position
            GD.Print("Slime spawned successfully.");
        }
        else
        {
            GD.PrintErr("Failed to instantiate slime scene.");
        }
    }
}
