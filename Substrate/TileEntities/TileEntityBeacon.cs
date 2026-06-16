using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityBeacon : TileEntity
{
    public static readonly SchemaNodeCompound BeaconSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Levels", TagType.TAG_INT),
        new SchemaNodeScaler("Primary", TagType.TAG_INT),
        new SchemaNodeScaler("Secondary", TagType.TAG_INT)
    });

    protected TileEntityBeacon(string id)
        : base(id)
    {
    }

    public TileEntityBeacon()
        : this(TypeId)
    {
    }

    public TileEntityBeacon(TileEntity te)
        : base(te)
    {
        var tes = te as TileEntityBeacon;
        if (tes != null)
        {
            Levels = tes.Levels;
            Primary = tes.Primary;
            Secondary = tes.Secondary;
        }
    }

    public static string TypeId => "Beacon";

    public int Levels { get; set; }

    public int Primary { get; set; }

    public int Secondary { get; set; }


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityBeacon(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        Levels = ctree["Levels"].ToTagInt();
        Primary = ctree["Primary"].ToTagInt();
        Secondary = ctree["Secondary"].ToTagInt();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Levels"] = new TagNodeInt(Levels);
        tree["Primary"] = new TagNodeInt(Primary);
        tree["Secondary"] = new TagNodeInt(Secondary);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, BeaconSchema).Verify();
    }

    #endregion
}