using System;

namespace Substrate.Core;

public struct RegionKey : IEquatable<RegionKey>
{
    public static RegionKey InvalidRegion = new(int.MinValue, int.MinValue);

    public int X { get; }

    public int Z { get; }

    public RegionKey(int _rx, int _rz)
    {
        X = _rx;
        Z = _rz;
    }

    public bool Equals(RegionKey ck)
    {
        return X == ck.X && Z == ck.Z;
    }

    public override bool Equals(object o)
    {
        try
        {
            return this == (RegionKey)o;
        }
        catch
        {
            return false;
        }
    }

    public override int GetHashCode()
    {
        var hash = 23;
        hash = hash * 37 + X;
        hash = hash * 37 + Z;
        return hash;
    }

    public static bool operator ==(RegionKey k1, RegionKey k2)
    {
        return k1.X == k2.X && k1.Z == k2.Z;
    }

    public static bool operator !=(RegionKey k1, RegionKey k2)
    {
        return k1.X != k2.X || k1.Z != k2.Z;
    }

    public override string ToString()
    {
        return "(" + X + ", " + Z + ")";
    }
}
