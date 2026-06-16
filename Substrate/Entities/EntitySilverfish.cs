using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySilverfish : EntityMob
{
    public static readonly SchemaNodeCompound SilverfishSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySilverfish(string id)
        : base(id)
    {
    }

    public EntitySilverfish()
        : this(TypeId)
    {
    }

    public EntitySilverfish(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Silverfish";


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySilverfish(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
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
        return new NbtVerifier(tree, SilverfishSchema).Verify();
    }

    #endregion
}
