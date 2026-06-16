using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityGiant : EntityMob
{
    public static readonly SchemaNodeCompound GiantSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityGiant(string id)
        : base(id)
    {
    }

    public EntityGiant()
        : this(TypeId)
    {
    }

    public EntityGiant(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Giant";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, GiantSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityGiant(this);
    }

    #endregion
}
