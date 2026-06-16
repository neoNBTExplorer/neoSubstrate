using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

/// <summary>
///     Represents a TileTick record, which is used to queue a block for processing in the future.
/// </summary>
public class TileTick : INbtObject<TileTick>, ICopyable<TileTick>
{
    private TagNodeCompound _source;

    /// <summary>
    ///     Constructs an empty <see cref="TileTick" /> object.
    /// </summary>
    public TileTick()
    {
    }

    /// <summary>
    ///     Constructs a <see cref="TileTick" /> by copying an existing one.
    /// </summary>
    /// <param name="tt">The <see cref="TileTick" /> to copy.</param>
    public TileTick(TileTick tt)
    {
        ID = tt.ID;
        Ticks = tt.Ticks;
        X = tt.X;
        Y = tt.Y;
        Z = tt.Z;

        if (tt._source != null) _source = tt._source.Copy() as TagNodeCompound;
    }

    /// <summary>
    ///     Gets or sets the ID (type) of the block that this <see cref="TileTick" /> is associated with.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    ///     Gets or sets the number of ticks remaining until the associated block is processed.
    /// </summary>
    public int Ticks { get; set; }

    /// <summary>
    ///     Gets or sets the global X-coordinate of the block that this <see cref="TileTick" /> is associated with.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    ///     Gets or sets the global Y-coordinate of the block that this <see cref="TileTick" /> is associated with.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    ///     Gets or sets the global Z-coordinate of the block that this <see cref="TileTick" /> is associated with.
    /// </summary>
    public int Z { get; set; }

    #region ICopyable<TileTick> Members

    /// <summary>
    ///     Creates a deep-copy of the <see cref="TileTick" /> including any data defined in a subtype.
    /// </summary>
    /// <returns>A deep-copy of the <see cref="TileTick" />.</returns>
    public virtual TileTick Copy()
    {
        return new TileTick(this);
    }

    #endregion

    /// <summary>
    ///     Checks whether the <see cref="TileTick" /> is located (associated with a block) at the specific global coordinates.
    /// </summary>
    /// <param name="x">The global X-coordinate to test.</param>
    /// <param name="y">The global Y-coordinate to test.</param>
    /// <param name="z">The global Z-coordinate to test.</param>
    /// <returns>Status indicating whether the <see cref="TileTick" /> is located at the specified global coordinates.</returns>
    public bool LocatedAt(int x, int y, int z)
    {
        return X == x && Y == y && Z == z;
    }

    /// <summary>
    ///     Moves the <see cref="TileTick" /> by given block offsets.
    /// </summary>
    /// <param name="diffX">The X-offset to move by, in blocks.</param>
    /// <param name="diffY">The Y-offset to move by, in blocks.</param>
    /// <param name="diffZ">The Z-offset to move by, in blocks.</param>
    public virtual void MoveBy(int diffX, int diffY, int diffZ)
    {
        X += diffX;
        Y += diffY;
        Z += diffZ;
    }

    /// <summary>
    ///     Attempt to construct a new <see cref="TileTick" /> from a Tile Entity subtree without validation.
    /// </summary>
    /// <param name="tree">The root node of a <see cref="TileTick" /> subtree.</param>
    /// <returns>A new <see cref="TileTick" /> on success, or null if the tree was unparsable.</returns>
    public static TileTick FromTree(TagNode tree)
    {
        return new TileTick().LoadTree(tree);
    }

    /// <summary>
    ///     Attempt to construct a new <see cref="TileTick" /> from a Tile Entity subtree with validation.
    /// </summary>
    /// <param name="tree">The root node of a <see cref="TileTick" /> subtree.</param>
    /// <returns>A new <see cref="TileTick" /> on success, or null if the tree failed validation.</returns>
    public static TileTick FromTreeSafe(TagNode tree)
    {
        return new TileTick().LoadTreeSafe(tree);
    }

    #region INbtObject<TileTick> Members

    /// <summary>
    ///     Gets a <see cref="SchemaNode" /> representing the basic schema of a <see cref="TileTick" />.
    /// </summary>
    public static SchemaNodeCompound Schema { get; } = new("")
    {
        new SchemaNodeScaler("i", TagType.TAG_INT),
        new SchemaNodeScaler("t", TagType.TAG_INT),
        new SchemaNodeScaler("x", TagType.TAG_INT),
        new SchemaNodeScaler("y", TagType.TAG_INT),
        new SchemaNodeScaler("z", TagType.TAG_INT)
    };

    /// <summary>
    ///     Attempt to load a <see cref="TileTick" /> subtree into the <see cref="TileTick" /> without validation.
    /// </summary>
    /// <param name="tree">The root node of a <see cref="TileTick" /> subtree.</param>
    /// <returns>The <see cref="TileTick" /> returns itself on success, or null if the tree was unparsable.</returns>
    public TileTick LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null) return null;

        ID = ctree["i"].ToTagInt();
        Ticks = ctree["t"].ToTagInt();
        X = ctree["x"].ToTagInt();
        Y = ctree["y"].ToTagInt();
        Z = ctree["z"].ToTagInt();

        _source = ctree.Copy() as TagNodeCompound;

        return this;
    }

    /// <summary>
    ///     Attempt to load a <see cref="TileTick" /> subtree into the <see cref="TileTick" /> with validation.
    /// </summary>
    /// <param name="tree">The root node of a <see cref="TileTick" /> subtree.</param>
    /// <returns>The <see cref="TileTick" /> returns itself on success, or null if the tree failed validation.</returns>
    public TileTick LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    /// <summary>
    ///     Builds a <see cref="TileTick" /> subtree from the current data.
    /// </summary>
    /// <returns>The root node of a <see cref="TileTick" /> subtree representing the current data.</returns>
    public TagNode BuildTree()
    {
        var tree = new TagNodeCompound();
        tree["i"] = new TagNodeInt(ID);
        tree["t"] = new TagNodeInt(Ticks);
        tree["x"] = new TagNodeInt(X);
        tree["y"] = new TagNodeInt(Y);
        tree["z"] = new TagNodeInt(Z);

        if (_source != null) tree.MergeFrom(_source);

        return tree;
    }

    /// <summary>
    ///     Validate a <see cref="TileTick" /> subtree against a basic schema.
    /// </summary>
    /// <param name="tree">The root node of a <see cref="TileTick" /> subtree.</param>
    /// <returns>Status indicating whether the tree was valid against the internal schema.</returns>
    public bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, Schema).Verify();
    }

    #endregion
}
