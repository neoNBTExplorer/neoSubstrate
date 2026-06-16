using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityZombie : EntityMob
{
    public static readonly SchemaNodeCompound ZombieSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityZombie(string id)
        : base(id)
    {
    }

    public EntityZombie()
        : this(TypeId)
    {
    }

    public EntityZombie(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Zombie";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, ZombieSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityZombie(this);
    }

    #endregion
}