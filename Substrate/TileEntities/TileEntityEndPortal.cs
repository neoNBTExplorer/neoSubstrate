using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityEndPortal : TileEntity
{
    public static readonly SchemaNodeCompound EndPortalSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected TileEntityEndPortal(string id)
        : base(id)
    {
    }

    public TileEntityEndPortal()
        : this(TypeId)
    {
    }

    public TileEntityEndPortal(TileEntity te)
        : base(te)
    {
    }

    public static string TypeId => "Airportal";


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityEndPortal(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, EndPortalSchema).Verify();
    }

    #endregion
}