using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityEnderDragon : EntityMob
{
    public static readonly SchemaNodeCompound EnderDragonSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityEnderDragon(string id)
        : base(id)
    {
    }

    public EntityEnderDragon()
        : this(TypeId)
    {
    }

    public EntityEnderDragon(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "EnderDragon";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, EnderDragonSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityEnderDragon(this);
    }

    #endregion
}
