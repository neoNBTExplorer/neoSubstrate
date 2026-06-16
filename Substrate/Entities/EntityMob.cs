using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.Entities;

/// <summary>
///     Encompasses data in the "ActiveEffects" compound attribute of mob entity types, used to specify potion effects
/// </summary>
public class ActiveEffects : ICopyable<ActiveEffects>
{
    private byte _amplifier;
    private byte _id;

    /// <summary>
    ///     Gets or sets the ID of the potion effect type.
    /// </summary>
    public int Id
    {
        get => _id;
        set => _id = (byte)value;
    }

    /// <summary>
    ///     Gets or sets the amplification of the potion effect.
    /// </summary>
    public int Amplifier
    {
        get => _amplifier;
        set => _amplifier = (byte)value;
    }

    /// <summary>
    ///     Gets or sets the remaining duration of the potion effect.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    ///     Determine if the combination of properties in this ActiveEffects is valid.
    /// </summary>
    public bool IsValid => !(_id == 0 || _amplifier == 0 || Duration == 0);

    #region ICopyable<ActiveEffects> Members

    public ActiveEffects Copy()
    {
        var ae = new ActiveEffects();
        ae._amplifier = _amplifier;
        ae.Duration = Duration;
        ae._id = _id;

        return ae;
    }

    #endregion
}

public class EntityMob : TypedEntity
{
    public static readonly SchemaNodeCompound MobSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("AttackTime", TagType.TAG_SHORT),
        new SchemaNodeScaler("DeathTime", TagType.TAG_SHORT),
        new SchemaNodeScaler("Health", TagType.TAG_SHORT),
        new SchemaNodeScaler("HurtTime", TagType.TAG_SHORT),
        new SchemaNodeCompound("ActiveEffects", SchemaOptions.OPTIONAL)
        {
            new SchemaNodeScaler("Id", TagType.TAG_BYTE),
            new SchemaNodeScaler("Amplifier", TagType.TAG_BYTE),
            new SchemaNodeScaler("Duration", TagType.TAG_INT)
        }
    });

    private short _attackTime;
    private short _deathTime;
    private short _health;
    private short _hurtTime;

    protected EntityMob(string id)
        : base(id)
    {
        ActiveEffects = new ActiveEffects();
    }

    public EntityMob()
        : this(TypeId)
    {
    }

    public EntityMob(TypedEntity e)
        : base(e)
    {
        var e2 = e as EntityMob;
        if (e2 != null)
        {
            _attackTime = e2._attackTime;
            _deathTime = e2._deathTime;
            _health = e2._health;
            _hurtTime = e2._hurtTime;
            ActiveEffects = e2.ActiveEffects.Copy();
        }
        else
        {
            ActiveEffects = new ActiveEffects();
        }
    }

    public static string TypeId => "Mob";

    public int AttackTime
    {
        get => _attackTime;
        set => _attackTime = (short)value;
    }

    public int DeathTime
    {
        get => _deathTime;
        set => _deathTime = (short)value;
    }

    public int Health
    {
        get => _health;
        set => _health = (short)value;
    }

    public int HurtTime
    {
        get => _hurtTime;
        set => _hurtTime = (short)value;
    }

    public ActiveEffects ActiveEffects { get; set; }


    #region ICopyable<Entity> Members

    public override TypedEntity Copy()
    {
        return new EntityMob(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    public override TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _attackTime = ctree["AttackTime"].ToTagShort();
        _deathTime = ctree["DeathTime"].ToTagShort();
        _health = ctree["Health"].ToTagShort();
        _hurtTime = ctree["HurtTime"].ToTagShort();

        if (ctree.ContainsKey("ActiveEffects"))
        {
            var ae = ctree["ActiveEffects"].ToTagCompound();

            ActiveEffects = new ActiveEffects();
            ActiveEffects.Id = ae["Id"].ToTagByte();
            ActiveEffects.Amplifier = ae["Amplifier"].ToTagByte();
            ActiveEffects.Duration = ae["Duration"].ToTagInt();
        }

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["AttackTime"] = new TagNodeShort(_attackTime);
        tree["DeathTime"] = new TagNodeShort(_deathTime);
        tree["Health"] = new TagNodeShort(_health);
        tree["HurtTime"] = new TagNodeShort(_hurtTime);

        if (ActiveEffects != null && ActiveEffects.IsValid)
        {
            var ae = new TagNodeCompound();
            ae["Id"] = new TagNodeByte((byte)ActiveEffects.Id);
            ae["Amplifier"] = new TagNodeByte((byte)ActiveEffects.Amplifier);
            ae["Duration"] = new TagNodeInt(ActiveEffects.Duration);

            tree["ActiveEffects"] = ae;
        }

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, MobSchema).Verify();
    }

    #endregion
}