using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySmallFireball : EntityFireball
{
    public static readonly SchemaNodeCompound SmallFireballSchema = FireballSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySmallFireball(string id)
        : base(id)
    {
    }

    public EntitySmallFireball()
        : this(TypeId)
    {
    }

    public EntitySmallFireball(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "SmallFireball";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SmallFireballSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySmallFireball(this);
    }

    #endregion
}
