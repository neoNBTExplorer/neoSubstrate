namespace Substrate.Nbt;

/// <summary>
///     A concrete <see cref="SchemaNode" /> representing a <see cref="TagNodeLongArray" />.
/// </summary>
public sealed class SchemaNodeLongArray : SchemaNode
{
    /// <summary>
    ///     Constructs a new <see cref="SchemaNodeLongArray" /> representing a <see cref="TagNodeLongArray" /> named
    ///     <paramref name="name" />.
    /// </summary>
    /// <param name="name">The name of the corresponding <see cref="TagNodeIntArray" />.</param>
    public SchemaNodeLongArray(string name)
        : base(name)
    {
        Length = 0;
    }

    /// <summary>
    ///     Constructs a new <see cref="SchemaNodeLongArray" /> with additional options.
    /// </summary>
    /// <param name="name">The name of the corresponding <see cref="TagNodeLongArray" />.</param>
    /// <param name="options">One or more option flags modifying the processing of this node.</param>
    public SchemaNodeLongArray(string name, SchemaOptions options)
        : base(name, options)
    {
        Length = 0;
    }

    /// <summary>
    ///     Constructs a new <see cref="SchemaNodeLongArray" /> representing a <see cref="TagNodeLongArray" /> named
    ///     <paramref name="name" /> with expected length <paramref name="length" />.
    /// </summary>
    /// <param name="name">The name of the corresponding <see cref="TagNodeIntArray" />.</param>
    /// <param name="length">The expected length of corresponding byte array.</param>
    public SchemaNodeLongArray(string name, int length)
        : base(name)
    {
        Length = length;
    }

    /// <summary>
    ///     Constructs a new <see cref="SchemaNodeLongArray" /> with additional options.
    /// </summary>
    /// <param name="name">The name of the corresponding <see cref="TagNodeLongArray" />.</param>
    /// <param name="length">The expected length of corresponding byte array.</param>
    /// <param name="options">One or more option flags modifying the processing of this node.</param>
    public SchemaNodeLongArray(string name, int length, SchemaOptions options)
        : base(name, options)
    {
        Length = length;
    }

    /// <summary>
    ///     Gets the expected length of the corresponding long array.
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     Indicates whether there is an expected length of the corresponding int array.
    /// </summary>
    public bool HasExpectedLength => Length > 0;

    /// <summary>
    ///     Constructs a default <see cref="TagNodeLongArray" /> satisfying the constraints of this node.
    /// </summary>
    /// <returns>A <see cref="TagNodeString" /> with a sensible default value.</returns>
    public override TagNode BuildDefaultTree()
    {
        return new TagNodeLongArray(new long[Length]);
    }
}