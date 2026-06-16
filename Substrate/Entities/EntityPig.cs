using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityPig : EntityAnimal
{
    public static readonly SchemaNodeCompound PigSchema = AnimalSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Saddle", TagType.TAG_BYTE)
    });

    protected EntityPig(string id)
        : base(id)
    {
    }

    public EntityPig()
        : this(TypeId)
    {
    }

    public EntityPig(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityPig;
        if (e2 != null) HasSaddle = e2.HasSaddle;
    }

    public new static string TypeId => "Pig";

    public bool HasSaddle { get; set; }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityPig(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        HasSaddle = ctree["Saddle"].ToTagByte() == 1;

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Saddle"] = new TagNodeByte((byte)(HasSaddle ? 1 : 0));

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, PigSchema).Verify();
    }

    #endregion
}