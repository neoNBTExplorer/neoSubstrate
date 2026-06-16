using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityMagmaCube : EntitySlime
{
    public static readonly SchemaNodeCompound MagmaCubeSchema = SlimeSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityMagmaCube(string id)
        : base(id)
    {
    }

    public EntityMagmaCube()
        : this(TypeId)
    {
    }

    public EntityMagmaCube(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "LavaSlime";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, MagmaCubeSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityMagmaCube(this);
    }

    #endregion
}
