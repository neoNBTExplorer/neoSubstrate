using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityMusic : TileEntity
{
    public static readonly SchemaNodeCompound MusicSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("note", TagType.TAG_BYTE)
    });

    private byte _note;

    protected TileEntityMusic(string id)
        : base(id)
    {
    }

    public TileEntityMusic()
        : this(TypeId)
    {
    }

    public TileEntityMusic(TileEntity te)
        : base(te)
    {
        var tes = te as TileEntityMusic;
        if (tes != null) _note = tes._note;
    }

    public static string TypeId => "Music";

    public int Note
    {
        get => _note;
        set => _note = (byte)value;
    }


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityMusic(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _note = ctree["note"].ToTagByte();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["note"] = new TagNodeByte(_note);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, MusicSchema).Verify();
    }

    #endregion
}
