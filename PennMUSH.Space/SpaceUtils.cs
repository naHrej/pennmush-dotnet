namespace PennMUSH.Space;

public static class SpaceUtils
{
    public static double CalculateRange(CartesianCoordinates from, CartesianCoordinates to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var dz = to.Z - from.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>Aspace xy2bearing: 0 is +X and 90 is +Y.</summary>
    public static double CalculateBearing(CartesianCoordinates from, CartesianCoordinates to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var bearing = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        return bearing < 0 ? bearing + 360.0 : bearing;
    }

    public static double CalculateElevation(CartesianCoordinates from, CartesianCoordinates to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var dz = to.Z - from.Z;
        var range = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return range > 0 ? Math.Asin(dz / range) * 180.0 / Math.PI : 0.0;
    }
}
