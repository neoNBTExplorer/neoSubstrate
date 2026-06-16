using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Substrate.Nbt;

namespace Substrate.Data;

/// <summary>
///     Functions to manage all <see cref="Map" /> data resources.
/// </summary>
/// <remarks>This manager is intended for map files stored in standard compressed NBT format.</remarks>
public class MapManager : IMapManager, IEnumerable<Map>
{
    private static readonly Regex _namePattern = new("^map_[0-9]+\\.dat$");
    private readonly NbtWorld _world;

    /// <summary>
    ///     Create a new <see cref="MapManager" /> for a given world.
    /// </summary>
    /// <param name="world">World containing data files.</param>
    public MapManager(NbtWorld world)
    {
        _world = world;
    }

    #region IEnumerable<Map> Members

    /// <summary>
    ///     Gets an enumerator that iterates through all the maps in the world's data directory.
    /// </summary>
    /// <returns>An enumerator for this manager.</returns>
    public IEnumerator<Map> GetEnumerator()
    {
        var path = Path.Combine(_world.Path, _world.DataDirectory);

        if (!Directory.Exists(path)) throw new DirectoryNotFoundException();

        var files = Directory.GetFiles(path);
        foreach (var file in files)
        {
            var basename = Path.GetFileName(file);

            if (!ParseFileName(basename)) continue;

            var id = MapFile.IdFromFilename(basename);
            yield return GetMap(id);
        }
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    /// <summary>
    ///     Gets a <see cref="MapFile" /> representing the backing NBT data stream.
    /// </summary>
    /// <param name="id">The id of the map to fetch.</param>
    /// <returns>A <see cref="MapFile" /> for the given map.</returns>
    protected MapFile GetMapFile(int id)
    {
        return new MapFile(Path.Combine(_world.Path, _world.DataDirectory), id);
    }

    /// <summary>
    ///     Gets a raw <see cref="NbtTree" /> of data for the given map.
    /// </summary>
    /// <param name="id">The id of the map to fetch.</param>
    /// <returns>An <see cref="NbtTree" /> containing the given map's raw data.</returns>
    /// <exception cref="NbtIOException">Thrown when the manager cannot read in an NBT data stream.</exception>
    public NbtTree GetMapTree(int id)
    {
        var mf = GetMapFile(id);
        using (var nbtstr = mf.GetDataInputStream())
        {
            if (nbtstr == null) throw new NbtIOException("Failed to initialize NBT data stream for input.");

            return new NbtTree(nbtstr);
        }
    }

    /// <summary>
    ///     Saves a raw <see cref="NbtTree" /> representing a map to the given map's file.
    /// </summary>
    /// <param name="id">The id of the map to write data to.</param>
    /// <param name="tree">The map's data as an <see cref="NbtTree" />.</param>
    /// <exception cref="NbtIOException">Thrown when the manager cannot initialize an NBT data stream for output.</exception>
    public void SetMapTree(int id, NbtTree tree)
    {
        var mf = GetMapFile(id);
        using (var zipstr = mf.GetDataOutputStream())
        {
            if (zipstr == null) throw new NbtIOException("Failed to initialize NBT data stream for output.");

            tree.WriteTo(zipstr);
        }
    }

    private bool ParseFileName(string filename)
    {
        var match = _namePattern.Match(filename);
        if (!match.Success) return false;

        return true;
    }

    #region IMapManager Members

    /// <inherit />
    /// <exception cref="DataIOException">Thrown when the manager cannot read in a map that should exist.</exception>
    public Map GetMap(int id)
    {
        if (!MapExists(id)) return null;

        try
        {
            var m = new Map().LoadTreeSafe(GetMapTree(id).Root);
            m.Id = id;
            return m;
        }
        catch (Exception ex)
        {
            var pex = new DataIOException("Could not load map", ex);
            pex.Data["MapId"] = id;
            throw pex;
        }
    }

    /// <inherit />
    /// <exception cref="DataIOException">Thrown when the manager cannot write out the map</exception>
    public void SetMap(int id, Map map)
    {
        try
        {
            SetMapTree(id, new NbtTree(map.BuildTree() as TagNodeCompound));
        }
        catch (Exception ex)
        {
            var pex = new DataIOException("Could not save map", ex);
            pex.Data["MapId"] = id;
            throw pex;
        }
    }

    /// <summary>
    ///     Saves a <see cref="Map" /> object's data back to file given the id set in the <see cref="Map" /> object.
    /// </summary>
    /// <param name="map">The <see cref="Map" /> object containing the data to write back.</param>
    /// <exception cref="DataIOException">Thrown when the manager cannot write out the map</exception>
    public void SetMap(Map map)
    {
        SetMap(map.Id, map);
    }

    /// <inherit />
    public bool MapExists(int id)
    {
        return new MapFile(Path.Combine(_world.Path, _world.DataDirectory), id).Exists();
    }

    /// <inherit />
    /// <exception cref="DataIOException">Thrown when the manager cannot delete the map.</exception>
    public void DeleteMap(int id)
    {
        try
        {
            new MapFile(Path.Combine(_world.Path, _world.DataDirectory), id).Delete();
        }
        catch (Exception ex)
        {
            var pex = new DataIOException("Could not remove map", ex);
            pex.Data["MapId"] = id;
            throw pex;
        }
    }

    #endregion
}
