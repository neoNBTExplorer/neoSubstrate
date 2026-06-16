using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySkeleton : EntityMob
{
    public static readonly SchemaNodeCompound SkeletonSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySkeleton(string id)
        : base(id)
    {
    }

    public EntitySkeleton()
        : this(TypeId)
    {
    }

    public EntitySkeleton(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Skeleton";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SkeletonSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySkeleton(this);
    }

    #endregion
}
