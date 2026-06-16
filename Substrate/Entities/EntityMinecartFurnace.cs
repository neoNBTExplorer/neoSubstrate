using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityMinecartFurnace : EntityMinecart
{
    public static readonly SchemaNodeCompound MinecartFurnaceSchema = MinecartSchema.MergeInto(
        new SchemaNodeCompound("")
        {
            new SchemaNodeScaler("PushX", TagType.TAG_DOUBLE),
            new SchemaNodeScaler("PushZ", TagType.TAG_DOUBLE),
            new SchemaNodeScaler("Fuel", TagType.TAG_SHORT)
        });

    private short _fuel;

    protected EntityMinecartFurnace(string id)
        : base(id)
    {
    }

    public EntityMinecartFurnace()
    {
    }

    public EntityMinecartFurnace(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityMinecartFurnace;
        if (e2 != null)
        {
            PushX = e2.PushX;
            PushZ = e2.PushZ;
            _fuel = e2._fuel;
        }
    }

    public new static string TypeId => EntityMinecart.TypeId;

    public double PushX { get; set; }

    public double PushZ { get; set; }

    public int Fuel
    {
        get => _fuel;
        set => _fuel = (short)value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityMinecartFurnace(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        PushX = ctree["PushX"].ToTagDouble();
        PushZ = ctree["PushZ"].ToTagDouble();
        _fuel = ctree["Fuel"].ToTagShort();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["PushX"] = new TagNodeDouble(PushX);
        tree["PushZ"] = new TagNodeDouble(PushZ);
        tree["Fuel"] = new TagNodeShort(_fuel);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, MinecartFurnaceSchema).Verify();
    }

    #endregion
}