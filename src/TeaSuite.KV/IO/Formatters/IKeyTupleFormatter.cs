using System;

namespace TeaSuite.KV.IO.Formatters;

public interface IKeyTupleFormatter<TMajor, TMinor> : IFormatter<KeyTuple<TMajor, TMinor>>
    where TMajor : IComparable<TMajor>
    where TMinor : IComparable<TMinor>
{
}
