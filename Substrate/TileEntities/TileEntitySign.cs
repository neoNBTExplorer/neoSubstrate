using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntitySign : TileEntity
{
    public static readonly SchemaNodeCompound SignSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Text1", TagType.TAG_STRING),
        new SchemaNodeScaler("Text2", TagType.TAG_STRING),
        new SchemaNodeScaler("Text3", TagType.TAG_STRING),
        new SchemaNodeScaler("Text4", TagType.TAG_STRING)
    });

    private string _text1 = "";
    private string _text2 = "";
    private string _text3 = "";
    private string _text4 = "";

    protected TileEntitySign(string id)
        : base(id)
    {
    }

    public TileEntitySign()
        : this(TypeId)
    {
    }

    public TileEntitySign(TileEntity te)
        : base(te)
    {
        var tes = te as TileEntitySign;
        if (tes != null)
        {
            _text1 = tes._text1;
            _text2 = tes._text2;
            _text3 = tes._text3;
            _text4 = tes._text4;
        }
    }

    public static string TypeId => "Sign";

    public string Text1
    {
        get => _text1;
        set => _text1 = value.Length > 14 ? value.Substring(0, 14) : value;
    }

    public string Text2
    {
        get => _text2;
        set => _text2 = value.Length > 14 ? value.Substring(0, 14) : value;
    }

    public string Text3
    {
        get => _text3;
        set => _text3 = value.Length > 14 ? value.Substring(0, 14) : value;
    }

    public string Text4
    {
        get => _text4;
        set => _text4 = value.Length > 14 ? value.Substring(0, 14) : value;
    }


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntitySign(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _text1 = ctree["Text1"].ToTagString();
        _text2 = ctree["Text2"].ToTagString();
        _text3 = ctree["Text3"].ToTagString();
        _text4 = ctree["Text4"].ToTagString();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Text1"] = new TagNodeString(_text1);
        tree["Text2"] = new TagNodeString(_text2);
        tree["Text3"] = new TagNodeString(_text3);
        tree["Text4"] = new TagNodeString(_text4);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SignSchema).Verify();
    }

    #endregion
}