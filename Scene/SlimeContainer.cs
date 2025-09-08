using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;


public partial class SlimeContainer : Node
{
    public List<Node2D> slimes = new List<Node2D>();
    [Export] public PackedScene slimePrefab;
    [Signal] public delegate void SlimeRemovedEventHandler(Node2D slime);

    public static SlimeContainer Instance { get; private set; }
    public override void _Ready()
    {
        Instance = this;
        foreach (Node2D child in GetChildren())
        {
            slimes.Add(child);
        }

        if (slimes.Count <= 0)
        {
            SpawnSlime();
        }

        GD.Print("Slimes in container: " + slimes.Count);
    }

    public List<Node2D> GetSlimes()
    {
        return slimes;
    }

    public void RemoveSlime(Node2D slime)
    {
        //find the slime in the list and remove it
        if (slimes.Contains(slime))
        {
            slime.QueueFree();
            slimes.Remove(slime);
            EmitSignal(nameof(SlimeRemoved), slime);
        }
        else
        {
            GD.Print("Slime not found in container");
            return;
        }

    }

    public void ClearSlimes()
    {
        foreach (var slime in slimes)
        {
            slime.QueueFree();
        }
        slimes.Clear();
    }
    public Node2D SpawnSlime()
    {
        Node2D newSlime = (Node2D)slimePrefab.Instantiate();
        AddChild(newSlime);
        slimes.Add(newSlime);
        return newSlime;
    }


}
