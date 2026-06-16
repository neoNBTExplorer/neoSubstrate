using System;
using System.Collections.Generic;
using System.IO;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

public class AnvilChunk : IChunk, INbtObject<AnvilChunk>, ICopyable<AnvilChunk>
{
    private const int XDIM = 16;
    private const int YDIM = 256;
    private const int ZDIM = 16;

    public static SchemaNodeCompound LevelSchema = new()
    {
        new SchemaNodeCompound("Level")
        {
            new SchemaNodeList("Sections", TagType.TAG_COMPOUND, new SchemaNodeCompound
            {
                new SchemaNodeArray("Blocks", 4096),
                new SchemaNodeArray("Data", 2048),
                new SchemaNodeArray("SkyLight", 2048),
                new SchemaNodeArray("BlockLight", 2048),
                new SchemaNodeScaler("Y", TagType.TAG_BYTE),
                new SchemaNodeArray("Add", 2048, SchemaOptions.OPTIONAL)
            }),
            new SchemaNodeArray("Biomes", 256, SchemaOptions.OPTIONAL),
            new SchemaNodeIntArray("HeightMap", 256),
            new SchemaNodeList("Entities", TagType.TAG_COMPOUND, SchemaOptions.CREATE_ON_MISSING),
            new SchemaNodeList("TileEntities", TagType.TAG_COMPOUND, TileEntity.Schema,
                SchemaOptions.CREATE_ON_MISSING),
            new SchemaNodeList("TileTicks", TagType.TAG_COMPOUND, TileTick.Schema, SchemaOptions.OPTIONAL),
            new SchemaNodeScaler("LastUpdate", TagType.TAG_LONG, SchemaOptions.CREATE_ON_MISSING),
            new SchemaNodeScaler("xPos", TagType.TAG_INT),
            new SchemaNodeScaler("zPos", TagType.TAG_INT),
            new SchemaNodeScaler("TerrainPopulated", TagType.TAG_BYTE, SchemaOptions.CREATE_ON_MISSING)
        }
    };

    private ZXByteArray _biomes;
    private IDataArray3 _blockLight;

    private IDataArray3 _blocks;

    private IDataArray3 _data;

    private TagNodeList _entities;

    private ZXIntArray _heightMap;

    private IDataArray3 _skyLight;
    private TagNodeList _tileEntities;
    private TagNodeList _tileTicks;


    private AnvilChunk()
    {
        Sections = new AnvilSection[16];
    }

    public AnvilSection[] Sections { get; private set; }

    public NbtTree Tree { get; private set; }

    public int X { get; private set; }

    public int Z { get; private set; }

    public AlphaBlockCollection Blocks { get; private set; }

    public AnvilBiomeCollection Biomes { get; private set; }

    public EntityCollection Entities { get; private set; }

    public bool IsTerrainPopulated
    {
        get => Tree.Root["Level"].ToTagCompound()["TerrainPopulated"].ToTagByte() == 1;
        set => Tree.Root["Level"].ToTagCompound()["TerrainPopulated"].ToTagByte().Data = (byte)(value ? 1 : 0);
    }

    /// <summary>
    ///     Updates the chunk's global world coordinates.
    /// </summary>
    /// <param name="x">Global X-coordinate.</param>
    /// <param name="z">Global Z-coordinate.</param>
    public virtual void SetLocation(int x, int z)
    {
        var diffx = (x - X) * XDIM;
        var diffz = (z - Z) * ZDIM;

        // Update chunk position

        X = x;
        Z = z;

        Tree.Root["Level"].ToTagCompound()["xPos"].ToTagInt().Data = x;
        Tree.Root["Level"].ToTagCompound()["zPos"].ToTagInt().Data = z;

        // Update tile entity coordinates

        var tileEntites = new List<TileEntity>();
        foreach (TagNodeCompound tag in _tileEntities)
        {
            var te = TileEntityFactory.Create(tag);
            if (te == null) te = TileEntity.FromTreeSafe(tag);

            if (te != null)
            {
                te.MoveBy(diffx, 0, diffz);
                tileEntites.Add(te);
            }
        }

        _tileEntities.Clear();
        foreach (var te in tileEntites) _tileEntities.Add(te.BuildTree());

        // Update tile tick coordinates

        if (_tileTicks != null)
        {
            var tileTicks = new List<TileTick>();
            foreach (TagNodeCompound tag in _tileTicks)
            {
                var tt = TileTick.FromTreeSafe(tag);

                if (tt != null)
                {
                    tt.MoveBy(diffx, 0, diffz);
                    tileTicks.Add(tt);
                }
            }

            _tileTicks.Clear();
            foreach (var tt in tileTicks) _tileTicks.Add(tt.BuildTree());
        }

        // Update entity coordinates

        var entities = new List<TypedEntity>();
        foreach (var entity in Entities)
        {
            entity.MoveBy(diffx, 0, diffz);
            entities.Add(entity);
        }

        _entities.Clear();
        foreach (var entity in entities) Entities.Add(entity);
    }

    public bool Save(Stream outStream)
    {
        if (outStream == null || !outStream.CanWrite) return false;

        BuildConditional();

        var tree = new NbtTree();
        tree.Root["Level"] = BuildTree();

        tree.WriteTo(outStream);

        return true;
    }

    #region ICopyable<AnvilChunk> Members

    public AnvilChunk Copy()
    {
        return Create(Tree.Copy());
    }

    #endregion

    public static AnvilChunk Create(int x, int z)
    {
        var c = new AnvilChunk();

        c.X = x;
        c.Z = z;

        c.BuildNBTTree();
        return c;
    }

    public static AnvilChunk Create(NbtTree tree)
    {
        var c = new AnvilChunk();

        return c.LoadTree(tree.Root);
    }

    public static AnvilChunk CreateVerified(NbtTree tree)
    {
        var c = new AnvilChunk();

        return c.LoadTreeSafe(tree.Root);
    }

    private void BuildConditional()
    {
        var level = Tree.Root["Level"] as TagNodeCompound;
        if (_tileTicks != Blocks.TileTicks && Blocks.TileTicks.Count > 0)
        {
            _tileTicks = Blocks.TileTicks;
            level["TileTicks"] = _tileTicks;
        }
    }

    private void BuildNBTTree()
    {
        var elements2 = XDIM * ZDIM;

        Sections = new AnvilSection[16];
        var sections = new TagNodeList(TagType.TAG_COMPOUND);

        for (var i = 0; i < Sections.Length; i++)
        {
            Sections[i] = new AnvilSection(i);
            sections.Add(Sections[i].BuildTree());
        }

        var blocksBA = new FusedDataArray3[Sections.Length];
        var dataBA = new YZXNibbleArray[Sections.Length];
        var skyLightBA = new YZXNibbleArray[Sections.Length];
        var blockLightBA = new YZXNibbleArray[Sections.Length];

        for (var i = 0; i < Sections.Length; i++)
        {
            blocksBA[i] = new FusedDataArray3(Sections[i].AddBlocks, Sections[i].Blocks);
            dataBA[i] = Sections[i].Data;
            skyLightBA[i] = Sections[i].SkyLight;
            blockLightBA[i] = Sections[i].BlockLight;
        }

        _blocks = new CompositeDataArray3(blocksBA);
        _data = new CompositeDataArray3(dataBA);
        _skyLight = new CompositeDataArray3(skyLightBA);
        _blockLight = new CompositeDataArray3(blockLightBA);

        var heightMap = new TagNodeIntArray(new int[elements2]);
        _heightMap = new ZXIntArray(XDIM, ZDIM, heightMap);

        var biomes = new TagNodeByteArray(new byte[elements2]);
        _biomes = new ZXByteArray(XDIM, ZDIM, biomes);
        for (var x = 0; x < XDIM; x++)
        for (var z = 0; z < ZDIM; z++)
            _biomes[x, z] = BiomeType.Default;

        _entities = new TagNodeList(TagType.TAG_COMPOUND);
        _tileEntities = new TagNodeList(TagType.TAG_COMPOUND);
        _tileTicks = new TagNodeList(TagType.TAG_COMPOUND);

        var level = new TagNodeCompound();
        level.Add("Sections", sections);
        level.Add("HeightMap", heightMap);
        level.Add("Biomes", biomes);
        level.Add("Entities", _entities);
        level.Add("TileEntities", _tileEntities);
        level.Add("TileTicks", _tileTicks);
        level.Add("LastUpdate", new TagNodeLong(Timestamp()));
        level.Add("xPos", new TagNodeInt(X));
        level.Add("zPos", new TagNodeInt(Z));
        level.Add("TerrainPopulated", new TagNodeByte());

        Tree = new NbtTree();
        Tree.Root.Add("Level", level);

        Blocks = new AlphaBlockCollection(_blocks, _data, _blockLight, _skyLight, _heightMap, _tileEntities);
        Entities = new EntityCollection(_entities);
    }

    private int Timestamp()
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return (int)((DateTime.UtcNow - epoch).Ticks / (10000L * 1000L));
    }

    #region INbtObject<AnvilChunk> Members

    public AnvilChunk LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null) return null;

        Tree = new NbtTree(ctree);

        var level = Tree.Root["Level"] as TagNodeCompound;

        var sections = level["Sections"] as TagNodeList;
        foreach (TagNodeCompound section in sections)
        {
            var anvilSection = new AnvilSection(section);
            if (anvilSection.Y < 0 || anvilSection.Y >= Sections.Length)
                continue;
            Sections[anvilSection.Y] = anvilSection;
        }

        var blocksBA = new FusedDataArray3[Sections.Length];
        var dataBA = new YZXNibbleArray[Sections.Length];
        var skyLightBA = new YZXNibbleArray[Sections.Length];
        var blockLightBA = new YZXNibbleArray[Sections.Length];

        for (var i = 0; i < Sections.Length; i++)
        {
            if (Sections[i] == null)
                Sections[i] = new AnvilSection(i);

            blocksBA[i] = new FusedDataArray3(Sections[i].AddBlocks, Sections[i].Blocks);
            dataBA[i] = Sections[i].Data;
            skyLightBA[i] = Sections[i].SkyLight;
            blockLightBA[i] = Sections[i].BlockLight;
        }

        _blocks = new CompositeDataArray3(blocksBA);
        _data = new CompositeDataArray3(dataBA);
        _skyLight = new CompositeDataArray3(skyLightBA);
        _blockLight = new CompositeDataArray3(blockLightBA);

        _heightMap = new ZXIntArray(XDIM, ZDIM, level["HeightMap"] as TagNodeIntArray);

        if (level.ContainsKey("Biomes"))
        {
            _biomes = new ZXByteArray(XDIM, ZDIM, level["Biomes"] as TagNodeByteArray);
        }
        else
        {
            level["Biomes"] = new TagNodeByteArray(new byte[256]);
            _biomes = new ZXByteArray(XDIM, ZDIM, level["Biomes"] as TagNodeByteArray);
            for (var x = 0; x < XDIM; x++)
            for (var z = 0; z < ZDIM; z++)
                _biomes[x, z] = BiomeType.Default;
        }

        _entities = level["Entities"] as TagNodeList;
        _tileEntities = level["TileEntities"] as TagNodeList;

        if (level.ContainsKey("TileTicks"))
            _tileTicks = level["TileTicks"] as TagNodeList;
        else
            _tileTicks = new TagNodeList(TagType.TAG_COMPOUND);

        // List-type patch up
        if (_entities.Count == 0)
        {
            level["Entities"] = new TagNodeList(TagType.TAG_COMPOUND);
            _entities = level["Entities"] as TagNodeList;
        }

        if (_tileEntities.Count == 0)
        {
            level["TileEntities"] = new TagNodeList(TagType.TAG_COMPOUND);
            _tileEntities = level["TileEntities"] as TagNodeList;
        }

        if (_tileTicks.Count == 0)
        {
            level["TileTicks"] = new TagNodeList(TagType.TAG_COMPOUND);
            _tileTicks = level["TileTicks"] as TagNodeList;
        }

        X = level["xPos"].ToTagInt();
        Z = level["zPos"].ToTagInt();

        Blocks = new AlphaBlockCollection(_blocks, _data, _blockLight, _skyLight, _heightMap, _tileEntities,
            _tileTicks);
        Entities = new EntityCollection(_entities);
        Biomes = new AnvilBiomeCollection(_biomes);

        return this;
    }

    public AnvilChunk LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    private bool ShouldIncludeSection(AnvilSection section)
    {
        var y = (section.Y + 1) * section.Blocks.YDim;
        for (var i = 0; i < _heightMap.Length; i++)
            if (_heightMap[i] > y)
                return true;

        return !section.CheckEmpty();
    }

    public TagNode BuildTree()
    {
        var level = Tree.Root["Level"] as TagNodeCompound;
        var levelCopy = new TagNodeCompound();
        foreach (var node in level)
            levelCopy.Add(node.Key, node.Value);

        var sections = new TagNodeList(TagType.TAG_COMPOUND);
        for (var i = 0; i < Sections.Length; i++)
            if (ShouldIncludeSection(Sections[i]))
                sections.Add(Sections[i].BuildTree());

        levelCopy["Sections"] = sections;

        if (_tileTicks.Count == 0)
            levelCopy.Remove("TileTicks");

        return levelCopy;
    }

    public bool ValidateTree(TagNode tree)
    {
        var v = new NbtVerifier(tree, LevelSchema);
        return v.Verify();
    }

    #endregion
}