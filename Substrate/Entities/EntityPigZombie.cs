using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityPigZombie : EntityMob
{
    public static readonly SchemaNodeCompound PigZombieSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Anger", TagType.TAG_SHORT)
    });

    private short _anger;

    protected EntityPigZombie(string id)
        : base(id)
    {
    }

    public EntityPigZombie()
        : this(TypeId)
    {
    }

    public EntityPigZombie(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityPigZombie;
        if (e2 != null) _anger = e2._anger;
    }

    public new static string TypeId => "PigZombie";

    public int Anger
    {
        get => _anger;
        set => _anger = (short)value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityPigZombie(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _anger = ctree["Anger"].ToTagShort();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Anger"] = new TagNodeShort(_anger);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, PigZombieSchema).Verify();
    }

    #endregion
}