using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityMooshroom : EntityCow
{
    public static readonly SchemaNodeCompound MooshroomSchema = CowSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityMooshroom(string id)
        : base(id)
    {
    }

    public EntityMooshroom()
        : this(TypeId)
    {
    }

    public EntityMooshroom(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "MushroomCow";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, MooshroomSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityMooshroom(this);
    }

    #endregion
}
