using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityItem : TypedEntity
{
    public static readonly SchemaNodeCompound ItemSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Health", TagType.TAG_SHORT),
        new SchemaNodeScaler("Age", TagType.TAG_SHORT),
        new SchemaNodeCompound("Item", Item.Schema)
    });

    private short _age;

    private short _health;

    protected EntityItem(string id)
        : base(id)
    {
    }

    public EntityItem()
        : this(TypeId)
    {
    }

    public EntityItem(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityItem;
        if (e2 != null)
        {
            _health = e2._health;
            _age = e2._age;
            Item = e2.Item.Copy();
        }
    }

    public static string TypeId => "Item";

    public int Health
    {
        get => _health;
        set => _health = (short)value;
    }

    public int Age
    {
        get => _age;
        set => _age = (short)value;
    }

    public Item Item { get; set; }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityItem(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _health = ctree["Health"].ToTagShort();
        _age = ctree["Age"].ToTagShort();

        Item = new Item().LoadTree(ctree["Item"]);

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Health"] = new TagNodeShort(_health);
        tree["Age"] = new TagNodeShort(_age);
        tree["Item"] = Item.BuildTree();

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, ItemSchema).Verify();
    }

    #endregion
}
