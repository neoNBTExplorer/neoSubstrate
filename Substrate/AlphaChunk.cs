using System;
using System.Collections.Generic;
using System.IO;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

/// <summary>
///     A Minecraft Alpha- and Beta-compatible chunk data structure.
/// </summary>
/// <remarks>
///     A Chunk internally wraps an NBT_Tree of raw chunk data.  Modifying the chunk will update the tree, and vice-versa.
/// </remarks>
public class AlphaChunk : IChunk, INbtObject<AlphaChunk>, ICopyable<AlphaChunk>
{
    private const int XDIM = 16;
    private const int YDIM = 128;
    private const int ZDIM = 16;

    /// <summary>
    ///     An NBT Schema definition for valid chunk data.
    /// </summary>
    public static SchemaNodeCompound LevelSchema = new()
    {
        new SchemaNodeCompound("Level")
        {
            new SchemaNodeArray("Blocks", 32768),
            new SchemaNodeArray("Data", 16384),
            new SchemaNodeArray("SkyLight", 16384),
            new SchemaNodeArray("BlockLight", 16384),
            new SchemaNodeArray("HeightMap", 256),
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

    private XZYNibbleArray _blockLight;

    private XZYByteArray _blocks;

    private XZYNibbleArray _data;

    private TagNodeList _entities;
    private ZXByteArray _heightMap;
    private XZYNibbleArray _skyLight;
    private TagNodeList _tileEntities;
    private TagNodeList _tileTicks;


    private AlphaChunk()
    {
    }

    /// <summary>
    ///     Provides raw access to the underlying NBT_Tree.
    /// </summary>
    public NbtTree Tree { get; private set; }

    /// <summary>
    ///     Gets the global X-coordinate of the chunk.
    /// </summary>
    public int X { get; private set; }

    /// <summary>
    ///     Gets the global Z-coordinate of the chunk.
    /// </summary>
    public int Z { get; private set; }

    /// <summary>
    ///     Gets the collection of all blocks and their data stored in the chunk.
    /// </summary>
    public AlphaBlockCollection Blocks { get; private set; }

    public AnvilBiomeCollection Biomes => null;

    /// <summary>
    ///     Gets the collection of all entities stored in the chunk.
    /// </summary>
    public EntityCollection Entities { get; private set; }

    /// <summary>
    ///     Gets or sets the chunk's TerrainPopulated status.
    /// </summary>
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
    public void SetLocation(int x, int z)
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

    /// <summary>
    ///     Saves a Chunk's underlying NBT_Tree to an output stream.
    /// </summary>
    /// <param name="outStream">An open, writable output stream.</param>
    /// <returns>True if the data is written out to the stream.</returns>
    public bool Save(Stream outStream)
    {
        if (outStream == null || !outStream.CanWrite) return false;

        BuildConditional();

        Tree.WriteTo(outStream);

        return true;
    }


    #region ICopyable<Chunk> Members

    /// <summary>
    ///     Creates a deep copy of the Chunk and its underlying NBT tree.
    /// </summary>
    /// <returns>A new Chunk with copied data.</returns>
    public AlphaChunk Copy()
    {
        return Create(Tree.Copy());
    }

    #endregion

    /// <summary>
    ///     Creates a default (empty) chunk.
    /// </summary>
    /// <param name="x">Global X-coordinate of the chunk.</param>
    /// <param name="z">Global Z-coordinate of the chunk.</param>
    /// <returns>A new Chunk object.</returns>
    public static AlphaChunk Create(int x, int z)
    {
        var c = new AlphaChunk();

        c.X = x;
        c.Z = z;

        c.BuildNBTTree();
        return c;
    }

    /// <summary>
    ///     Creates a chunk object from an existing NBT_Tree.
    /// </summary>
    /// <param name="tree">An NBT_Tree conforming to the chunk schema definition.</param>
    /// <returns>A new Chunk object wrapping an existing NBT_Tree.</returns>
    public static AlphaChunk Create(NbtTree tree)
    {
        var c = new AlphaChunk();

        return c.LoadTree(tree.Root);
    }

    /// <summary>
    ///     Creates a chunk object from a verified NBT_Tree.
    /// </summary>
    /// <param name="tree">An NBT_Tree conforming to the chunk schema definition.</param>
    /// <returns>A new Chunk object wrapping an existing NBT_Tree, or null on verification failure.</returns>
    public static AlphaChunk CreateVerified(NbtTree tree)
    {
        var c = new AlphaChunk();

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
        var elements3 = elements2 * YDIM;

        var blocks = new TagNodeByteArray(new byte[elements3]);
        var data = new TagNodeByteArray(new byte[elements3 >> 1]);
        var blocklight = new TagNodeByteArray(new byte[elements3 >> 1]);
        var skylight = new TagNodeByteArray(new byte[elements3 >> 1]);
        var heightMap = new TagNodeByteArray(new byte[elements2]);

        _blocks = new XZYByteArray(XDIM, YDIM, ZDIM, blocks);
        _data = new XZYNibbleArray(XDIM, YDIM, ZDIM, data);
        _blockLight = new XZYNibbleArray(XDIM, YDIM, ZDIM, blocklight);
        _skyLight = new XZYNibbleArray(XDIM, YDIM, ZDIM, skylight);
        _heightMap = new ZXByteArray(XDIM, ZDIM, heightMap);

        _entities = new TagNodeList(TagType.TAG_COMPOUND);
        _tileEntities = new TagNodeList(TagType.TAG_COMPOUND);
        _tileTicks = new TagNodeList(TagType.TAG_COMPOUND);

        var level = new TagNodeCompound();
        level.Add("Blocks", blocks);
        level.Add("Data", data);
        level.Add("SkyLight", blocklight);
        level.Add("BlockLight", skylight);
        level.Add("HeightMap", heightMap);
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


    #region INBTObject<Chunk> Members

    /// <summary>
    ///     Loads the Chunk from an NBT tree rooted at the given TagValue node.
    /// </summary>
    /// <param name="tree">Root node of an NBT tree.</param>
    /// <returns>A reference to the current Chunk, or null if the tree is unparsable.</returns>
    public AlphaChunk LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null) return null;

        Tree = new NbtTree(ctree);

        var level = Tree.Root["Level"] as TagNodeCompound;

        _blocks = new XZYByteArray(XDIM, YDIM, ZDIM, level["Blocks"] as TagNodeByteArray);
        _data = new XZYNibbleArray(XDIM, YDIM, ZDIM, level["Data"] as TagNodeByteArray);
        _blockLight = new XZYNibbleArray(XDIM, YDIM, ZDIM, level["BlockLight"] as TagNodeByteArray);
        _skyLight = new XZYNibbleArray(XDIM, YDIM, ZDIM, level["SkyLight"] as TagNodeByteArray);
        _heightMap = new ZXByteArray(XDIM, ZDIM, level["HeightMap"] as TagNodeByteArray);

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

        return this;
    }

    /// <summary>
    ///     Loads the Chunk from a validated NBT tree rooted at the given TagValue node.
    /// </summary>
    /// <param name="tree">Root node of an NBT tree.</param>
    /// <returns>A reference to the current Chunk, or null if the tree does not conform to the chunk's NBT Schema definition.</returns>
    public AlphaChunk LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    /// <summary>
    ///     Gets a valid NBT tree representing the Chunk.
    /// </summary>
    /// <returns>The root node of the Chunk's NBT tree.</returns>
    public TagNode BuildTree()
    {
        BuildConditional();

        return Tree.Root;
    }

    /// <summary>
    ///     Validates an NBT tree against the chunk's NBT schema definition.
    /// </summary>
    /// <param name="tree">The root node of the NBT tree to verify.</param>
    /// <returns>Status indicating if the tree represents a valid chunk.</returns>
    public bool ValidateTree(TagNode tree)
    {
        var v = new NbtVerifier(tree, LevelSchema);
        return v.Verify();
    }

    #endregion
}
