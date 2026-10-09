// Deterministic overlap tests on plain shapes and positions. Knows nothing about PhysicsObjects,
// so the physics server and anything else, like hit detection, share the exact same math.
// Edges that only touch don't count as overlapping
public static class Overlap
{
    // Origins are the global centers of the shapes
    public static bool Shapes(Shape a, DMVector aOrigin, Shape b, DMVector bOrigin)
    {
        if (a is Square aSquare && b is Square bSquare)
        {
            return Squares(aSquare, aOrigin, bSquare, bOrigin);
        }
        // Circles aren't supported yet
        return false;
    }

    public static bool Squares(Square a, DMVector aOrigin, Square b, DMVector bOrigin)
    {
        a.GetBounds(aOrigin, out DMVector aMin, out DMVector aMax);
        b.GetBounds(bOrigin, out DMVector bMin, out DMVector bMax);
        return Bounds(aMin, aMax, bMin, bMax);
    }

    // Axis aligned boxes given by center and full size
    public static bool Boxes(DMVector aCenter, DMVector aSize, DMVector bCenter, DMVector bSize)
    {
        DMVector aHalf = aSize / 2;
        DMVector bHalf = bSize / 2;
        return Bounds(aCenter - aHalf, aCenter + aHalf, bCenter - bHalf, bCenter + bHalf);
    }

    public static bool Bounds(DMVector aMin, DMVector aMax, DMVector bMin, DMVector bMax)
    {
        return aMin.x < bMax.x
            && aMax.x > bMin.x
            && aMin.y < bMax.y
            && aMax.y > bMin.y;
    }
}
