using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityCreeper : EntityMob
{
    public static readonly SchemaNodeCompound CreeperSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("powered", TagType.TAG_BYTE, SchemaOptions.OPTIONAL)
    });

    private bool? _powered;

    protected EntityCreeper(string id)
        : base(id)
    {
    }

    public EntityCreeper()
        : this(TypeId)
    {
    }

    public EntityCreeper(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityCreeper;
        if (e2 != null) _powered = e2._powered;
    }

    public new static string TypeId => "Creeper";

    public bool Powered
    {
        get => _powered ?? false;
        set => _powered = value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityCreeper(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        if (ctree.ContainsKey("powered")) _powered = ctree["powered"].ToTagByte() == 1;

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;

        if (_powered != null) tree["powered"] = new TagNodeByte((byte)(_powered ?? false ? 1 : 0));

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, CreeperSchema).Verify();
    }

    #endregion
}