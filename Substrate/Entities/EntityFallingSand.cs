using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityFallingSand : TypedEntity
{
    public static readonly SchemaNodeCompound FallingSandSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Tile", TagType.TAG_BYTE)
    });

    private byte _tile;

    protected EntityFallingSand(string id)
        : base(id)
    {
    }

    public EntityFallingSand()
        : this(TypeId)
    {
    }

    public EntityFallingSand(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityFallingSand;
        if (e2 != null) _tile = e2._tile;
    }

    public static string TypeId => "FallingSand";

    public int Tile
    {
        get => _tile;
        set => _tile = (byte)value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityFallingSand(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _tile = ctree["Tile"].ToTagByte();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Tile"] = new TagNodeByte(_tile);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, FallingSandSchema).Verify();
    }

    #endregion
}
