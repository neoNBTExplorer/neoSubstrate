using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityCow : EntityMob
{
    public static readonly SchemaNodeCompound CowSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityCow(string id)
        : base(id)
    {
    }

    public EntityCow()
        : this(TypeId)
    {
    }

    public EntityCow(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Cow";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, CowSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityCow(this);
    }

    #endregion
}