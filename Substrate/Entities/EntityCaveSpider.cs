using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityCaveSpider : EntitySpider
{
    public static readonly SchemaNodeCompound CaveSpiderSchema = SpiderSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntityCaveSpider(string id)
        : base(id)
    {
    }

    public EntityCaveSpider()
        : this(TypeId)
    {
    }

    public EntityCaveSpider(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "CaveSpider";

    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, CaveSpiderSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityCaveSpider(this);
    }

    #endregion
}