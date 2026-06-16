using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityGhast : EntityMob
{
    public static readonly SchemaNodeCompound GhastSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityGhast(string id)
        : base(id)
    {
    }

    public EntityGhast()
        : this(TypeId)
    {
    }

    public EntityGhast(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Ghast";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, GhastSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityGhast(this);
    }

    #endregion
}
