using Substrate.Nbt;

namespace Substrate.Entities;

public class EntitySheep : EntityAnimal
{
    public static readonly SchemaNodeCompound SheepSchema = AnimalSchema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Sheared", TagType.TAG_BYTE),
        new SchemaNodeScaler("Color", TagType.TAG_BYTE, SchemaOptions.CREATE_ON_MISSING)
    });

    private byte _color;

    protected EntitySheep(string id)
        : base(id)
    {
    }

    public EntitySheep()
        : this(TypeId)
    {
    }

    public EntitySheep(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntitySheep;
        if (e2 != null)
        {
            IsSheared = e2.IsSheared;
            _color = e2._color;
        }
    }

    public new static string TypeId => "Sheep";

    public bool IsSheared { get; set; }

    public int Color
    {
        get => _color;
        set => _color = (byte)value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntitySheep(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        IsSheared = ctree["Sheared"].ToTagByte() == 1;
        _color = ctree["Color"].ToTagByte();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Sheared"] = new TagNodeByte((byte)(IsSheared ? 1 : 0));
        tree["Color"] = new TagNodeByte(_color);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, SheepSchema).Verify();
    }

    #endregion
}