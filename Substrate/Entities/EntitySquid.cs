using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySquid : EntityMob
{
    public static readonly SchemaNodeCompound SquidSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySquid(string id)
        : base(id)
    {
    }

    public EntitySquid()
        : this(TypeId)
    {
    }

    public EntitySquid(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Squid";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SquidSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySquid(this);
    }

    #endregion
}