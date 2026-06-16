using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

/// <summary>
///     Represents an enchantment that can be applied to some <see cref="Item" />s.
/// </summary>
public class Enchantment : INbtObject<Enchantment>, ICopyable<Enchantment>
{
    private short _id;
    private short _level;

    private TagNodeCompound _source;

    /// <summary>
    ///     Constructs a blank <see cref="Enchantment" />.
    /// </summary>
    public Enchantment()
    {
    }

    /// <summary>
    ///     Constructs an <see cref="Enchantment" /> from a given id and level.
    /// </summary>
    /// <param name="id">The id (type) of the enchantment.</param>
    /// <param name="level">The level of the enchantment.</param>
    public Enchantment(int id, int level)
    {
        _id = (short)id;
        _level = (short)level;
    }

    #region ICopyable<Enchantment> Members

    /// <inheritdoc />
    public Enchantment Copy()
    {
        var ench = new Enchantment(_id, _level);

        if (_source != null) ench._source = _source.Copy() as TagNodeCompound;

        return ench;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets an <see cref="EnchantmentInfo" /> entry for this enchantment's type.
    /// </summary>
    public EnchantmentInfo Info => EnchantmentInfo.EnchantmentTable[_id];

    /// <summary>
    ///     Gets or sets the current type (id) of the enchantment.
    /// </summary>
    public int Id
    {
        get => _id;
        set => _id = (short)value;
    }

    /// <summary>
    ///     Gets or sets the level of the enchantment.
    /// </summary>
    public int Level
    {
        get => _level;
        set => _level = (short)value;
    }

    /// <summary>
    ///     Gets a <see cref="SchemaNode" /> representing the schema of an enchantment.
    /// </summary>
    public static SchemaNodeCompound Schema { get; } = new("")
    {
        new SchemaNodeScaler("id", TagType.TAG_SHORT),
        new SchemaNodeScaler("lvl", TagType.TAG_SHORT)
    };

    #endregion

    #region INbtObject<Enchantment> Members

    /// <inheritdoc />
    public Enchantment LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null) return null;

        _id = ctree["id"].ToTagShort();
        _level = ctree["lvl"].ToTagShort();

        _source = ctree.Copy() as TagNodeCompound;

        return this;
    }

    /// <inheritdoc />
    public Enchantment LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    /// <inheritdoc />
    public TagNode BuildTree()
    {
        var tree = new TagNodeCompound();
        tree["id"] = new TagNodeShort(_id);
        tree["lvl"] = new TagNodeShort(_level);

        if (_source != null) tree.MergeFrom(_source);

        return tree;
    }

    /// <inheritdoc />
    public bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, Schema).Verify();
    }

    #endregion
}
