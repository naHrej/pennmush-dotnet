namespace PennMUSH.Space;

public sealed class CartesianCoordinates
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public CartesianCoordinates()
    {
    }

    public CartesianCoordinates(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public sealed class SphericalCoordinates
{
    public double Bearing { get; set; }
    public double Elevation { get; set; }
    public double Range { get; set; }

    public SphericalCoordinates()
    {
    }

    public SphericalCoordinates(double bearing, double elevation, double range)
    {
        Bearing = bearing;
        Elevation = elevation;
        Range = range;
    }
}
