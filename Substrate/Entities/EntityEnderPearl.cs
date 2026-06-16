using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityEnderPearl : EntityThrowable
{
    public static readonly SchemaNodeCompound EnderPearlSchema = ThrowableSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityEnderPearl(string id)
        : base(id)
    {
    }

    public EntityEnderPearl()
        : this(TypeId)
    {
    }

    public EntityEnderPearl(TypedEntity e)
        : base(e)
    {
    }

    public static string TypeId => "ThrownEnderpearl";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, EnderPearlSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityEnderPearl(this);
    }

    #endregion
}