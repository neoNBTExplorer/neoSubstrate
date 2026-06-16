using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

/// <summary>
///     The base Entity type for Minecraft Entities, providing access to data common to all Minecraft Entities.
/// </summary>
public class Entity : INbtObject<Entity>, ICopyable<Entity>
{
    private short _air;

    private float _fallDistance;
    private short _fire;
    private byte _onGround;

    /// <summary>
    ///     Constructs a new generic <see cref="Entity" /> with default values.
    /// </summary>
    public Entity()
    {
        Position = new Vector3();
        Motion = new Vector3();
        Rotation = new Orientation();

        Source = new TagNodeCompound();
    }

    /// <summary>
    ///     Constructs a new generic <see cref="Entity" /> by copying fields from another <see cref="Entity" /> object.
    /// </summary>
    /// <param name="e">An <see cref="Entity" /> to copy fields from.</param>
    protected Entity(Entity e)
    {
        Position = new Vector3();
        Position.X = e.Position.X;
        Position.Y = e.Position.Y;
        Position.Z = e.Position.Z;

        Motion = new Vector3();
        Motion.X = e.Motion.X;
        Motion.Y = e.Motion.Y;
        Motion.Z = e.Motion.Z;

        Rotation = new Orientation();
        Rotation.Pitch = e.Rotation.Pitch;
        Rotation.Yaw = e.Rotation.Yaw;

        _fallDistance = e._fallDistance;
        _fire = e._fire;
        _air = e._air;
        _onGround = e._onGround;

        if (e.Source != null) Source = e.Source.Copy() as TagNodeCompound;
    }

    /// <summary>
    ///     Gets or sets the global position of the entity in fractional block coordinates.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    ///     Gets or sets the velocity of the entity.
    /// </summary>
    public Vector3 Motion { get; set; }

    /// <summary>
    ///     Gets or sets the orientation of the entity.
    /// </summary>
    public Orientation Rotation { get; set; }

    /// <summary>
    ///     Gets or sets the distance that the entity has fallen, if it is falling.
    /// </summary>
    public double FallDistance
    {
        get => _fallDistance;
        set => _fallDistance = (float)value;
    }

    /// <summary>
    ///     Gets or sets the fire counter of the entity.
    /// </summary>
    public int Fire
    {
        get => _fire;
        set => _fire = (short)value;
    }

    /// <summary>
    ///     Gets or sets the remaining air availale to the entity.
    /// </summary>
    public int Air
    {
        get => _air;
        set => _air = (short)value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the entity is currently touch the ground.
    /// </summary>
    public bool IsOnGround
    {
        get => _onGround == 1;
        set => _onGround = (byte)(value ? 1 : 0);
    }

    /// <summary>
    ///     Gets the source <see cref="TagNodeCompound" /> used to create this <see cref="Entity" /> if it exists.
    /// </summary>
    public TagNodeCompound Source { get; private set; }


    #region ICopyable<Entity> Members

    /// <summary>
    ///     Creates a deep-copy of the <see cref="Entity" />.
    /// </summary>
    /// <returns>A deep-copy of the <see cref="Entity" />.</returns>
    public Entity Copy()
    {
        return new Entity(this);
    }

    #endregion

    /// <summary>
    ///     Moves the <see cref="Entity" /> by given block offsets.
    /// </summary>
    /// <param name="diffX">The X-offset to move by, in blocks.</param>
    /// <param name="diffY">The Y-offset to move by, in blocks.</param>
    /// <param name="diffZ">The Z-offset to move by, in blocks.</param>
    public virtual void MoveBy(int diffX, int diffY, int diffZ)
    {
        Position.X += diffX;
        Position.Y += diffY;
        Position.Z += diffZ;
    }


    #region INBTObject<Entity> Members

    /// <summary>
    ///     Gets a <see cref="SchemaNode" /> representing the basic schema of an Entity.
    /// </summary>
    public static SchemaNodeCompound Schema { get; } = new("")
    {
        new SchemaNodeList("Pos", TagType.TAG_DOUBLE, 3),
        new SchemaNodeList("Motion", TagType.TAG_DOUBLE, 3),
        new SchemaNodeList("Rotation", TagType.TAG_FLOAT, 2),
        new SchemaNodeScaler("FallDistance", TagType.TAG_FLOAT),
        new SchemaNodeScaler("Fire", TagType.TAG_SHORT),
        new SchemaNodeScaler("Air", TagType.TAG_SHORT),
        new SchemaNodeScaler("OnGround", TagType.TAG_BYTE)
    };

    /// <summary>
    ///     Attempt to load an Entity subtree into the <see cref="Entity" /> without validation.
    /// </summary>
    /// <param name="tree">The root node of an Entity subtree.</param>
    /// <returns>The <see cref="Entity" /> returns itself on success, or null if the tree was unparsable.</returns>
    public Entity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null) return null;

        var pos = ctree["Pos"].ToTagList();
        Position = new Vector3();
        Position.X = pos[0].ToTagDouble();
        Position.Y = pos[1].ToTagDouble();
        Position.Z = pos[2].ToTagDouble();

        var motion = ctree["Motion"].ToTagList();
        Motion = new Vector3();
        Motion.X = motion[0].ToTagDouble();
        Motion.Y = motion[1].ToTagDouble();
        Motion.Z = motion[2].ToTagDouble();

        var rotation = ctree["Rotation"].ToTagList();
        Rotation = new Orientation();
        Rotation.Yaw = rotation[0].ToTagFloat();
        Rotation.Pitch = rotation[1].ToTagFloat();

        _fire = ctree["Fire"].ToTagShort();
        _air = ctree["Air"].ToTagShort();
        _onGround = ctree["OnGround"].ToTagByte();

        Source = ctree.Copy() as TagNodeCompound;

        return this;
    }

    /// <summary>
    ///     Attempt to load an Entity subtree into the <see cref="Entity" /> with validation.
    /// </summary>
    /// <param name="tree">The root node of an Entity subtree.</param>
    /// <returns>The <see cref="Entity" /> returns itself on success, or null if the tree failed validation.</returns>
    public Entity LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    /// <summary>
    ///     Builds an Entity subtree from the current data.
    /// </summary>
    /// <returns>The root node of an Entity subtree representing the current data.</returns>
    public TagNode BuildTree()
    {
        var tree = new TagNodeCompound();

        var pos = new TagNodeList(TagType.TAG_DOUBLE);
        pos.Add(new TagNodeDouble(Position.X));
        pos.Add(new TagNodeDouble(Position.Y));
        pos.Add(new TagNodeDouble(Position.Z));
        tree["Pos"] = pos;

        var motion = new TagNodeList(TagType.TAG_DOUBLE);
        motion.Add(new TagNodeDouble(Motion.X));
        motion.Add(new TagNodeDouble(Motion.Y));
        motion.Add(new TagNodeDouble(Motion.Z));
        tree["Motion"] = motion;

        var rotation = new TagNodeList(TagType.TAG_FLOAT);
        rotation.Add(new TagNodeFloat((float)Rotation.Yaw));
        rotation.Add(new TagNodeFloat((float)Rotation.Pitch));
        tree["Rotation"] = rotation;

        tree["FallDistance"] = new TagNodeFloat(_fallDistance);
        tree["Fire"] = new TagNodeShort(_fire);
        tree["Air"] = new TagNodeShort(_air);
        tree["OnGround"] = new TagNodeByte(_onGround);

        if (Source != null) tree.MergeFrom(Source);

        return tree;
    }

    /// <summary>
    ///     Validate an Entity subtree against a basic schema.
    /// </summary>
    /// <param name="tree">The root node of an Entity subtree.</param>
    /// <returns>Status indicating whether the tree was valid against the internal schema.</returns>
    public bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, Schema).Verify();
    }

    #endregion
}

/// <summary>
///     A base entity type for all entities except <see cref="Player" /> entities.
/// </summary>
/// <remarks>
///     Generally, this class should be subtyped into new concrete Entity types, as this generic type is unable to
///     capture any of the custom data fields.  It is however still possible to create instances of <see cref="Entity" />
///     objects,
///     which may allow for graceful handling of unknown Entity types.
/// </remarks>
public class TypedEntity : Entity, INbtObject<TypedEntity>, ICopyable<TypedEntity>
{
    /// <summary>
    ///     Creates a new generic <see cref="TypedEntity" /> with the given id.
    /// </summary>
    /// <param name="id">The id (name) of the Entity.</param>
    public TypedEntity(string id)
    {
        ID = id;
    }

    /// <summary>
    ///     Constructs a new <see cref="TypedEntity" /> by copying an existing one.
    /// </summary>
    /// <param name="e">The <see cref="TypedEntity" /> to copy.</param>
    protected TypedEntity(TypedEntity e)
        : base(e)
    {
        ID = e.ID;
    }

    /// <summary>
    ///     Gets the id (type) of the entity.
    /// </summary>
    public string ID { get; private set; }


    #region ICopyable<Entity> Members

    /// <summary>
    ///     Creates a deep-copy of the <see cref="TypedEntity" />.
    /// </summary>
    /// <returns>A deep-copy of the <see cref="TypedEntity" />.</returns>
    public new virtual TypedEntity Copy()
    {
        return new TypedEntity(this);
    }

    #endregion


    #region INBTObject<Entity> Members

    /// <summary>
    ///     Gets a <see cref="SchemaNode" /> representing the basic schema of an Entity.
    /// </summary>
    public new static SchemaNodeCompound Schema { get; } = Entity.Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeScaler("id", TagType.TAG_STRING)
    });

    /// <summary>
    ///     Attempt to load an Entity subtree into the <see cref="TypedEntity" /> without validation.
    /// </summary>
    /// <param name="tree">The root node of an Entity subtree.</param>
    /// <returns>The <see cref="TypedEntity" /> returns itself on success, or null if the tree was unparsable.</returns>
    public new virtual TypedEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        ID = ctree["id"].ToTagString();

        return this;
    }

    /// <summary>
    ///     Attempt to load an Entity subtree into the <see cref="TypedEntity" /> with validation.
    /// </summary>
    /// <param name="tree">The root node of an Entity subtree.</param>
    /// <returns>The <see cref="TypedEntity" /> returns itself on success, or null if the tree failed validation.</returns>
    public new virtual TypedEntity LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    /// <summary>
    ///     Builds an Entity subtree from the current data.
    /// </summary>
    /// <returns>The root node of an Entity subtree representing the current data.</returns>
    public new virtual TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["id"] = new TagNodeString(ID);

        return tree;
    }

    /// <summary>
    ///     Validate an Entity subtree against a basic schema.
    /// </summary>
    /// <param name="tree">The root node of an Entity subtree.</param>
    /// <returns>Status indicating whether the tree was valid against the internal schema.</returns>
    public new virtual bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, Schema).Verify();
    }

    #endregion
}
