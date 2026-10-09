using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// A system the world ticks after physics, like hit detection. Systems tick in the order they're added.
// Add them once while the match is built, so the tick order is the same on every machine
public interface IWorldSystem
{
    void Tick();
}

// Draws debug or display info every frame after the ticks run, like the physics shapes or hitboxes.
// Display only, so it must never change the simulation. Draws through the world's BoxDrawer, which shows the frame once every renderer has drawn
public interface IWorldRenderer
{
    void Draw();
}

// Anything in the simulation that other things need to refer to, like a hit group remembering who it hit.
// Ids are handed out once by the DeterministicWorld when it's built, in collection order, and never change after,
// so they aren't rollback state. Don't assume an id matches a player number
public interface IEntity
{
    byte EntityId { get; set; }
}

// The deterministic simulation. Everything in it is instanced under the root before it's built, then collected in one walk.
// Nothing joins or leaves after that, so there's no registering, only the arrays built here.
// Knows nothing about the game: game specific systems are added by whatever builds the world, for example
//     world.AddSystem(new HitboxManager(world.All<Character>().OrderBy(c => c.PlayerIndex).ToArray()));
//     world.AddRenderer(new RuntimeBoxRenderer(hitboxManager, world.BoxDrawer));
// Rollback state registers when it's constructed, so call SerializableDataManager.Reset before instancing anything under the root.
// The root must be in the scene tree, since the debug drawer draws into its 3D world.
// Dispose when the match ends, or the debug boxes stay on screen
public class DeterministicWorld : IDisposable
{
    public readonly TickManager TickManager;
    public readonly PhysicsServer PhysicsServer;
    // Shared by every renderer
    public readonly DebugBoxDrawer BoxDrawer;
    public readonly RuntimePhysicsShapeRenderer ShapeRenderer;
    // Indexed by entity id
    public readonly IEntity[] Entities;

    // Every node under the root, in collection order
    private readonly Node[] nodes;
    // Ticked in collection order
    private readonly ITickable[] tickables;
    private readonly List<IWorldSystem> systems = new();
    private readonly List<IWorldRenderer> renderers = new();

    public DeterministicWorld(Node root, InputManager inputManager)
    {
        nodes = Collect(root);
        tickables = All<ITickable>().ToArray();
        Entities = AssignEntityIds(All<IEntity>().ToArray());

        PhysicsObject[] physicsObjects = All<PhysicsObject>().ToArray();
        PhysicsServer = new PhysicsServer(physicsObjects);
        BoxDrawer = new DebugBoxDrawer(root);
        ShapeRenderer = new RuntimePhysicsShapeRenderer(physicsObjects, BoxDrawer);
        AddRenderer(ShapeRenderer);
        TickManager = new TickManager(inputManager, Step);
    }

    // Collected nodes of a type, in collection order. For building systems
    public IEnumerable<T> All<T>() => nodes.OfType<T>();

    // Ticks after physics and every system added before it
    public void AddSystem(IWorldSystem system) => systems.Add(system);

    // Draws over every renderer added before it
    public void AddRenderer(IWorldRenderer renderer) => renderers.Add(renderer);

    // Runs as many ticks as the elapsed time calls for, then draws the result.
    // Nodes sync their own display in _Process, after this
    public int Update(double deltaTime)
    {
        int ticks = TickManager.Advance(deltaTime);
        // Every frame, ticked or not, since the boxes only stay drawn until the next submit
        foreach (var renderer in renderers)
        {
            renderer.Draw();
        }
        BoxDrawer.Submit();
        return ticks;
    }

    public void Dispose()
    {
        TickManager.Dispose();
        BoxDrawer.Dispose();
    }

    // One tick of the simulation. Rollback saving and input happen around it in the tick manager
    private void Step()
    {
        foreach (var tickable in tickables)
        {
            tickable.Tick();
        }
        PhysicsServer.Tick();
        // After physics, so systems like hit detection see final positions
        foreach (var system in systems)
        {
            system.Tick();
        }
    }

    // Breadth first, in child order, so every machine collects in the same order.
    // Nodes with ProcessMode Disabled, and everything under them, aren't part of the world
    private static Node[] Collect(Node root)
    {
        var collected = new List<Node>();
        var queue = new Queue<Node>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            if (current.ProcessMode == Node.ProcessModeEnum.Disabled) continue;

            collected.Add(current);
            foreach (Node child in current.GetChildren())
            {
                queue.Enqueue(child);
            }
        }
        return collected.ToArray();
    }

    // Ids are positions in collection order, so they match on every machine
    private static IEntity[] AssignEntityIds(IEntity[] entities)
    {
        // Hit groups remember who they hit with one bit per entity
        // if (entities.Length > HitGroup.MaxEntities)
        // {
        //     GD.PushError($"The world has {entities.Length} entities, past the {HitGroup.MaxEntities} hit groups can track");
        // }
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i].EntityId = (byte)i;
        }
        return entities;
    }
}
