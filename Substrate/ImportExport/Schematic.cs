using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.ImportExport;

/// <summary>
///     Provides import and export support for the 3rd party schematic file format.
/// </summary>
public class Schematic
{
    private static readonly SchemaNodeCompound _schema = new()
    {
        new SchemaNodeScaler("Width", TagType.TAG_SHORT),
        new SchemaNodeScaler("Length", TagType.TAG_SHORT),
        new SchemaNodeScaler("Height", TagType.TAG_SHORT),
        new SchemaNodeString("Materials", "Alpha"),
        new SchemaNodeArray("Blocks"),
        new SchemaNodeArray("Data"),
        new SchemaNodeList("Entities", TagType.TAG_COMPOUND, Entity.Schema),
        new SchemaNodeList("TileEntities", TagType.TAG_COMPOUND, TileEntity.Schema)
    };

    private readonly XZYNibbleArray _blockLight;

    private readonly XZYByteArray _blocks;
    private readonly XZYNibbleArray _data;

    private readonly TagNodeList _entities;
    private readonly ZXByteArray _heightMap;
    private readonly XZYNibbleArray _skyLight;
    private readonly TagNodeList _tileEntities;

    private Schematic()
    {
    }

    /// <summary>
    ///     Create an exportable schematic wrapper around existing blocks and entities.
    /// </summary>
    /// <param name="blocks">An existing <see cref="AlphaBlockCollection" />.</param>
    /// <param name="entities">An existing <see cref="EntityCollection" />.</param>
    public Schematic(AlphaBlockCollection blocks, EntityCollection entities)
    {
        Blocks = blocks;
        Entities = entities;
    }

    /// <summary>
    ///     Create an empty, exportable schematic of given dimensions.
    /// </summary>
    /// <param name="xdim">The length of the X-dimension in blocks.</param>
    /// <param name="ydim">The length of the Y-dimension in blocks.</param>
    /// <param name="zdim">The length of the Z-dimension in blocks.</param>
    public Schematic(int xdim, int ydim, int zdim)
    {
        _blocks = new XZYByteArray(xdim, ydim, zdim);
        _data = new XZYNibbleArray(xdim, ydim, zdim);
        _blockLight = new XZYNibbleArray(xdim, ydim, zdim);
        _skyLight = new XZYNibbleArray(xdim, ydim, zdim);
        _heightMap = new ZXByteArray(xdim, zdim);

        _entities = new TagNodeList(TagType.TAG_COMPOUND);
        _tileEntities = new TagNodeList(TagType.TAG_COMPOUND);

        Blocks = new AlphaBlockCollection(_blocks, _data, _blockLight, _skyLight, _heightMap, _tileEntities);
        Entities = new EntityCollection(_entities);
    }

    /// <summary>
    ///     Imports a schematic file at the given path and returns in as a <see cref="Schematic" /> object.
    /// </summary>
    /// <param name="path">The path to the schematic file.</param>
    /// <returns>A <see cref="Schematic" /> object containing the decoded schematic file data.</returns>
    public static Schematic Import(string path)
    {
        var schematicFile = new NBTFile(path);
        if (!schematicFile.Exists()) return null;
        NbtTree tree;

        using (var nbtStream = schematicFile.GetDataInputStream())
        {
            if (nbtStream == null) return null;

            tree = new NbtTree(nbtStream);
        }

        var v = new NbtVerifier(tree.Root, _schema);
        if (!v.Verify()) return null;

        //TagNodeCompound schematic = tree.Root["Schematic"] as TagNodeCompound;
        var schematic = tree.Root;
        int xdim = schematic["Width"].ToTagShort();
        int zdim = schematic["Length"].ToTagShort();
        int ydim = schematic["Height"].ToTagShort();

        var self = new Schematic(xdim, ydim, zdim);

        // Damnit, schematic is YZX ordering.
        var schemaBlocks = new YZXByteArray(xdim, ydim, zdim, schematic["Blocks"].ToTagByteArray());
        var schemaData = new YZXByteArray(xdim, ydim, zdim, schematic["Data"].ToTagByteArray());

        for (var x = 0; x < xdim; x++)
        for (var y = 0; y < ydim; y++)
        for (var z = 0; z < zdim; z++)
        {
            self._blocks[x, y, z] = schemaBlocks[x, y, z];
            self._data[x, y, z] = schemaData[x, y, z];
        }

        var entities = schematic["Entities"] as TagNodeList;
        foreach (var e in entities) self._entities.Add(e);

        var tileEntities = schematic["TileEntities"] as TagNodeList;
        foreach (var te in tileEntities) self._tileEntities.Add(te);

        self.Blocks.Refresh();

        return self;
    }

    /// <summary>
    ///     Exports the <see cref="Schematic" /> object to a schematic file.
    /// </summary>
    /// <param name="path">The path to write out the schematic file to.</param>
    public void Export(string path)
    {
        var xdim = Blocks.XDim;
        var ydim = Blocks.YDim;
        var zdim = Blocks.ZDim;

        var blockData = new byte[xdim * ydim * zdim];
        var dataData = new byte[xdim * ydim * zdim];

        var schemaBlocks = new YZXByteArray(Blocks.XDim, Blocks.YDim, Blocks.ZDim, blockData);
        var schemaData = new YZXByteArray(Blocks.XDim, Blocks.YDim, Blocks.ZDim, dataData);

        var entities = new TagNodeList(TagType.TAG_COMPOUND);
        var tileEntities = new TagNodeList(TagType.TAG_COMPOUND);

        for (var x = 0; x < xdim; x++)
        for (var z = 0; z < zdim; z++)
        for (var y = 0; y < ydim; y++)
        {
            var block = Blocks.GetBlock(x, y, z);
            schemaBlocks[x, y, z] = (byte)block.ID;
            schemaData[x, y, z] = (byte)block.Data;

            var te = block.GetTileEntity();
            if (te != null)
            {
                te.X = x;
                te.Y = y;
                te.Z = z;

                tileEntities.Add(te.BuildTree());
            }
        }

        foreach (var e in Entities) entities.Add(e.BuildTree());

        var schematic = new TagNodeCompound();
        schematic["Width"] = new TagNodeShort((short)xdim);
        schematic["Length"] = new TagNodeShort((short)zdim);
        schematic["Height"] = new TagNodeShort((short)ydim);

        schematic["Entities"] = entities;
        schematic["TileEntities"] = tileEntities;

        schematic["Materials"] = new TagNodeString("Alpha");

        schematic["Blocks"] = new TagNodeByteArray(blockData);
        schematic["Data"] = new TagNodeByteArray(dataData);

        var schematicFile = new NBTFile(path);

        using (var nbtStream = schematicFile.GetDataOutputStream())
        {
            if (nbtStream == null) return;

            var tree = new NbtTree(schematic, "Schematic");
            tree.WriteTo(nbtStream);
        }
    }

    #region Properties

    /// <summary>
    ///     Gets or sets the underlying block collection.
    /// </summary>
    public AlphaBlockCollection Blocks { get; set; }

    /// <summary>
    ///     Gets or sets the underlying entity collection.
    /// </summary>
    public EntityCollection Entities { get; set; }

    #endregion
}