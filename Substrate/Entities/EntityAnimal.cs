using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityAnimal : EntityMob
{
    public static readonly SchemaNodeCompound AnimalSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeScaler("Age", TagType.TAG_INT, SchemaOptions.CREATE_ON_MISSING),
        new SchemaNodeScaler("InLove", TagType.TAG_INT, SchemaOptions.CREATE_ON_MISSING)
    });

    protected EntityAnimal(string id)
        : base(id)
    {
    }

    public EntityAnimal()
        : this(TypeId)
    {
    }

    public EntityAnimal(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityAnimal;
        if (e2 != null)
        {
            Age = e2.Age;
            InLove = e2.InLove;
        }
    }

    public int Age { get; set; }

    public int InLove { get; set; }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityAnimal(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        Age = ctree["Age"].ToTagInt();
        InLove = ctree["InLove"].ToTagInt();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Age"] = new TagNodeInt(Age);
        tree["InLove"] = new TagNodeInt(InLove);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, AnimalSchema).Verify();
    }

    #endregion
}
