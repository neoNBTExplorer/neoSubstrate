using Substrate.Nbt;

namespace Substrate.Entities;

public class EntityXPOrb : TypedEntity
{
    public static readonly SchemaNodeCompound XPOrbSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("Health", TagType.TAG_SHORT),
        new SchemaNodeScaler("Age", TagType.TAG_SHORT),
        new SchemaNodeScaler("Value", TagType.TAG_SHORT)
    });

    private short _age;

    private short _health;
    private short _value;

    protected EntityXPOrb(string id)
        : base(id)
    {
    }

    public EntityXPOrb()
        : this(TypeId)
    {
    }

    public EntityXPOrb(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityXPOrb;
        if (e2 != null)
        {
            _health = e2._health;
            _age = e2._age;
            _value = e2._value;
        }
    }

    public static string TypeId => "XPOrb";

    public int Health
    {
        get => _health;
        set => _health = (short)(value & 0xFF);
    }

    public int Age
    {
        get => _age;
        set => _age = (short)value;
    }

    public int Value
    {
        get => _value;
        set => _value = (short)value;
    }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityXPOrb(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _health = ctree["Health"].ToTagShort();
        _age = ctree["Age"].ToTagShort();
        _value = ctree["Value"].ToTagShort();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["Health"] = new TagNodeShort(_health);
        tree["Age"] = new TagNodeShort(_age);
        tree["Value"] = new TagNodeShort(_value);

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, XPOrbSchema).Verify();
    }

    #endregion
}
