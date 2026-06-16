using System;
using System.IO;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.Data;

/// <summary>
///     Represents the complete data of a Map item.
/// </summary>
public class Map : INbtObject<Map>, ICopyable<Map>
{
    private static readonly SchemaNodeCompound _schema = new()
    {
        new SchemaNodeCompound("data")
        {
            new SchemaNodeScaler("scale", TagType.TAG_BYTE),
            new SchemaNodeScaler("dimension", TagType.TAG_BYTE),
            new SchemaNodeScaler("height", TagType.TAG_SHORT),
            new SchemaNodeScaler("width", TagType.TAG_SHORT),
            new SchemaNodeScaler("xCenter", TagType.TAG_INT),
            new SchemaNodeScaler("zCenter", TagType.TAG_INT),
            new SchemaNodeArray("colors")
        }
    };

    private readonly NbtWorld _world;

    private byte _dimension;
    private short _height;
    private int _id;

    private byte _scale;

    private TagNodeCompound _source;
    private short _width;

    /// <summary>
    ///     Creates a new default <see cref="Map" /> object.
    /// </summary>
    public Map()
    {
        _scale = 3;
        _dimension = 0;
        _height = 128;
        _width = 128;

        Colors = new byte[_width * _height];
    }

    /// <summary>
    ///     Creates a new <see cref="Map" /> object with copied data.
    /// </summary>
    /// <param name="p">A <see cref="Map" /> to copy data from.</param>
    protected Map(Map p)
    {
        _world = p._world;
        _id = p._id;

        _scale = p._scale;
        _dimension = p._dimension;
        _height = p._height;
        _width = p._width;
        X = p.X;
        Z = p.Z;

        Colors = new byte[_width * _height];
        if (p.Colors != null) p.Colors.CopyTo(Colors, 0);
    }

    /// <summary>
    ///     Gets or sets the id value associated with this map.
    /// </summary>
    public int Id
    {
        get => _id;
        set
        {
            if (_id < 0 || _id >= 65536)
                throw new ArgumentOutOfRangeException("value", value, "Map Ids must be in the range [0, 65535].");
            _id = value;
        }
    }

    /// <summary>
    ///     Gets or sets the scale of the map.  Acceptable values are 0 (1:1) to 4 (1:16).
    /// </summary>
    public int Scale
    {
        get => _scale;
        set => _scale = (byte)value;
    }

    /// <summary>
    ///     Gets or sets the (World) Dimension of the map.
    /// </summary>
    public int Dimension
    {
        get => _dimension;
        set => _dimension = (byte)value;
    }

    /// <summary>
    ///     Gets or sets the height of the map.
    /// </summary>
    /// <remarks>If the new height dimension is different, the map's color data will be reset.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the new height value is zero or negative.</exception>
    public int Height
    {
        get => _height;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException("value", "Height must be a positive number");
            if (_height != value)
            {
                _height = (short)value;
                Colors = new byte[_width * _height];
            }
        }
    }

    /// <summary>
    ///     Gets or sets the width of the map.
    /// </summary>
    /// <remarks>If the new width dimension is different, the map's color data will be reset.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the new width value is zero or negative.</exception>
    public int Width
    {
        get => _width;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException("value", "Width must be a positive number");
            if (_width != value)
            {
                _width = (short)value;
                Colors = new byte[_width * _height];
            }
        }
    }

    /// <summary>
    ///     Gets or sets the global X-coordinate that this map is centered on, in blocks.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    ///     Gets or sets the global Z-coordinate that this map is centered on, in blocks.
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    ///     Gets the raw byte array of the map's color index values.
    /// </summary>
    public byte[] Colors { get; private set; }

    /// <summary>
    ///     Gets or sets a color index value within the map's internal colors bitmap.
    /// </summary>
    /// <param name="x">The X-coordinate to get or set.</param>
    /// <param name="z">The Z-coordinate to get or set.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown when the X- or Z-coordinates exceed the map dimensions.</exception>
    public byte this[int x, int z]
    {
        get
        {
            if (x < 0 || x >= _width || z < 0 || z >= _height) throw new IndexOutOfRangeException();
            return Colors[x + _width * z];
        }

        set
        {
            if (x < 0 || x >= _width || z < 0 || z >= _height) throw new IndexOutOfRangeException();
            Colors[x + _width * z] = value;
        }
    }


    #region ICopyable<Map> Members

    /// <summary>
    ///     Creates a deep-copy of the <see cref="Map" />.
    /// </summary>
    /// <returns>A deep-copy of the <see cref="Map" />.</returns>
    public virtual Map Copy()
    {
        return new Map(this);
    }

    #endregion


    /// <summary>
    ///     Saves a <see cref="Map" /> object to disk as a standard compressed NBT stream.
    /// </summary>
    /// <returns>True if the map was saved; false otherwise.</returns>
    /// <exception cref="Exception">Thrown when an error is encountered writing out the level.</exception>
    public bool Save()
    {
        if (_world == null) return false;

        try
        {
            var path = Path.Combine(_world.Path, _world.DataDirectory);
            var nf = new NBTFile(Path.Combine(path, "map_" + _id + ".dat"));

            using (var zipstr = nf.GetDataOutputStream())
            {
                if (zipstr == null)
                {
                    var nex = new NbtIOException("Failed to initialize compressed NBT stream for output");
                    nex.Data["Map"] = this;
                    throw nex;
                }

                new NbtTree(BuildTree() as TagNodeCompound).WriteTo(zipstr);
            }

            return true;
        }
        catch (Exception ex)
        {
            var mex = new Exception("Could not save map file.", ex); // TODO: Exception Type
            mex.Data["Map"] = this;
            throw mex;
        }
    }


    #region INBTObject<Map> Members

    /// <summary>
    ///     Attempt to load a Map subtree into the <see cref="Map" /> without validation.
    /// </summary>
    /// <param name="tree">The root node of a Map subtree.</param>
    /// <returns>The <see cref="Map" /> returns itself on success, or null if the tree was unparsable.</returns>
    public virtual Map LoadTree(TagNode tree)
    {
        var dtree = tree as TagNodeCompound;
        if (dtree == null) return null;

        var ctree = dtree["data"].ToTagCompound();

        _scale = ctree["scale"].ToTagByte();
        _dimension = ctree["dimension"].ToTagByte();
        _height = ctree["height"].ToTagShort();
        _width = ctree["width"].ToTagShort();
        X = ctree["xCenter"].ToTagInt();
        Z = ctree["zCenter"].ToTagInt();

        Colors = ctree["colors"].ToTagByteArray();

        _source = ctree.Copy() as TagNodeCompound;

        return this;
    }

    /// <summary>
    ///     Attempt to load a Map subtree into the <see cref="Map" /> with validation.
    /// </summary>
    /// <param name="tree">The root node of a Map subtree.</param>
    /// <returns>The <see cref="Map" /> returns itself on success, or null if the tree failed validation.</returns>
    public virtual Map LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        var map = LoadTree(tree);

        if (map != null)
            if (map.Colors.Length != map._width * map._height)
                throw new Exception("Unexpected length of colors byte array in Map"); // TODO: Expception Type

        return map;
    }

    /// <summary>
    ///     Builds a Map subtree from the current data.
    /// </summary>
    /// <returns>The root node of a Map subtree representing the current data.</returns>
    public virtual TagNode BuildTree()
    {
        var data = new TagNodeCompound();
        data["scale"] = new TagNodeByte(_scale);
        data["dimension"] = new TagNodeByte(_dimension);
        data["height"] = new TagNodeShort(_height);
        data["width"] = new TagNodeShort(_width);
        data["xCenter"] = new TagNodeInt(X);
        data["zCenter"] = new TagNodeInt(Z);

        data["colors"] = new TagNodeByteArray(Colors);

        if (_source != null) data.MergeFrom(_source);

        var tree = new TagNodeCompound();
        tree.Add("data", data);

        return tree;
    }

    /// <summary>
    ///     Validate a Map subtree against a schema defintion.
    /// </summary>
    /// <param name="tree">The root node of a Map subtree.</param>
    /// <returns>Status indicating whether the tree was valid against the internal schema.</returns>
    public virtual bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, _schema).Verify();
    }

    #endregion
}