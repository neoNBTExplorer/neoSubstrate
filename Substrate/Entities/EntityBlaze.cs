using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityBlaze : EntityMob
{
    public static readonly SchemaNodeCompound BlazeSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityBlaze(string id)
        : base(id)
    {
    }

    public EntityBlaze()
        : this(TypeId)
    {
    }

    public EntityBlaze(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Blaze";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, BlazeSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityBlaze(this);
    }

    #endregion
}
