using System;

namespace TeaSuite.KV;

public readonly record struct KeyTuple<TMajor, TMinor> : IComparable<KeyTuple<TMajor, TMinor>>
    where TMajor : IComparable<TMajor>
    where TMinor : IComparable<TMinor>
{
    public TMajor Major { get; init; }
    public TMinor Minor { get; init; }

    public int CompareTo(KeyTuple<TMajor, TMinor> other)
    {
        int major = Major.CompareTo(other.Major);
        if (0 != major)
        {
            return major;
        }

        return Minor.CompareTo(other.Minor);
    }
}
