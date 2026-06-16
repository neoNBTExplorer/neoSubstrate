using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityPrimedTnt : TypedEntity
{
    public static readonly SchemaNodeCompound PrimedTntSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Fuse", TagType.TAG_BYTE)
    });

    private byte _fuse;

    protected EntityPrimedTnt(string id)
        : base(id)
    {
    }

    public EntityPrimedTnt()
        : this(TypeId)
    {
    }

    public EntityPrimedTnt(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityPrimedTnt;
        if (e2 != null) _fuse = e2._fuse;
    }

    public static string TypeId => "PrimedTnt";

    public int Fuse
    {
        get => _fuse;
        set => _fuse = (byte)value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityPrimedTnt(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _fuse = ctree["Fuse"].ToTagByte();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Fuse"] = new TagNodeByte(_fuse);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, PrimedTntSchema).Verify();
    }

    #endregion
}
