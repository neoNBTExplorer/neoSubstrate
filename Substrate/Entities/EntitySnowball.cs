using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySnowball : EntityThrowable
{
    public static readonly SchemaNodeCompound SnowballSchema = ThrowableSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySnowball(string id)
        : base(id)
    {
    }

    public EntitySnowball()
        : this(TypeId)
    {
    }

    public EntitySnowball(TypedEntity e)
        : base(e)
    {
    }

    public static string TypeId => "Snowball";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SnowballSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySnowball(this);
    }

    #endregion
}
