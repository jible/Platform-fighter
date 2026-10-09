// A position in the simulation, relative to its parent transform, or world space when it has none.
// The global position is worked out on every read instead of cached, so a rollback only has to restore local positions
public class DeterministicTransform
{
    private readonly SerializableProperty<DMVector> localPosition = new();
    // Null when this is a root of the simulation. Not rollback state, so it must not change during a match
    public DeterministicTransform Parent;

    public DMVector LocalPosition
    {
        get => localPosition.Value;
        set => localPosition.Value = value;
    }

    public DMVector GlobalPosition
    {
        get => Parent == null ? LocalPosition : Parent.GlobalPosition + LocalPosition;
        set => LocalPosition = Parent == null ? value : value - Parent.GlobalPosition;
    }
}
