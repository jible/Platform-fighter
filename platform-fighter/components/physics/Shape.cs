public abstract class Shape{
    /// <summary>
    /// Axis-aligned bounds of this shape, given the global position of its owner.
    /// </summary>
    public abstract void GetBounds(DMVector origin, out DMVector min, out DMVector max);
}

public class Square : Shape
{
    public DMVector size;

    // The origin is the center of the box.
    public override void GetBounds(DMVector origin, out DMVector min, out DMVector max)
    {
        DMVector half = size / 2;
        min = origin - half;
        max = origin + half;
    }
}

public class Circle : Shape
{
    public DM64 radius;

    // The origin is the center of the circle.
    public override void GetBounds(DMVector origin, out DMVector min, out DMVector max)
    {
        DMVector reach = new DMVector(radius, radius);
        min = origin - reach;
        max = origin + reach;
    }
}