using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TeaSuite.KV.IO.Formatters;

/// <summary>
/// Provides a default implementation for <see cref="IEntryFormatter{TKey, TValue}"/>.
/// </summary>
/// <remarks>
/// The default implementation uses <see cref="IFormatter{T}"/> for both the <typeparamref name="TKey"/> and
/// <typeparamref name="TValue"/> type parameters.
/// </remarks>
public readonly struct DefaultEntryFormatter<TKey, TValue> : IEntryFormatter<TKey, TValue>
{
    private readonly IFormatter<TKey> keyFormatter;
    private readonly IFormatter<TValue> valueFormatter;

    /// <summary>
    /// Initializes a new instance of DefaultEntryFormatter.
    /// </summary>
    /// <param name="keyFormatter">
    /// An <see cref="IFormatter{T}"/> of <typeparamref name="TKey"/> to used to read/write keys.
    /// </param>
    /// <param name="valueFormatter">
    /// An <see cref="IFormatter{T}"/> of <typeparamref name="TValue"/> to used to read/write values.
    /// </param>
    public DefaultEntryFormatter(IFormatter<TKey> keyFormatter, IFormatter<TValue> valueFormatter)
    {
        this.keyFormatter = keyFormatter;
        this.valueFormatter = valueFormatter;
    }

    /// <inheritdoc/>
    public ValueTask<TKey> ReadKeyAsync(Stream source, CancellationToken cancellationToken)
    {
        return keyFormatter.ReadAsync(source, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask<TValue> ReadValueAsync(Stream source, CancellationToken cancellationToken)
    {
        return valueFormatter.ReadAsync(source, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask SkipReadValueAsync(Stream source, CancellationToken cancellationToken)
    {
        return valueFormatter.SkipReadAsync(source, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask WriteKeyAsync(TKey key, Stream destination, CancellationToken cancellationToken)
    {
        return keyFormatter.WriteAsync(key, destination, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask WriteValueAsync(TValue value, Stream destination, CancellationToken cancellationToken)
    {
        return valueFormatter.WriteAsync(value, destination, cancellationToken);
    }
}
