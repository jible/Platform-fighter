using Godot;
using System;

// A node the physics server moves and collides. Physics moves this node's own SimTransform,
// so a dynamic collision object should be a root of the simulation. Triggers and static objects can be children.
// Collected by the DeterministicWorld when it is built, so it never registers itself anywhere.
// objectType, isStatic and Layer are read when the world is built, so they must be set before then
public partial class PhysicsObject : DeterministicNode3D
{
    public Shape shape;
    public readonly SerializableProperty<DMVector> velocity = new();
    /// <summary>
    /// The Mask is what layer this object "sees". For collision, the non-static object should
    /// be mask the static one
    /// </summary>
    public int Mask;
    /// <summary>
    /// The Layer is what layer this object exists on.
    /// For collision, the static object should be masked by the non-static one
    /// </summary>
    public int Layer;
    public bool isStatic = false;
    // Whether the physics shape renderers should draw this object's shape
    public bool renderShape = true;
    // Color the physics shape renderers draw this object's shape with
    public Color renderColor = new Color(1f, 1f, 1f, 0.5f);
    // Only used by the physics server within a tick, so it isn't rollback state
    public int visitStamp;
    public ObjectType objectType;

    private readonly SerializableProperty<bool> serializedIsActive = new();
    public bool isActive
    {
        get => serializedIsActive.Value;
        set => serializedIsActive.Value = value;
    }

    // Read when the world is built. A dynamic body with this set has the physics server check for ground under it
    // at the end of every step, see PhysicsServer.ProbeGround
    public bool probesGround;
    private readonly SerializableProperty<bool> serializedOnGround = new();
    // Whether the last physics step found ground just below this body. Always false without probesGround
    public bool OnGround
    {
        get => serializedOnGround.Value;
        set => serializedOnGround.Value = value;
    }

    /// <summary>
    /// Emitted when this object has been entered by another object
    /// </summary>
    public event Action<PhysicsObject, PhysicsObject> Overlapping;

    /// <summary>
    /// Emitted when this body was stopped by another object.
    /// The DMVector is the side of the collision: zero on the axis that wasn't hit, and on the
    /// hit axis, the sign of this body's velocity relative to the other object's along it.
    /// </summary>
    public event Action<PhysicsObject, PhysicsObject, DMVector> Colliding;

    public enum ObjectType
    {
        TriggerBox,
        CollisionObject
    }

    public PhysicsObject()
    {
        isActive = true;
    }

    /// <summary>
    /// Call this to emit the Overlapping Event- when this is the trigger and detects other.
    /// </summary>
    /// <param name="other"></param>
    public void OnOverlap(PhysicsObject other)
    {
        Overlapping?.Invoke(this, other);
    }

    /// <summary>
    /// Call this to emit the Colliding event- when this body collided with other.
    /// </summary>
    /// <param name="other"></param>
    /// <param name="side">The side of the collision (see the Colliding event's doc comment).</param>
    public void OnCollide(PhysicsObject other, DMVector side)
    {
        Colliding?.Invoke(this, other, side);
    }
}
