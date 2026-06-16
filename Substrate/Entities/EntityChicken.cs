using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityChicken : EntityAnimal
{
    public static readonly SchemaNodeCompound ChickenSchema = AnimalSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityChicken(string id)
        : base(id)
    {
    }

    public EntityChicken()
        : this(TypeId)
    {
    }

    public EntityChicken(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Chicken";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, ChickenSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityChicken(this);
    }

    #endregion
}
