using System;
using System.Collections.Generic;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

using IO = System.IO;

/// <summary>
///     Represents an Alpha-compatible (up to Beta 1.2) Minecraft world.
/// </summary>
public class AlphaWorld : NbtWorld
{
    private const string _PLAYER_DIR = "players";
    private readonly Dictionary<string, BlockManager> _blockMgrs;

    private readonly Dictionary<string, AlphaChunkManager> _chunkMgrs;

    private Level _level;
    private string _levelFile = "level.dat";

    private PlayerManager _playerMan;

    private AlphaWorld()
    {
        _chunkMgrs = new Dictionary<string, AlphaChunkManager>();
        _blockMgrs = new Dictionary<string, BlockManager>();
    }

    /// <summary>
    ///     Gets a reference to this world's <see cref="Level" /> object.
    /// </summary>
    public override Level Level => _level;

    /// <summary>
    ///     Gets a <see cref="BlockManager" /> for the default dimension.
    /// </summary>
    /// <returns>A <see cref="BlockManager" /> tied to the default dimension in this world.</returns>
    /// <remarks>
    ///     Get a <see cref="BlockManager" /> if you need to manage blocks as a global, unbounded matrix.  This abstracts away
    ///     any higher-level organizational divisions.  If your task is going to be heavily performance-bound, consider getting
    ///     a
    ///     <see cref="RegionChunkManager" /> instead and working with blocks on a chunk-local level.
    /// </remarks>
    public new BlockManager GetBlockManager()
    {
        return GetBlockManagerVirt(Dimension.DEFAULT) as BlockManager;
    }

    /// <summary>
    ///     Gets a <see cref="BlockManager" /> for the given dimension.
    /// </summary>
    /// <param name="dim">The id of the dimension to look up.</param>
    /// <returns>A <see cref="BlockManager" /> tied to the given dimension in this world.</returns>
    /// <remarks>
    ///     Get a <see cref="BlockManager" /> if you need to manage blocks as a global, unbounded matrix.  This abstracts away
    ///     any higher-level organizational divisions.  If your task is going to be heavily performance-bound, consider getting
    ///     a
    ///     <see cref="RegionChunkManager" /> instead and working with blocks on a chunk-local level.
    /// </remarks>
    public new BlockManager GetBlockManager(int dim)
    {
        return GetBlockManagerVirt(dim) as BlockManager;
    }

    /// <summary>
    ///     Gets a <see cref="RegionChunkManager" /> for the default dimension.
    /// </summary>
    /// <returns>A <see cref="RegionChunkManager" /> tied to the default dimension in this world.</returns>
    /// <remarks>
    ///     Get a <see cref="RegionChunkManager" /> if you you need to work with easily-digestible, bounded chunks of
    ///     blocks.
    /// </remarks>
    public new AlphaChunkManager GetChunkManager()
    {
        return GetChunkManagerVirt(Dimension.DEFAULT) as AlphaChunkManager;
    }

    /// <summary>
    ///     Gets a <see cref="RegionChunkManager" /> for the given dimension.
    /// </summary>
    /// <param name="dim">The id of the dimension to look up.</param>
    /// <returns>A <see cref="RegionChunkManager" /> tied to the given dimension in this world.</returns>
    /// <remarks>
    ///     Get a <see cref="RegionChunkManager" /> if you you need to work with easily-digestible, bounded chunks of
    ///     blocks.
    /// </remarks>
    public new AlphaChunkManager GetChunkManager(int dim)
    {
        return GetChunkManagerVirt(dim) as AlphaChunkManager;
    }

    /// <summary>
    ///     Gets a <see cref="PlayerManager" /> for maanging players on multiplayer worlds.
    /// </summary>
    /// <returns>A <see cref="PlayerManager" /> for this world.</returns>
    /// <remarks>To manage the player of a single-player world, get a <see cref="Level" /> object for the world instead.</remarks>
    public new PlayerManager GetPlayerManager()
    {
        return GetPlayerManagerVirt() as PlayerManager;
    }

    /// <inherits />
    public override void Save()
    {
        _level.Save();

        foreach (var cm in _chunkMgrs) cm.Value.Save();
    }

    /// <summary>
    ///     Opens an existing Alpha-compatible Minecraft world and returns a new <see cref="AlphaWorld" /> to represent it.
    /// </summary>
    /// <param name="path">The path to the directory containing the world's level.dat, or the path to level.dat itself.</param>
    /// <returns>A new <see cref="AlphaWorld" /> object representing an existing world on disk.</returns>
    public new static AlphaWorld Open(string path)
    {
        return new AlphaWorld().OpenWorld(path);
    }

    /// <summary>
    ///     Creates a new Alpha-compatible Minecraft world and returns a new <see cref="AlphaWorld" /> to represent it.
    /// </summary>
    /// <param name="path">The path to the directory where the new world should be stored.</param>
    /// <returns>A new <see cref="AlphaWorld" /> object representing a new world.</returns>
    /// <remarks>
    ///     This method will attempt to create the specified directory immediately if it does not exist, but will not
    ///     write out any world data unless it is explicitly saved at a later time.
    /// </remarks>
    public static AlphaWorld Create(string path)
    {
        return new AlphaWorld().CreateWorld(path);
    }

    /// <exclude />
    protected override IBlockManager GetBlockManagerVirt(int dim)
    {
        return GetBlockManagerVirt(DimensionFromInt(dim));
    }

    protected override IBlockManager GetBlockManagerVirt(string dim)
    {
        BlockManager rm;
        if (_blockMgrs.TryGetValue(dim, out rm)) return rm;

        OpenDimension(dim);
        return _blockMgrs[dim];
    }

    /// <exclude />
    protected override IChunkManager GetChunkManagerVirt(int dim)
    {
        return GetChunkManagerVirt(DimensionFromInt(dim));
    }

    protected override IChunkManager GetChunkManagerVirt(string dim)
    {
        AlphaChunkManager rm;
        if (_chunkMgrs.TryGetValue(dim, out rm)) return rm;

        OpenDimension(dim);
        return _chunkMgrs[dim];
    }

    /// <exclude />
    protected override IPlayerManager GetPlayerManagerVirt()
    {
        if (_playerMan != null) return _playerMan;

        var path = IO.Path.Combine(Path, _PLAYER_DIR);

        _playerMan = new PlayerManager(path);
        return _playerMan;
    }

    private string DimensionFromInt(int dim)
    {
        if (dim == Dimension.DEFAULT)
            return "";
        return "DIM" + dim;
    }

    private void OpenDimension(string dim)
    {
        var path = Path;
        if (!string.IsNullOrEmpty(dim)) path = IO.Path.Combine(path, dim);

        if (!IO.Directory.Exists(path)) IO.Directory.CreateDirectory(path);

        var cm = new AlphaChunkManager(path);
        BlockManager bm = new AlphaBlockManager(cm);

        _chunkMgrs[dim] = cm;
        _blockMgrs[dim] = bm;
    }

    private AlphaWorld OpenWorld(string path)
    {
        if (!IO.Directory.Exists(path))
        {
            if (IO.File.Exists(path))
            {
                _levelFile = IO.Path.GetFileName(path);
                path = IO.Path.GetDirectoryName(path);
            }
            else
            {
                throw new IO.DirectoryNotFoundException("Directory '" + path + "' not found");
            }
        }

        Path = path;

        var ldat = IO.Path.Combine(path, _levelFile);
        if (!IO.File.Exists(ldat))
            throw new IO.FileNotFoundException("Data file '" + _levelFile + "' not found in '" + path + "'", ldat);

        if (!LoadLevel()) throw new Exception("Failed to load '" + _levelFile + "'");

        return this;
    }

    private AlphaWorld CreateWorld(string path)
    {
        if (!IO.Directory.Exists(path)) throw new IO.DirectoryNotFoundException("Directory '" + path + "' not found");

        Path = path;
        _level = new Level(this);
        _level.Version = 0;

        return this;
    }

    private bool LoadLevel()
    {
        var nf = new NBTFile(IO.Path.Combine(Path, _levelFile));
        NbtTree tree;

        using (var nbtstr = nf.GetDataInputStream())
        {
            if (nbtstr == null) return false;

            tree = new NbtTree(nbtstr);
        }

        _level = new Level(this);
        _level = _level.LoadTreeSafe(tree.Root);

        return _level != null;
    }


    internal static void OnResolveOpen(object sender, OpenWorldEventArgs e)
    {
        try
        {
            var world = new AlphaWorld().OpenWorld(e.Path);
            if (world == null) return;

            if (world.Level.Version != 0) return;

            e.AddHandler(Open);
        }
        catch (Exception)
        {
        }
    }
}
