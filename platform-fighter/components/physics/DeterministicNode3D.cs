using Godot;

// A node that exists in the simulation. Its SimTransform holds the real position, and the Node3D position is only a display copy of it.
// The transform inherits from the parent's when the parent is also a DeterministicNode3D, otherwise it's world space.
// Nodes must not be reparented during a match, since the parent isn't rollback state
public partial class DeterministicNode3D : Node3D
{
    public readonly DeterministicTransform SimTransform = new();

    // Sealed so a subclass can't skip linking the parent. Subclasses use OnEnterTree
    public sealed override void _EnterTree()
    {
        SimTransform.Parent = (GetParent() as DeterministicNode3D)?.SimTransform;
        OnEnterTree();
    }

    // Sealed so a subclass can't skip the display sync. Subclasses use OnProcess.
    // Runs after the play manager ticks the world, as long as the play manager has the lower ProcessPriority
    public sealed override void _Process(double delta)
    {
        SyncDisplay();
        OnProcess(delta);
    }

    protected virtual void OnEnterTree() { }
    protected virtual void OnProcess(double delta) { }

    // Display only
    private void SyncDisplay()
    {
        // Godot's hierarchy matches the sim's here, so it adds the parent itself
        if (SimTransform.Parent != null) Position = SimTransform.LocalPosition.ToVector3();
        // Ignores any plain Node3D above, so the display can't pick up an offset the sim doesn't have
        else GlobalPosition = SimTransform.GlobalPosition.ToVector3();
    }
}
