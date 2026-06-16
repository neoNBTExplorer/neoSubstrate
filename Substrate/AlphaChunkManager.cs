using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Substrate.Core;
using Substrate.Nbt;

namespace Substrate;

/// <summary>
///     Represents an Alpha-compatible interface for globally managing chunks.
/// </summary>
public class AlphaChunkManager : IChunkManager, IEnumerable<ChunkRef>
{
    //protected Dictionary<ChunkKey, WeakReference> _cache;
    private readonly LRUCache<ChunkKey, ChunkRef> _cache;
    private readonly Dictionary<ChunkKey, ChunkRef> _dirty;

    /// <summary>
    ///     Creates a new <see cref="AlphaChunkManager" /> instance for the give chunk base directory.
    /// </summary>
    /// <param name="mapDir">The path to the chunk base directory.</param>
    public AlphaChunkManager(string mapDir)
    {
        ChunkPath = mapDir;
        _cache = new LRUCache<ChunkKey, ChunkRef>(256);
        _dirty = new Dictionary<ChunkKey, ChunkRef>();
    }

    /// <summary>
    ///     Gets the path to the base directory containing the chunk directory structure.
    /// </summary>
    public string ChunkPath { get; }

    #region IEnumerable<ChunkRef> Members

    /// <summary>
    ///     Gets an enumerator that iterates through all the chunks in the world.
    /// </summary>
    /// <returns>An enumerator for this manager.</returns>
    public IEnumerator<ChunkRef> GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    private ChunkFile GetChunkFile(int cx, int cz)
    {
        return new ChunkFile(ChunkPath, cx, cz);
    }

    private NbtTree GetChunkTree(int cx, int cz)
    {
        var cf = GetChunkFile(cx, cz);
        using (var nbtstr = cf.GetDataInputStream())
        {
            if (nbtstr == null) return null;

            return new NbtTree(nbtstr);
        }
    }

    private bool SaveChunkTree(int cx, int cz, NbtTree tree)
    {
        var cf = GetChunkFile(cx, cz);
        using (var zipstr = cf.GetDataOutputStream())
        {
            if (zipstr == null) return false;

            tree.WriteTo(zipstr);
        }

        return true;
    }

    private Stream GetChunkOutStream(int cx, int cz)
    {
        return new ChunkFile(ChunkPath, cx, cz).GetDataOutputStream();
    }

    /// <summary>
    ///     Gets the (last modified) timestamp of the underlying chunk file.
    /// </summary>
    /// <param name="cx">The global X-coordinate of a chunk.</param>
    /// <param name="cz">The global Z-coordinate of a chunk.</param>
    /// <returns>The last modified timestamp of the underlying chunk file.</returns>
    public int GetChunkTimestamp(int cx, int cz)
    {
        var cf = GetChunkFile(cx, cz);
        if (cf == null) return 0;

        return cf.GetModifiedTime();
    }


    private class Enumerator : IEnumerator<ChunkRef>
    {
        protected static readonly Regex _namePattern = new("c\\.(-?[0-9a-zA-Z]+)\\.(-?[0-9a-zA-Z]+)\\.dat$");
        protected readonly AlphaChunkManager _cm;
        protected Queue<ChunkRef> _chunks;
        private ChunkRef _curchunk;
        private string _cursld;

        private string _curtld;
        protected Queue<string> _sld;
        protected Queue<string> _tld;

        public Enumerator(AlphaChunkManager cfm)
        {
            _cm = cfm;

            if (!Directory.Exists(_cm.ChunkPath)) throw new DirectoryNotFoundException();

            Reset();
        }

        public ChunkRef Current
        {
            get
            {
                if (_curchunk == null) throw new InvalidOperationException();
                return _curchunk;
            }
        }

        public bool MoveNext()
        {
            while (_chunks.Count == 0)
                if (!MoveNextSLD())
                    return false;

            _curchunk = _chunks.Dequeue();
            return true;
        }

        public void Reset()
        {
            _curchunk = null;

            _tld = new Queue<string>();
            _sld = new Queue<string>();
            _chunks = new Queue<ChunkRef>();

            var files = Directory.GetDirectories(_cm.ChunkPath);
            foreach (var file in files) _tld.Enqueue(file);
        }

        void IDisposable.Dispose()
        {
        }

        object IEnumerator.Current => Current;

        ChunkRef IEnumerator<ChunkRef>.Current => Current;

        private bool MoveNextTLD()
        {
            if (_tld.Count == 0) return false;

            _curtld = _tld.Dequeue();

            //string path = Path.Combine(_cm.ChunkPath, _curtld);

            var files = Directory.GetDirectories(_curtld);
            foreach (var file in files) _sld.Enqueue(file);

            return true;
        }

        public bool MoveNextSLD()
        {
            while (_sld.Count == 0)
                if (!MoveNextTLD())
                    return false;

            _cursld = _sld.Dequeue();

            //string path = Path.Combine(_cm.ChunkPath, _curtld);
            //path = Path.Combine(path, _cursld);

            var files = Directory.GetFiles(_cursld);
            foreach (var file in files)
            {
                int x;
                int z;

                var basename = Path.GetFileName(file);

                if (!ParseFileName(basename, out x, out z)) continue;

                var cref = _cm.GetChunkRef(x, z);
                if (cref != null) _chunks.Enqueue(cref);
            }

            return true;
        }

        private bool ParseFileName(string filename, out int x, out int z)
        {
            x = 0;
            z = 0;

            var match = _namePattern.Match(filename);
            if (!match.Success) return false;

            x = (int)Base36.Decode(match.Groups[1].Value);
            z = (int)Base36.Decode(match.Groups[2].Value);
            return true;
        }
    }

    #region IChunkContainer Members

    /// <inheritdoc />
    public int ChunkGlobalX(int cx)
    {
        return cx;
    }

    /// <inheritdoc />
    public int ChunkGlobalZ(int cz)
    {
        return cz;
    }

    /// <inheritdoc />
    public int ChunkLocalX(int cx)
    {
        return cx;
    }

    /// <inheritdoc />
    public int ChunkLocalZ(int cz)
    {
        return cz;
    }

    /// <inheritdoc />
    public IChunk GetChunk(int cx, int cz)
    {
        if (!ChunkExists(cx, cz)) return null;

        return AlphaChunk.CreateVerified(GetChunkTree(cx, cz));
    }

    /// <inheritdoc />
    public ChunkRef GetChunkRef(int cx, int cz)
    {
        var k = new ChunkKey(cx, cz);

        ChunkRef c = null;

        //WeakReference chunkref = null;
        if (_cache.TryGetValue(k, out c)) return c;

        c = ChunkRef.Create(this, cx, cz);
        if (c != null) _cache[k] = c;

        return c;
    }

    /// <inheritdoc />
    public ChunkRef CreateChunk(int cx, int cz)
    {
        DeleteChunk(cx, cz);
        var chunk = AlphaChunk.Create(cx, cz);

        using (var chunkOutStream = GetChunkOutStream(cx, cz))
        {
            chunk.Save(chunkOutStream);
        }

        var cr = ChunkRef.Create(this, cx, cz);
        var k = new ChunkKey(cx, cz);
        _cache[k] = cr;

        return cr;
    }

    /// <inheritdoc />
    public bool ChunkExists(int cx, int cz)
    {
        return new ChunkFile(ChunkPath, cx, cz).Exists();
    }

    /// <inheritdoc />
    public bool DeleteChunk(int cx, int cz)
    {
        new ChunkFile(ChunkPath, cx, cz).Delete();

        var k = new ChunkKey(cx, cz);
        _cache.Remove(k);
        _dirty.Remove(k);

        return true;
    }

    /// <inheritdoc />
    public ChunkRef SetChunk(int cx, int cz, IChunk chunk)
    {
        DeleteChunk(cx, cz);
        chunk.SetLocation(cx, cz);
        using (var chunkOutStream = GetChunkOutStream(cx, cz))
        {
            chunk.Save(chunkOutStream);
        }

        var cr = ChunkRef.Create(this, cx, cz);
        var k = new ChunkKey(cx, cz);
        _cache[k] = cr;

        return cr;
    }

    /// <inheritdoc />
    public int Save()
    {
        foreach (var e in _cache)
            if (e.Value.IsDirty)
                _dirty[e.Key] = e.Value;

        var saved = 0;
        foreach (var chunkRef in _dirty.Values)
        {
            var cx = ChunkGlobalX(chunkRef.X);
            var cz = ChunkGlobalZ(chunkRef.Z);

            using (var chunkOutStream = GetChunkOutStream(cx, cz))
            {
                if (chunkRef.Save(chunkOutStream)) saved++;
            }
        }

        _dirty.Clear();
        return saved;
    }

    /// <inheritdoc />
    public bool SaveChunk(IChunk chunk)
    {
        using (var chunkOutStream = GetChunkOutStream(ChunkGlobalX(chunk.X), ChunkGlobalZ(chunk.Z)))
        {
            if (chunk.Save(chunkOutStream))
            {
                _dirty.Remove(new ChunkKey(chunk.X, chunk.Z));
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool CanDelegateCoordinates => true;

    #endregion
}
