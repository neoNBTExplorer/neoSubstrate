using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityTrap : TileEntity, IItemContainer
{
    private const int _CAPACITY = 8;

    public static readonly SchemaNodeCompound TrapSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeList("Items", TagType.TAG_COMPOUND, ItemCollection.Schema)
    });

    protected TileEntityTrap(string id)
        : base(id)
    {
        Items = new ItemCollection(_CAPACITY);
    }

    public TileEntityTrap()
        : this(TypeId)
    {
    }

    public TileEntityTrap(TileEntity te)
        : base(te)
    {
        var tec = te as TileEntityTrap;
        if (tec != null)
            Items = tec.Items.Copy();
        else
            Items = new ItemCollection(_CAPACITY);
    }

    public static string TypeId => "Trap";


    #region IItemContainer Members

    public ItemCollection Items { get; private set; }

    #endregion


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityTrap(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        var items = ctree["Items"].ToTagList();
        Items = new ItemCollection(_CAPACITY).LoadTree(items);

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Items"] = Items.BuildTree();

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, TrapSchema).Verify();
    }

    #endregion
}