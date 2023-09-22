using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

public readonly struct KeyTupleFormatter<TMajor, TMinor> : IKeyTupleFormatter<TMajor, TMinor>, IFormatter<KeyTuple<TMajor, TMinor>>
    where TMajor : IComparable<TMajor>
    where TMinor : IComparable<TMinor>
{
    private readonly IFormatter<TMajor> majorFormatter;
    private readonly IFormatter<TMinor> minorFormatter;

    public KeyTupleFormatter(
        IFormatter<TMajor> majorFormatter,
        IFormatter<TMinor> minorFormatter
        )
    {
        this.majorFormatter = majorFormatter;
        this.minorFormatter = minorFormatter;
    }

    /// <inheritdoc/>
    public async ValueTask<KeyTuple<TMajor, TMinor>> ReadAsync(Stream source, CancellationToken cancellationToken)
    {
        TMajor major = await majorFormatter.ReadAsync(source, cancellationToken).ConfigureAwaitLib();
        TMinor minor = await minorFormatter.ReadAsync(source, cancellationToken).ConfigureAwaitLib();

        return new KeyTuple<TMajor, TMinor>()
        {
            Major = major,
            Minor = minor,
        };
    }

    /// <inheritdoc/>
    public async ValueTask SkipReadAsync(Stream source, CancellationToken cancellationToken)
    {
        await majorFormatter.SkipReadAsync(source, cancellationToken).ConfigureAwaitLib();
        await minorFormatter.SkipReadAsync(source, cancellationToken).ConfigureAwaitLib();
    }

    /// <inheritdoc/>
    public async ValueTask WriteAsync(KeyTuple<TMajor, TMinor> value, Stream destination, CancellationToken cancellationToken)
    {
        await majorFormatter.WriteAsync(value.Major, destination, cancellationToken).ConfigureAwaitLib();
        await minorFormatter.WriteAsync(value.Minor, destination, cancellationToken).ConfigureAwaitLib();
    }
}
