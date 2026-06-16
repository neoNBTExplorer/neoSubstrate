using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityBrewingStand : TileEntity, IItemContainer
{
    private const int _CAPACITY = 4;

    public static readonly SchemaNodeCompound BrewingStandSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeList("Items", TagType.TAG_COMPOUND, ItemCollection.Schema),
        new SchemaNodeScaler("BrewTime", TagType.TAG_SHORT)
    });

    private short _brewTime;

    protected TileEntityBrewingStand(string id)
        : base(id)
    {
        Items = new ItemCollection(_CAPACITY);
    }

    public TileEntityBrewingStand()
        : this(TypeId)
    {
    }

    public TileEntityBrewingStand(TileEntity te)
        : base(te)
    {
        var tec = te as TileEntityBrewingStand;
        if (tec != null)
        {
            Items = tec.Items.Copy();
            _brewTime = tec._brewTime;
        }
        else
        {
            Items = new ItemCollection(_CAPACITY);
        }
    }

    public static string TypeId => "Cauldron";

    public int BrewTime
    {
        get => _brewTime;
        set => _brewTime = (short)value;
    }


    #region IItemContainer Members

    public ItemCollection Items { get; private set; }

    #endregion

    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityBrewingStand(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        var items = ctree["Items"].ToTagList();
        Items = new ItemCollection(_CAPACITY).LoadTree(items);

        _brewTime = ctree["BrewTime"].ToTagShort();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Items"] = Items.BuildTree();
        tree["BrewTime"] = new TagNodeShort(_brewTime);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, BrewingStandSchema).Verify();
    }

    #endregion
}