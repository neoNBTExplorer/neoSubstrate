using Substrate.Nbt;

namespace Substrate.TileEntities;

public class TileEntityMobSpawner : TileEntity
{
    public static readonly SchemaNodeCompound MobSpawnerSchema = Schema.MergeInto(new SchemaNodeCompound("")
    {
        new SchemaNodeString("id", TypeId),
        new SchemaNodeScaler("EntityId", TagType.TAG_STRING),
        new SchemaNodeScaler("Delay", TagType.TAG_SHORT),
        new SchemaNodeScaler("MaxSpawnDelay", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("MinSpawnDelay", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("SpawnCount", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("SpawnRange", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("MaxNearbyEnemies", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("RequiredPlayerRange", TagType.TAG_SHORT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("MaxExperience", TagType.TAG_INT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("RemainingExperience", TagType.TAG_INT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("ExperienceRegenTick", TagType.TAG_INT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("ExperienceRegenRate", TagType.TAG_INT, SchemaOptions.OPTIONAL),
        new SchemaNodeScaler("ExperienceRegenAmount", TagType.TAG_INT, SchemaOptions.OPTIONAL),
        new SchemaNodeCompound("SpawnData", SchemaOptions.OPTIONAL)
    });

    private short _delay;
    private int? _experienceRegenAmount;
    private int? _experienceRegenRate;
    private int? _experienceRegenTick;
    private short? _maxDelay;
    private int? _maxExperience;
    private short? _maxNearbyEnemies;
    private short? _minDelay;
    private int? _remainingExperience;
    private short? _requiredPlayerRange;
    private short? _spawnCount;
    private short? _spawnRange;

    protected TileEntityMobSpawner(string id)
        : base(id)
    {
    }

    public TileEntityMobSpawner()
        : this(TypeId)
    {
    }

    public TileEntityMobSpawner(TileEntity te)
        : base(te)
    {
        var tes = te as TileEntityMobSpawner;
        if (tes != null)
        {
            _delay = tes._delay;
            EntityID = tes.EntityID;
            _maxDelay = tes._maxDelay;
            _minDelay = tes._minDelay;
            _spawnCount = tes._spawnCount;
            _spawnRange = tes._spawnRange;
            _maxNearbyEnemies = tes._maxNearbyEnemies;
            _requiredPlayerRange = tes._requiredPlayerRange;
            _maxExperience = tes._maxExperience;
            _remainingExperience = tes._remainingExperience;
            _experienceRegenTick = tes._experienceRegenTick;
            _experienceRegenRate = tes._experienceRegenRate;
            _experienceRegenAmount = tes._experienceRegenAmount;

            if (tes.SpawnData != null)
                SpawnData = tes.SpawnData.Copy() as TagNodeCompound;
        }
    }

    public static string TypeId => "MobSpawner";

    public int Delay
    {
        get => _delay;
        set => _delay = (short)value;
    }

    public TagNodeCompound SpawnData { get; set; }

    public string EntityID { get; set; }

    public short MaxSpawnDelay
    {
        get => _maxDelay ?? 0;
        set => _maxDelay = value;
    }

    public short MinSpawnDelay
    {
        get => _minDelay ?? 0;
        set => _minDelay = value;
    }

    public short SpawnCount
    {
        get => _spawnCount ?? 0;
        set => _spawnCount = value;
    }

    public short SpawnRange
    {
        get => _spawnRange ?? 0;
        set => _spawnRange = value;
    }

    public short MaxNearbyEnemies
    {
        get => _maxNearbyEnemies ?? 0;
        set => _maxNearbyEnemies = value;
    }

    public short RequiredPlayerRange
    {
        get => _requiredPlayerRange ?? 0;
        set => _requiredPlayerRange = value;
    }

    public int MaxExperience
    {
        get => _maxExperience ?? 0;
        set => _maxExperience = value;
    }

    public int RemainingExperience
    {
        get => _remainingExperience ?? 0;
        set => _remainingExperience = value;
    }

    public int ExperienceRegenTick
    {
        get => _experienceRegenTick ?? 0;
        set => _experienceRegenTick = value;
    }

    public int ExperienceRegenRate
    {
        get => _experienceRegenRate ?? 0;
        set => _experienceRegenRate = value;
    }

    public int ExperienceRegenAmount
    {
        get => _experienceRegenAmount ?? 0;
        set => _experienceRegenAmount = value;
    }


    #region ICopyable<TileEntity> Members

    public override TileEntity Copy()
    {
        return new TileEntityMobSpawner(this);
    }

    #endregion


    #region INBTObject<TileEntity> Members

    public override TileEntity LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null || base.LoadTree(tree) == null) return null;

        _delay = ctree["Delay"].ToTagShort();
        EntityID = ctree["EntityId"].ToTagString();

        if (ctree.ContainsKey("MaxSpawnDelay"))
            _maxDelay = ctree["MaxSpawnDelay"].ToTagShort();
        if (ctree.ContainsKey("MinSpawnDelay"))
            _minDelay = ctree["MinSpawnDelay"].ToTagShort();
        if (ctree.ContainsKey("SpawnCount"))
            _spawnCount = ctree["SpawnCount"].ToTagShort();
        if (ctree.ContainsKey("SpawnRange"))
            _spawnRange = ctree["SpawnRange"].ToTagShort();
        if (ctree.ContainsKey("MaxNearbyEnemies"))
            _maxNearbyEnemies = ctree["MaxNearbyEnemies"].ToTagShort();
        if (ctree.ContainsKey("RequiredPlayerRange"))
            _requiredPlayerRange = ctree["RequiredPlayerRange"].ToTagShort();
        if (ctree.ContainsKey("MaxExperience"))
            _maxExperience = ctree["MaxExperience"].ToTagInt();
        if (ctree.ContainsKey("RemainingExperience"))
            _remainingExperience = ctree["RemainingExperience"].ToTagInt();
        if (ctree.ContainsKey("ExperienceRegenTick"))
            _experienceRegenTick = ctree["ExperienceRegenTick"].ToTagInt();
        if (ctree.ContainsKey("ExperienceRegenRate"))
            _experienceRegenRate = ctree["ExperienceRegenRate"].ToTagInt();
        if (ctree.ContainsKey("ExperienceRegenAmount"))
            _experienceRegenRate = ctree["ExperienceRegenAmount"].ToTagInt();

        if (ctree.ContainsKey("SpawnData"))
            SpawnData = ctree["SpawnData"].ToTagCompound();

        return this;
    }

    public override TagNode BuildTree()
    {
        var tree = base.BuildTree() as TagNodeCompound;
        tree["EntityId"] = new TagNodeString(EntityID);
        tree["Delay"] = new TagNodeShort(_delay);

        if (_maxDelay != null)
            tree["MaxSpawnDelay"] = new TagNodeShort(_maxDelay ?? 0);
        if (_minDelay != null)
            tree["MinSpawnDelay"] = new TagNodeShort(_minDelay ?? 0);
        if (_spawnCount != null)
            tree["SpawnCount"] = new TagNodeShort(_spawnCount ?? 0);
        if (_spawnRange != null)
            tree["SpawnRange"] = new TagNodeShort(_spawnRange ?? 0);
        if (_maxNearbyEnemies != null)
            tree["MaxNearbyEnemies"] = new TagNodeShort(_maxNearbyEnemies ?? 0);
        if (_requiredPlayerRange != null)
            tree["RequiredPlayerRange"] = new TagNodeShort(_requiredPlayerRange ?? 0);
        if (_maxExperience != null)
            tree["MaxExperience"] = new TagNodeInt(_maxExperience ?? 0);
        if (_remainingExperience != null)
            tree["RemainingExperience"] = new TagNodeInt(_remainingExperience ?? 0);
        if (_experienceRegenTick != null)
            tree["ExperienceRegenTick"] = new TagNodeInt(_experienceRegenTick ?? 0);
        if (_experienceRegenRate != null)
            tree["ExperienceRegenRate"] = new TagNodeInt(_experienceRegenRate ?? 0);
        if (_experienceRegenAmount != null)
            tree["ExperienceRegenAmount"] = new TagNodeInt(_experienceRegenAmount ?? 0);

        if (SpawnData != null && SpawnData.Count > 0)
            tree["SpawnData"] = SpawnData;

        return tree;
    }

    public override bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, MobSpawnerSchema).Verify();
    }

    #endregion
}
