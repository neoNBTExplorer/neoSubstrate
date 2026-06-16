using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySnowman : EntityMob
{
    public static readonly SchemaNodeCompound SnowmanSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySnowman(string id)
        : base(id)
    {
    }

    public EntitySnowman()
        : this(TypeId)
    {
    }

    public EntitySnowman(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "SnowMan";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SnowmanSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySnowman(this);
    }

    #endregion
}
