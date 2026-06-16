using System;
using System.IO;
using System.Text.RegularExpressions;
using Substrate.Nbt;

namespace Substrate.Core;

/// <summary>
///     Represents a single region containing 32x32 chunks.
/// </summary>
public abstract class Region : IDisposable, IRegion
{
    protected const int Kxdim = 32;
    protected const int Kzdim = 32;
    protected const int Kxmask = Kxdim - 1;
    protected const int Kzmask = Kzdim - 1;
    protected const int Kxlog = 5;
    protected const int Kzlog = 5;

    private static Regex _namePattern = new("r\\.(-?[0-9]+)\\.(-?[0-9]+)\\.mca$");

    private readonly WeakReference _regionFile;
    private bool _disposed;

    protected ChunkCache Cache;

    protected RegionManager RegionMan;

    protected int Rx;
    protected int Rz;

    /// <summary>
    ///     Creates an instance of a <see cref="Region" /> for a given set of coordinates.
    /// </summary>
    /// <param name="rm">The <see cref="RegionManager" /> that should be managing this region.</param>
    /// <param name="cache">A shared cache for holding chunks.</param>
    /// <param name="rx">The global X-coordinate of the region.</param>
    /// <param name="rz">The global Z-coordinate of the region.</param>
    /// <remarks>
    ///     <para>
    ///         The constructor will not actually open or parse any region files.  Given just the region coordinates, the
    ///         region will be able to determien the correct region file to look for based on the naming pattern for regions:
    ///         r.x.z.mcr, given x and z are integers representing the region's coordinates.
    ///     </para>
    ///     <para>
    ///         Regions require a <see cref="ChunkCache" /> to be provided because they do not actually store any chunks or
    ///         references
    ///         to chunks on their own.  This allows regions to easily pass off requests outside of their bounds, if necessary.
    ///     </para>
    /// </remarks>
    public Region(RegionManager rm, ChunkCache cache, int rx, int rz)
    {
        RegionMan = rm;
        Cache = cache;
        _regionFile = new WeakReference(null);
        Rx = rx;
        Rz = rz;

        if (!File.Exists(GetFilePath())) throw new FileNotFoundException();
    }

    /// <summary>
    ///     Creates an instance of a <see cref="Region" /> for the given region file.
    /// </summary>
    /// <param name="rm">The <see cref="RegionManager" /> that should be managing this region.</param>
    /// <param name="cache">A shared cache for holding chunks.</param>
    /// <param name="filename">The region file to derive the region from.</param>
    /// <remarks>
    ///     <para>
    ///         The constructor will not actually open or parse the region file.  It will only read the file's name in order
    ///         to derive the region's coordinates, based on a strict naming pattern for regions: r.x.z.mcr, given x and z are
    ///         integers
    ///         representing the region's coordinates.
    ///     </para>
    ///     <para>
    ///         Regions require a <see cref="ChunkCache" /> to be provided because they do not actually store any chunks or
    ///         references
    ///         to chunks on their own.  This allows regions to easily pass off requests outside of their bounds, if necessary.
    ///     </para>
    /// </remarks>
    public Region(RegionManager rm, ChunkCache cache, string filename)
    {
        RegionMan = rm;
        Cache = cache;
        _regionFile = new WeakReference(null);

        ParseFileNameCore(filename, out Rx, out Rz);

        if (!File.Exists(Path.Combine(RegionMan.GetRegionPath(), filename))) throw new FileNotFoundException();
    }

    /// <summary>
    ///     Gets the length of the X-dimension of the region in chunks.
    /// </summary>
    public int XDim => Kxdim;

    /// <summary>
    ///     Gets the length of the Z-dimension of the region in chunks.
    /// </summary>
    public int ZDim => Kzdim;

    /// <summary>
    ///     Disposes any managed and unmanaged resources held by the region.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inherit />
    public int X => Rx;

    /// <inherit />
    public int Z => Rz;

    public abstract string GetFileName();

    public abstract string GetFilePath();

    /// <inherits />
    public NbtTree GetChunkTree(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? null : alt.GetChunkTree(ForeignX(lcx), ForeignZ(lcz));
        }

        var rf = GetRegionFile();
        NbtTree tree;

        using (var nbtstr = rf.GetChunkDataInputStream(lcx, lcz))
        {
            if (nbtstr == null) return null;

            tree = new NbtTree(nbtstr);
        }

        return tree;
    }

    // XXX: Exceptions
    /// <inherits />
    public bool SaveChunkTree(int lcx, int lcz, NbtTree tree)
    {
        return SaveChunkTree(lcx, lcz, tree, null);
    }

    /// <inherits />
    public bool SaveChunkTree(int lcx, int lcz, NbtTree tree, int timestamp)
    {
        return SaveChunkTree(lcx, lcz, tree, timestamp);
    }

    /// <inherits />
    public Stream GetChunkOutStream(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? null : alt.GetChunkOutStream(ForeignX(lcx), ForeignZ(lcz));
        }

        var rf = GetRegionFile();
        return rf.GetChunkDataOutputStream(lcx, lcz);
    }

    /// <inherits />
    public int ChunkCount()
    {
        var rf = GetRegionFile();

        var count = 0;
        for (var x = 0; x < Kxdim; x++)
        for (var z = 0; z < Kzdim; z++)
            if (rf.HasChunk(x, z))
                count++;

        return count;
    }

    // XXX: Consider revising foreign lookup support
    /// <inherits />
    public ChunkRef GetChunkRef(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? null : alt.GetChunkRef(ForeignX(lcx), ForeignZ(lcz));
        }

        var cx = lcx + Rx * Kxdim;
        var cz = lcz + Rz * Kzdim;

        var k = new ChunkKey(cx, cz);
        var c = Cache.Fetch(k);
        if (c != null) return c;

        c = ChunkRef.Create(this, lcx, lcz);
        if (c != null) Cache.Insert(c);

        return c;
    }

    /// <inherits />
    public ChunkRef CreateChunk(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? null : alt.CreateChunk(ForeignX(lcx), ForeignZ(lcz));
        }

        DeleteChunk(lcx, lcz);

        var cx = lcx + Rx * Kxdim;
        var cz = lcz + Rz * Kzdim;

        var c = CreateChunkCore(cx, cz);
        using (var chunkOutStream = GetChunkOutStream(lcx, lcz))
        {
            c.Save(chunkOutStream);
        }

        var cr = ChunkRef.Create(this, lcx, lcz);
        Cache.Insert(cr);

        return cr;
    }

    protected abstract IChunk CreateChunkCore(int cx, int cz);

    protected abstract IChunk CreateChunkVerifiedCore(NbtTree tree);

    protected abstract bool ParseFileNameCore(string filename, out int x, out int z);

    /// <summary>
    ///     Region finalizer that ensures any resources are cleaned up
    /// </summary>
    ~Region()
    {
        Dispose(false);
    }

    /// <summary>
    ///     Conditionally dispose managed or unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if the call to Dispose was explicit.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
            if (disposing)
            {
                // Cleanup managed resources
                var rf = _regionFile.Target as RegionFile;
                if (rf != null)
                {
                    rf.Dispose();
                    rf = null;
                }
            }

        // Cleanup unmanaged resources
        _disposed = true;
    }

    private RegionFile GetRegionFile()
    {
        var rf = _regionFile.Target as RegionFile;
        if (rf == null)
        {
            rf = new RegionFile(GetFilePath());
            _regionFile.Target = rf;
        }

        return rf;
    }

    private bool SaveChunkTree(int lcx, int lcz, NbtTree tree, int? timestamp)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? false : alt.SaveChunkTree(ForeignX(lcx), ForeignZ(lcz), tree);
        }

        var rf = GetRegionFile();
        using (var zipstr = timestamp == null
                   ? rf.GetChunkDataOutputStream(lcx, lcz)
                   : rf.GetChunkDataOutputStream(lcx, lcz, (int)timestamp))
        {
            if (zipstr == null) return false;

            tree.WriteTo(zipstr);
        }

        return true;
    }

    protected bool LocalBoundsCheck(int lcx, int lcz)
    {
        return lcx >= 0 && lcx < Kxdim && lcz >= 0 && lcz < Kzdim;
    }

    protected IRegion GetForeignRegion(int lcx, int lcz)
    {
        return RegionMan.GetRegion(Rx + (lcx >> Kxlog), Rz + (lcz >> Kzlog));
    }

    protected int ForeignX(int lcx)
    {
        return (lcx + Kxdim * 10000) & Kxmask;
    }

    protected int ForeignZ(int lcz)
    {
        return (lcz + Kzdim * 10000) & Kzmask;
    }


    #region IChunkCollection Members

    // XXX: This also feels dirty.
    /// <summary>
    ///     Gets the global X-coordinate of a chunk given an internal coordinate handed out by a <see cref="Region" />
    ///     container.
    /// </summary>
    /// <param name="cx">
    ///     An internal X-coordinate given to a <see cref="ChunkRef" /> by any instance of a <see cref="Region" />
    ///     container.
    /// </param>
    /// <returns>The global X-coordinate of the corresponding chunk.</returns>
    public int ChunkGlobalX(int cx)
    {
        return Rx * Kxdim + cx;
    }

    /// <summary>
    ///     Gets the global Z-coordinate of a chunk given an internal coordinate handed out by a <see cref="Region" />
    ///     container.
    /// </summary>
    /// <param name="cz">
    ///     An internal Z-coordinate given to a <see cref="ChunkRef" /> by any instance of a <see cref="Region" />
    ///     container.
    /// </param>
    /// <returns>The global Z-coordinate of the corresponding chunk.</returns>
    public int ChunkGlobalZ(int cz)
    {
        return Rz * Kzdim + cz;
    }

    /// <summary>
    ///     Gets the region-local X-coordinate of a chunk given an internal coordinate handed out by a <see cref="Region" />
    ///     container.
    /// </summary>
    /// <param name="cx">
    ///     An internal X-coordinate given to a <see cref="ChunkRef" /> by any instance of a <see cref="Region" />
    ///     container.
    /// </param>
    /// <returns>The region-local X-coordinate of the corresponding chunk.</returns>
    public int ChunkLocalX(int cx)
    {
        return cx;
    }

    /// <summary>
    ///     Gets the region-local Z-coordinate of a chunk given an internal coordinate handed out by a <see cref="Region" />
    ///     container.
    /// </summary>
    /// <param name="cz">
    ///     An internal Z-coordinate given to a <see cref="ChunkRef" /> by any instance of a <see cref="Region" />
    ///     container.
    /// </param>
    /// <returns>The region-local Z-coordinate of the corresponding chunk.</returns>
    public int ChunkLocalZ(int cz)
    {
        return cz;
    }

    /// <summary>
    ///     Returns a <see cref="IChunk" /> given local coordinates relative to this region.
    /// </summary>
    /// <param name="lcx">The local X-coordinate of a chunk relative to this region.</param>
    /// <param name="lcz">The local Z-coordinate of a chunk relative to this region.</param>
    /// <returns>A <see cref="IChunk" /> object for the given coordinates, or null if the chunk does not exist.</returns>
    /// <remarks>
    ///     If the local coordinates are out of bounds for this region, the action will be forwarded to the correct region
    ///     transparently.  The returned <see cref="IChunk" /> object may either come from cache, or be regenerated from disk.
    /// </remarks>
    public IChunk GetChunk(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? null : alt.GetChunk(ForeignX(lcx), ForeignZ(lcz));
        }

        if (!ChunkExists(lcx, lcz)) return null;

        return CreateChunkVerifiedCore(GetChunkTree(lcx, lcz));
    }

    /// <summary>
    ///     Checks if a chunk exists at the given local coordinates relative to this region.
    /// </summary>
    /// <param name="lcx">The local X-coordinate of a chunk relative to this region.</param>
    /// <param name="lcz">The local Z-coordinate of a chunk relative to this region.</param>
    /// <returns>True if there is a chunk at the given coordinates; false otherwise.</returns>
    /// <remarks>
    ///     If the local coordinates are out of bounds for this region, the action will be forwarded to the correct region
    ///     transparently.
    /// </remarks>
    public bool ChunkExists(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? false : alt.ChunkExists(ForeignX(lcx), ForeignZ(lcz));
        }

        var rf = GetRegionFile();
        return rf.HasChunk(lcx, lcz);
    }

    /// <summary>
    ///     Deletes a chunk from the underlying data store at the given local coordinates relative to this region.
    /// </summary>
    /// <param name="lcx">The local X-coordinate of a chunk relative to this region.</param>
    /// <param name="lcz">The local Z-coordinate of a chunk relative to this region.</param>
    /// <returns>True if there is a chunk was deleted; false otherwise.</returns>
    /// <remarks>
    ///     If the local coordinates are out of bounds for this region, the action will be forwarded to the correct region
    ///     transparently.
    /// </remarks>
    public bool DeleteChunk(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? false : alt.DeleteChunk(ForeignX(lcx), ForeignZ(lcz));
        }

        var rf = GetRegionFile();
        if (!rf.HasChunk(lcx, lcz)) return false;

        rf.DeleteChunk(lcx, lcz);

        var k = new ChunkKey(ChunkGlobalX(lcx), ChunkGlobalZ(lcz));
        Cache.Remove(k);

        if (ChunkCount() == 0)
        {
            RegionMan.DeleteRegion(X, Z);
            _regionFile.Target = null;
        }

        return true;
    }

    /// <summary>
    ///     Saves an existing <see cref="IChunk" /> to the region at the given local coordinates.
    /// </summary>
    /// <param name="lcx">The local X-coordinate of a chunk relative to this region.</param>
    /// <param name="lcz">The local Z-coordinate of a chunk relative to this region.</param>
    /// <param name="chunk">A <see cref="IChunk" /> to save to the given location.</param>
    /// <returns>A <see cref="ChunkRef" /> represneting the <see cref="IChunk" /> at its new location.</returns>
    /// <remarks>
    ///     If the local coordinates are out of bounds for this region, the action will be forwarded to the correct region
    ///     transparently.  The <see cref="IChunk" />'s internal global coordinates will be updated to reflect the new
    ///     location.
    /// </remarks>
    public ChunkRef SetChunk(int lcx, int lcz, IChunk chunk)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? null : alt.CreateChunk(ForeignX(lcx), ForeignZ(lcz));
        }

        DeleteChunk(lcx, lcz);

        var cx = lcx + Rx * Kxdim;
        var cz = lcz + Rz * Kzdim;

        chunk.SetLocation(cx, cz);
        using (var chunkOutStream = GetChunkOutStream(lcx, lcz))
        {
            chunk.Save(chunkOutStream);
        }

        var cr = ChunkRef.Create(this, lcx, lcz);
        Cache.Insert(cr);

        return cr;
    }

    /// <summary>
    ///     Saves all chunks within this region that have been marked as dirty.
    /// </summary>
    /// <returns>The number of chunks that were saved.</returns>
    public int Save()
    {
        Cache.SyncDirty();

        var saved = 0;
        var en = Cache.GetDirtyEnumerator();
        while (en.MoveNext())
        {
            var chunk = en.Current;

            if (!ChunkExists(chunk.LocalX, chunk.LocalZ)) throw new MissingChunkException();
            using (var chunkOutStream = GetChunkOutStream(chunk.LocalX, chunk.LocalZ))
            {
                if (chunk.Save(chunkOutStream)) saved++;
            }
        }

        Cache.ClearDirty();
        return saved;
    }

    // XXX: Allows a chunk not part of this region to be saved to it
    /// <exclude />
    public bool SaveChunk(IChunk chunk)
    {
        using (var chunkOutStream = GetChunkOutStream(ForeignX(chunk.X), ForeignZ(chunk.Z)))
        {
            //Console.WriteLine("Region[{0}, {1}].Save({2}, {3})", _rx, _rz, ForeignX(chunk.X),ForeignZ(chunk.Z));
            return chunk.Save(chunkOutStream);
        }
    }

    /// <summary>
    ///     Checks if this container supports delegating an action on out-of-bounds coordinates to another container.
    /// </summary>
    public bool CanDelegateCoordinates => true;

    /// <inherits />
    public int GetChunkTimestamp(int lcx, int lcz)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            return alt == null ? 0 : alt.GetChunkTimestamp(ForeignX(lcx), ForeignZ(lcz));
        }

        var rf = GetRegionFile();
        return rf.GetTimestamp(lcx, lcz);
    }

    /// <inherits />
    public void SetChunkTimestamp(int lcx, int lcz, int timestamp)
    {
        if (!LocalBoundsCheck(lcx, lcz))
        {
            var alt = GetForeignRegion(lcx, lcz);
            if (alt != null)
                alt.SetChunkTimestamp(ForeignX(lcx), ForeignZ(lcz), timestamp);
        }

        var rf = GetRegionFile();
        rf.SetTimestamp(lcx, lcz, timestamp);
    }

    #endregion
}