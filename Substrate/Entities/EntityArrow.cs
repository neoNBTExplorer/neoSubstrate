using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityArrow : EntityThrowable
{
    public static readonly SchemaNodeCompound ArrowSchema = ThrowableSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("inData", TagType.TAG_BYTE, SchemaOptions.CREATE_ON_MISSING),
        new SchemaNodeScaler("player", TagType.TAG_BYTE, SchemaOptions.CREATE_ON_MISSING)
    });

    private byte _inData;
    private byte _player;

    protected EntityArrow(string id)
        : base(id)
    {
    }

    public EntityArrow()
        : this(TypeId)
    {
    }

    public EntityArrow(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityArrow;
        if (e2 != null)
        {
            _inData = e2._inData;
            _player = e2._player;
        }
    }

    public static string TypeId => "Arrow";

    public int InData
    {
        get => _inData;
        set => _inData = (byte)value;
    }

    public bool IsPlayerArrow
    {
        get => _player != 0;
        set => _player = (byte)(value ? 1 : 0);
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityArrow(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _inData = ctree["inData"].ToTagByte();
        _player = ctree["player"].ToTagByte();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["inData"] = new TagNodeShort(_inData);
        tree["player"] = new TagNodeShort(_player);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, ArrowSchema).Verify();
    }

    #endregion
}
