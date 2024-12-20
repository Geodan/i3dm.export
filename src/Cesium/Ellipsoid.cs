namespace i3dm.export.Cesium;

public class Ellipsoid
{
    public Ellipsoid()
    {
        SemiMajorAxis = 6378137;
        SemiMinorAxis = 6356752.314245179;
        Eccentricity = 0.08181919084262157;
    }
    public double SemiMajorAxis { get; }
    public double SemiMinorAxis { get; }

    public double Eccentricity { get; }
}
