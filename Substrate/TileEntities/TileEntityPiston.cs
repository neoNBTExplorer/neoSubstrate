using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityPiston : TileEntity
{
    public static readonly SchemaNodeCompound PistonSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("blockId", TagType.TAG_INT),
        new SchemaNodeScaler("blockData", TagType.TAG_INT),
        new SchemaNodeScaler("facing", TagType.TAG_INT),
        new SchemaNodeScaler("progress", TagType.TAG_FLOAT),
        new SchemaNodeScaler("extending", TagType.TAG_BYTE)
    });

    private readonly int? _record = null;

    private byte _extending;

    protected TileEntityPiston(string id)
        : base(id)
    {
    }

    public TileEntityPiston()
        : this(TypeId)
    {
    }

    public TileEntityPiston(TileEntity te)
        : base(te)
    {
        var tes = te as TileEntityPiston;
        if (tes != null)
        {
            BlockId = tes.BlockId;
            BlockData = tes.BlockData;
            Facing = tes.Facing;
            Progress = tes.Progress;
            _extending = tes._extending;
        }
    }

    public static string TypeId => "Piston";

    public bool Extending
    {
        get => _extending != 0;
        set => _extending = (byte)(value ? 1 : 0);
    }

    public int BlockId { get; set; }

    public int BlockData { get; set; }

    public int Facing { get; set; }

    public float Progress { get; set; }


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityPiston(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        BlockId = ctree["blockId"].ToTagInt();
        BlockData = ctree["blockData"].ToTagInt();
        Facing = ctree["facing"].ToTagInt();
        Progress = ctree["progress"].ToTagFloat();
        _extending = ctree["extending"].ToTagByte();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;

        if (_record != null)
        {
            tree["blockId"] = new TagNodeInt(BlockId);
            tree["blockData"] = new TagNodeInt(BlockData);
            tree["facing"] = new TagNodeInt(Facing);
            tree["progress"] = new TagNodeFloat(Progress);
            tree["extending"] = new TagNodeByte(_extending);
        }

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, PistonSchema).Verify();
    }

    #endregion
}
