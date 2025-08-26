using Godot;
using System;
using System.Collections.Generic;

public partial class SlimeContainer : Node
{
    public List<Node2D> slimes = new List<Node2D>();


    public override void _Ready()
    {
        foreach (Node2D child in GetChildren())
        {
            slimes.Add(child);
        }

        GD.Print("Slimes in container: " + slimes.Count);
    }
}
