using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityBoat : TypedEntity
{
    public static readonly SchemaNodeCompound BoatSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityBoat(string id)
        : base(id)
    {
    }

    public EntityBoat()
        : this(TypeId)
    {
    }

    public EntityBoat(TypedEntity e)
        : base(e)
    {
    }

    public static string TypeId => "Boat";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, BoatSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityBoat(this);
    }

    #endregion
}