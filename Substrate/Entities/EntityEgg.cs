using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityEgg : EntityThrowable
{
    public static readonly SchemaNodeCompound EggSchema = ThrowableSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityEgg(string id)
        : base(id)
    {
    }

    public EntityEgg()
        : this(TypeId)
    {
    }

    public EntityEgg(TypedEntity e)
        : base(e)
    {
    }

    public static string TypeId => "Egg";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, EggSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityEgg(this);
    }

    #endregion
}
