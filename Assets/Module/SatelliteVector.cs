using System.Linq;

public class SatelliteVector
{
    public int[] Values { get; private set; }

    public SatelliteVector(int x, int y, int z)
    {
        Values = new int[] { x, y, z };
        Simplify();
    }

    public SatelliteVector() : this(0, 0, 0) { }

    public SatelliteVector Simplify()
    {
        int minVal = Values.Min();
        Values = Values.Select(x => x - minVal).ToArray();
        return this;
    }

    public SatelliteVector Primitive()
    {
        if (RingLength() <= 0)
            return new SatelliteVector();

        int a = Values.Max();
        int b = Values.Sum() - a;
        while (a * b > 0)
        {
            int s = a % b;
            a = b;
            b = s;
        }

        return new SatelliteVector(Values[0] / a, Values[1] / a, Values[2] / a);
    }

    public int RingLength()
    {
        return Values.Max();
    }

    public static SatelliteVector RotationToVector(int rotation)
    {
        rotation = (rotation % 6 + 6) % 6;
        switch (rotation)
        {
            case 0:
                return new SatelliteVector(1, 0, 0);
            case 1:
                return new SatelliteVector(1, 1, 0);
            case 2:
                return new SatelliteVector(0, 1, 0);
            case 3:
                return new SatelliteVector(0, 1, 1);
            case 4:
                return new SatelliteVector(0, 0, 1);
            case 5:
                return new SatelliteVector(1, 0, 1);
            default:
                return new SatelliteVector();
        }
    }

    public static SatelliteVector operator +(SatelliteVector left, SatelliteVector right)
    {
        return new SatelliteVector(left.Values[0] + right.Values[0], left.Values[1] + right.Values[1], left.Values[2] + right.Values[2]);
    }

    public static SatelliteVector operator -(SatelliteVector left, SatelliteVector right)
    {
        return new SatelliteVector(left.Values[0] - right.Values[0], left.Values[1] - right.Values[1], left.Values[2] - right.Values[2]);
    }

    public bool Matches(SatelliteVector other)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Values[i] != other.Values[i])
                return false;
        }
        return true;
    }

    public override string ToString()
    {
        return "(" + Values.Join(",") + ")";
    }
}