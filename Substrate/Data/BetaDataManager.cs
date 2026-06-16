using System;
using System.IO;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate.Data;

public class BetaDataManager : DataManager, INbtObject<BetaDataManager>
{
    private static readonly SchemaNodeCompound _schema = new()
    {
        new SchemaNodeScaler("map", TagType.TAG_SHORT)
    };

    private readonly NbtWorld _world;

    private short _mapId;

    private TagNodeCompound _source;

    public BetaDataManager(NbtWorld world)
    {
        _world = world;

        Maps = new MapManager(_world);
    }

    public override int CurrentMapId
    {
        get => _mapId;
        set => _mapId = (short)value;
    }

    public new MapManager Maps { get; }

    protected override IMapManager GetMapManager()
    {
        return Maps;
    }

    public override bool Save()
    {
        if (_world == null) return false;

        try
        {
            var path = Path.Combine(_world.Path, _world.DataDirectory);
            var nf = new NBTFile(Path.Combine(path, "idcounts.dat"));

            using (var zipstr = nf.GetDataOutputStream(CompressionType.None))
            {
                if (zipstr == null)
                {
                    var nex = new NbtIOException("Failed to initialize uncompressed NBT stream for output");
                    nex.Data["DataManager"] = this;
                    throw nex;
                }

                new NbtTree(BuildTree() as TagNodeCompound).WriteTo(zipstr);
            }

            return true;
        }
        catch (Exception ex)
        {
            var lex = new Exception("Could not save idcounts.dat file.", ex);
            lex.Data["DataManager"] = this;
            throw lex;
        }
    }

    #region INBTObject<DataManager>

    public virtual BetaDataManager LoadTree(TagNode tree)
    {
        var ctree = tree as TagNodeCompound;
        if (ctree == null) return null;

        _mapId = ctree["map"].ToTagShort();

        _source = ctree.Copy() as TagNodeCompound;

        return this;
    }

    public virtual BetaDataManager LoadTreeSafe(TagNode tree)
    {
        if (!ValidateTree(tree)) return null;

        return LoadTree(tree);
    }

    public virtual TagNode BuildTree()
    {
        var tree = new TagNodeCompound();

        tree["map"] = new TagNodeLong(_mapId);

        if (_source != null) tree.MergeFrom(_source);

        return tree;
    }

    public virtual bool ValidateTree(TagNode tree)
    {
        return new NbtVerifier(tree, _schema).Verify();
    }

    #endregion
}
