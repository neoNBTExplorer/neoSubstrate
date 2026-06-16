using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityRecordPlayer : TileEntity
{
    public static readonly SchemaNodeCompound RecordPlayerSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Record", TagType.TAG_INT, SchemaOptions.OPTIONAL)
    });

    protected TileEntityRecordPlayer(string id)
        : base(id)
    {
    }

    public TileEntityRecordPlayer()
        : this(TypeId)
    {
    }

    public TileEntityRecordPlayer(TileEntity te)
        : base(te)
    {
        var tes = te as TileEntityRecordPlayer;
        if (tes != null) Record = tes.Record;
    }

    public static string TypeId => "RecordPlayer";

    public int? Record { get; set; }


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityRecordPlayer(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        if (ctree.ContainsKey("Record")) Record = ctree["Record"].ToTagInt();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;

        if (Record != null) tree["Record"] = new TagNodeInt((int)Record);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, RecordPlayerSchema).Verify();
    }

    #endregion
}