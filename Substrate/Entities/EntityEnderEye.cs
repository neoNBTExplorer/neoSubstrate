using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityEnderEye : TypedEntity
{
    public static readonly SchemaNodeCompound EnderEyeSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityEnderEye(string id)
        : base(id)
    {
    }

    public EntityEnderEye()
        : this(TypeId)
    {
    }

    public EntityEnderEye(TypedEntity e)
        : base(e)
    {
    }

    public static string TypeId => "EyeOfEnderSignal";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, EnderEyeSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityEnderEye(this);
    }

    #endregion
}