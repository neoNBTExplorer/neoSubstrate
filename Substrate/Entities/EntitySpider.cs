using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySpider : EntityMob
{
    public static readonly SchemaNodeCompound SpiderSchema = MobSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId)
    });

    protected EntitySpider(string id)
        : base(id)
    {
    }

    public EntitySpider()
        : this(TypeId)
    {
    }

    public EntitySpider(TypedEntity e)
        : base(e)
    {
    }

    public new static string TypeId => "Spider";


    #region INBTObject<Entity> Members

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SpiderSchema).Verify();
    }

    #endregion


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySpider(this);
    }

    #endregion
}