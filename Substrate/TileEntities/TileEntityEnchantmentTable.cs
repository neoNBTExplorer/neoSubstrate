using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityEnchantmentTable : TileEntity
{
    public static readonly SchemaNodeCompound EnchantTableSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected TileEntityEnchantmentTable(string id)
        : base(id)
    {
    }

    public TileEntityEnchantmentTable()
        : this(TypeId)
    {
    }

    public TileEntityEnchantmentTable(TileEntity te)
        : base(te)
    {
    }

    public static string TypeId => "EnchantTable";


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityEnchantmentTable(this);
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
        return new NbtVerifier(tree, EnchantTableSchema).Verify();
    }

    #endregion
}
