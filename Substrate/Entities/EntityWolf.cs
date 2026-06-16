using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityWolf : EntityAnimal
{
    public static readonly SchemaNodeCompound WolfSchema = AnimalSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Owner", TagType.TAG_STRING),
        new SchemaNodeScaler("Sitting", TagType.TAG_BYTE),
        new SchemaNodeScaler("Angry", TagType.TAG_BYTE)
    });

    protected EntityWolf(string id)
        : base(id)
    {
    }

    public EntityWolf()
        : this(TypeId)
    {
    }

    public EntityWolf(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityWolf;
        if (e2 != null)
        {
            Owner = e2.Owner;
            IsSitting = e2.IsSitting;
            IsAngry = e2.IsAngry;
        }
    }

    public new static string TypeId => "Wolf";

    public string Owner { get; set; }

    public bool IsSitting { get; set; }

    public bool IsAngry { get; set; }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityWolf(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        Owner = ctree["Owner"].ToTagString();
        IsSitting = ctree["Sitting"].ToTagByte() == 1;
        IsAngry = ctree["Angry"].ToTagByte() == 1;

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Owner"] = new TagNodeString(Owner);
        tree["Sitting"] = new TagNodeByte((byte)(IsSitting ? 1 : 0));
        tree["Angry"] = new TagNodeByte((byte)(IsAngry ? 1 : 0));

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, WolfSchema).Verify();
    }

    #endregion
}