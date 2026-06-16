using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityPainting : TypedEntity
{
    public enum DirectionType
    {
        EAST = 0,
        NORTH = 1,
        WEST = 2,
        SOUTH = 3
    }

    public static readonly SchemaNodeCompound PaintingSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Dir", TagType.TAG_BYTE),
        new SchemaNodeScaler("TileX", TagType.TAG_INT),
        new SchemaNodeScaler("TileY", TagType.TAG_INT),
        new SchemaNodeScaler("TileZ", TagType.TAG_INT),
        new SchemaNodeScaler("Motive", TagType.TAG_STRING)
    });

    protected EntityPainting(string id)
        : base(id)
    {
    }

    public EntityPainting()
        : this(TypeId)
    {
    }

    public EntityPainting(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityPainting;
        if (e2 != null)
        {
            TileX = e2.TileX;
            TileY = e2.TileY;
            TileZ = e2.TileZ;
            Direction = e2.Direction;
            Motive = e2.Motive;
        }
    }

    public static string TypeId => "Painting";

    public DirectionType Direction { get; set; }

    public string Motive { get; set; } = "";

    public int TileX { get; set; }

    public int TileY { get; set; }

    public int TileZ { get; set; }

    public override void MoveBy(int diffX, int diffY, int diffZ)
    {
        base.MoveBy(diffX, diffY, diffZ);

        TileX += diffX;
        TileY += diffY;
        TileZ += diffZ;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityPainting(this);
    }

    #endregion

    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        Direction = (DirectionType)ctree["Dir"].ToTagByte().Data;
        Motive = ctree["Motive"].ToTagString();
        TileX = ctree["TileX"].ToTagInt();
        TileY = ctree["TileY"].ToTagInt();
        TileZ = ctree["TileZ"].ToTagInt();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Dir"] = new TagNodeByte((byte)Direction);
        tree["Motive"] = new TagNodeString(Motive);
        tree["TileX"] = new TagNodeInt(TileX);
        tree["TileY"] = new TagNodeInt(TileY);
        tree["TileZ"] = new TagNodeInt(TileZ);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, PaintingSchema).Verify();
    }

    #endregion
}
