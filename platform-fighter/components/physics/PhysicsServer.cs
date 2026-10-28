using System.Collections.Generic;
using System.Linq;

// Owned by the DeterministicWorld, which hands it every physics object once when the world is built.
// The sets of objects never change after that, so they're sorted into arrays up front
public class PhysicsServer
{
    public DM64 Epsilon = new(.0001f);
    // How far below a body the ground probe reaches. A body is pushed out to Epsilon above what it lands on, so it must be more
    public DM64 GroundProbeDistance = new(.001f);

    private const int LayerCount = 32;
    // Every object, in the world's collection order
    private readonly PhysicsObject[] all;
    // Trigger boxes, iterated by the trigger pass
    private readonly PhysicsObject[] triggers;
    // Collision objects that get moved and pushed out of static ones
    private readonly PhysicsObject[] dynamicBodies;
    // Dynamic bodies that want OnGround kept up to date
    private readonly PhysicsObject[] groundProbers;
    // Triggers can overlap anything, but only static colliders are ever collided against
    private readonly PhysicsObject[][] bucketByLayer;
    private readonly PhysicsObject[][] staticBucketByLayer;

    private enum Axis
    {
        X,Y
    }
    private static readonly Axis[] axes = {Axis.X, Axis.Y};

    // objectType, isStatic and Layer are read here, so they must be set before the world is built
    public PhysicsServer(IReadOnlyList<PhysicsObject> objects)
    {
        all = objects.ToArray();
        triggers = all.Where(obj => obj.objectType == PhysicsObject.ObjectType.TriggerBox).ToArray();
        dynamicBodies = all.Where(obj => obj.objectType == PhysicsObject.ObjectType.CollisionObject && !obj.isStatic).ToArray();
        groundProbers = dynamicBodies.Where(obj => obj.probesGround).ToArray();
        PhysicsObject[] staticColliders = all.Where(obj => obj.objectType == PhysicsObject.ObjectType.CollisionObject && obj.isStatic).ToArray();

        bucketByLayer = BucketByLayer(all);
        staticBucketByLayer = BucketByLayer(staticColliders);
    }

    private static PhysicsObject[][] BucketByLayer(PhysicsObject[] objects)
    {
        var buckets = new PhysicsObject[LayerCount][];
        for (int i = 0; i < LayerCount; i++)
        {
            int layerBit = 1 << i;
            buckets[i] = objects.Where(obj => (obj.Layer & layerBit) != 0).ToArray();
        }
        return buckets;
    }

    // Should be pretty much the last thing that happens every frame
    public void Tick()
    {
        collisionEvents.Clear();
        triggerEvents.Clear();

        foreach (Axis axis in axes)
        {
            ApplyVelocity(axis);
            ResolveAllCollisions(axis);
        }

        // After both axes, so it sees where bodies came to rest
        ProbeAllGround();

        DetectTriggers();

        DispatchSignals();
    }
    
    
    //A number that constantly increases each time an object queries its other objects
    // The only use for this is object A iterating over the objects it masks and 
    // making sure it doesn't visit them twice if it masks them on 2 different layers.
    private int Stamp = 0;

    // Signals are recorded while the passes run and dispatched together afterwards,
    // so listeners never run while positions are still being resolved
    private struct CollisionEvent
    {
        public PhysicsObject Body;
        public PhysicsObject Solid;
        // Which axis the collision happened on, and which side of it Body was on,
        // as the sign of Body's velocity relative to Solid's along that axis
        public DMVector Side;
    }
    private struct TriggerEvent
    {
        public PhysicsObject Trigger;
        public PhysicsObject Target;
    }
    private readonly List<CollisionEvent> collisionEvents = new();
    private readonly List<TriggerEvent> triggerEvents = new();


    // Velocity is in units per second, this is the only place it's converted to units per tick
    private void ApplyVelocity(Axis axis)
    {   
        foreach (var physicsObject in all)
            {
                var x = axis == Axis.X?physicsObject.velocity.Value.x: new DM64(0);
                var y = axis == Axis.Y?physicsObject.velocity.Value.y: new DM64(0);
                // Local, since moving the local position moves the global one by the same amount
                physicsObject.SimTransform.LocalPosition += new DMVector( x, y ) / TickManager.TicksPerSecond;
            }
    }

    private void ResolveAllCollisions(Axis axis)
    {
        // Only active dynamic bodies query, and only against static colliders
        foreach (var A in dynamicBodies)
        {
            if (!A.isActive) continue;
            int stamp = ++Stamp;
            for (int layerIndex = 0; layerIndex < staticBucketByLayer.Length; layerIndex++)
            {
                // Skip layers this object doesn't mask
                if ((A.Mask & (1 << layerIndex)) == 0) continue;
                foreach (var B in staticBucketByLayer[layerIndex])
                {
                    if (!B.isActive || B.visitStamp == stamp) continue;
                    B.visitStamp = stamp;
                    ResolveCollision(A, B, axis);
                }
            }
        }
    }

    private void ProbeAllGround()
    {
        foreach (var body in groundProbers)
        {
            body.OnGround = body.isActive && ProbeGround(body, GroundProbeDistance);
        }
    }

    // Whether an active static collider the body masks is within distance below it. Probes with a thin box as wide as
    // the body under its bottom edge, so standing with any part of it over ground counts, ledges included.
    // One bounds test per collider on the masked layers, the same as one collision query
    public bool ProbeGround(PhysicsObject body, DM64 distance)
    {
        body.shape.GetBounds(body.SimTransform.GlobalPosition, out DMVector min, out DMVector max);
        DMVector probeMin = new(min.x, min.y - distance);
        DMVector probeMax = new(max.x, min.y);

        int stamp = ++Stamp;
        for (int layerIndex = 0; layerIndex < staticBucketByLayer.Length; layerIndex++)
        {
            if ((body.Mask & (1 << layerIndex)) == 0) continue;
            foreach (var solid in staticBucketByLayer[layerIndex])
            {
                if (!solid.isActive || solid.visitStamp == stamp) continue;
                solid.visitStamp = stamp;
                solid.shape.GetBounds(solid.SimTransform.GlobalPosition, out DMVector solidMin, out DMVector solidMax);
                if (Overlap.Bounds(probeMin, probeMax, solidMin, solidMax)) return true;
            }
        }
        return false;
    }

    private void DetectTriggers()
    {
        // Only active triggers query, against anything on the layers they mask
        foreach (var A in triggers)
        {
            if (!A.isActive) continue;
            int stamp = ++Stamp;
            for (int layerIndex = 0; layerIndex < bucketByLayer.Length; layerIndex++)
            {
                if ((A.Mask & (1 << layerIndex)) == 0) continue;
                foreach (var B in bucketByLayer[layerIndex])
                {
                    if (A == B || !B.isActive || B.visitStamp == stamp) continue;
                    B.visitStamp = stamp;
                    checkTrigger(A, B);
                }
            }
        }
    }

    // Only records for a. The reverse direction is recorded when b's own query reaches a, so a mutual pair isn't recorded twice
    public void checkTrigger(PhysicsObject a, PhysicsObject b)
    {
        if (Overlaps(a, b))
        {
            triggerEvents.Add(new TriggerEvent { Trigger = a, Target = b });
        }
    }

    private void ResolveCollision(PhysicsObject a, PhysicsObject b, Axis axis)
    {
        // a is a dynamic body that masks b's layer, and b is a static collider
        if (Overlaps(a, b) && HandleCollision(a, b, axis, out DMVector side))
        {
            collisionEvents.Add(new CollisionEvent { Body = a, Solid = b, Side = side });
        }
    }

    // The side of the collision: zero on the axis the collision didn't happen on, and on the
    // collision axis, the sign of Body's velocity relative to Solid's (which side Body approached from)
    private static DMVector GetCollisionSide(PhysicsObject a, PhysicsObject b, Axis axis)
    {
        if (axis == Axis.X)
        {
            return new DMVector((a.velocity.Value.x - b.velocity.Value.x).Sign(), new DM64(0));
        }
        return new DMVector(new DM64(0), (a.velocity.Value.y - b.velocity.Value.y).Sign());
    }

    /// <summary>
    /// Handles the case such that a is a trigger and it masks b and overlaps b - Emits signal that a has been triggered
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public void DispatchTrigger(PhysicsObject a, PhysicsObject b)
    {
        // Already checked for overlap- just handle it
        a.OnOverlap(b);
    }

    // Emits everything recorded this tick, collisions first and then triggers, in the order they were detected
    private void DispatchSignals()
    {
        for (int i = 0; i < collisionEvents.Count; i++)
        {
            DispatchCollision(collisionEvents[i].Body, collisionEvents[i].Solid, collisionEvents[i].Side);
        }
        for (int i = 0; i < triggerEvents.Count; i++)
        {
            DispatchTrigger(triggerEvents[i].Trigger, triggerEvents[i].Target);
        }
    }

    /// <summary>
    /// Emits the signal that body was stopped by solid
    /// </summary>
    public void DispatchCollision(PhysicsObject body, PhysicsObject solid, DMVector side)
    {
        body.OnCollide(solid, side);
    }

    // Which way to push a out of b along one axis: back against a's motion, or, if a isn't moving on that axis
    // (a platform moved into it, or it spawned overlapping), away from b's center
    private static DM64 PushDirection(DM64 velocity, DM64 aPosition, DM64 bPosition)
    {
        DM64 direction = -velocity.Sign();
        if (direction == 0) direction = (aPosition - bPosition).Sign();
        if (direction == 0) direction = new DM64(1);
        return direction;
    }

    private bool HandleCollision(PhysicsObject a, PhysicsObject b, Axis axis, out DMVector side)
    {
        side = DMVector.zero;
        // Resolve the edges of the collision by moving the non-static object out of the static one
        // In this case, A is dynamic, b is static
        // Thus a will be repelled from b's surface
        if ( !(a.shape is Square) || !(b.shape is Square) ){
            return false; // Just don't do anything if they aren't squares for now
        }


        Square aSquare = (Square)a.shape;
        Square bSquare = (Square)b.shape;

        // Global on both, since either one may be the child of another object
        DMVector currentAPosition = a.SimTransform.GlobalPosition;
        DMVector bPosition = b.SimTransform.GlobalPosition;

        // Assuming the 2 are colliding because it has already been checked,
        // A is the non-static one, so its gets moved
        // It get slid to the slide opposite to the magnitude of the velocity
        DM64 newX = currentAPosition.x;
        DM64 newY = currentAPosition.y;
        if (axis == Axis.X){
            newX = bPosition.x +
            (PushDirection(a.velocity.Value.x, currentAPosition.x, bPosition.x) * (Epsilon + aSquare.size.x/2 + bSquare.size.x/2));
        }else if (axis == Axis.Y){
            newY = bPosition.y +
            (PushDirection(a.velocity.Value.y, currentAPosition.y, bPosition.y) * (Epsilon + aSquare.size.y/2 + bSquare.size.y/2));
        }

        a.SimTransform.GlobalPosition = new(newX, newY);

        side = GetCollisionSide(a, b, axis);
        return true;
    }


    // Physics objects overlap when their shapes do, at their transforms' global positions
    private static bool Overlaps(PhysicsObject a, PhysicsObject b)
    {
        return Overlap.Shapes(a.shape, a.SimTransform.GlobalPosition, b.shape, b.SimTransform.GlobalPosition);
    }
}