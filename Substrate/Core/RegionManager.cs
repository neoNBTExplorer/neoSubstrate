using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Substrate.Core;

/// <summary>
///     Manages the regions of a Beta-compatible world.
/// </summary>
public abstract class RegionManager : IRegionManager
{
    protected Dictionary<RegionKey, IRegion> Cache;

    protected ChunkCache ChunkCache;
    protected string RegionPath;

    /// <summary>
    ///     Creates a new instance of a <see cref="RegionManager" /> for the given region directory and chunk cache.
    /// </summary>
    /// <param name="regionDir">The path to a directory containing region files.</param>
    /// <param name="cache">The shared chunk cache to hold chunk data in.</param>
    public RegionManager(string regionDir, ChunkCache cache)
    {
        RegionPath = regionDir;
        ChunkCache = cache;
        Cache = new Dictionary<RegionKey, IRegion>();
    }

    /// <inherits />
    public IRegion GetRegion(int rx, int rz)
    {
        var k = new RegionKey(rx, rz);
        IRegion r;

        try
        {
            if (!Cache.TryGetValue(k, out r))
            {
                r = CreateRegionCore(rx, rz);
                Cache.Add(k, r);
            }

            return r;
        }
        catch (FileNotFoundException)
        {
            Cache.Add(k, null);
            return null;
        }
    }

    /// <inherits />
    public bool RegionExists(int rx, int rz)
    {
        var r = GetRegion(rx, rz);
        return r != null;
    }

    /// <inherits />
    public IRegion CreateRegion(int rx, int rz)
    {
        var r = GetRegion(rx, rz);
        if (r == null)
        {
            var fp = "r." + rx + "." + rz + ".mca";
            using (var rf = CreateRegionFileCore(rx, rz))
            {
            }

            r = CreateRegionCore(rx, rz);

            var k = new RegionKey(rx, rz);
            Cache[k] = r;
        }

        return r;
    }

    // XXX: Exceptions
    /// <inherits />
    public bool DeleteRegion(int rx, int rz)
    {
        var r = GetRegion(rx, rz);
        if (r == null) return false;

        var k = new RegionKey(rx, rz);
        Cache.Remove(k);

        DeleteRegionCore(r);

        try
        {
            File.Delete(r.GetFilePath());
        }
        catch (Exception e)
        {
            Console.WriteLine("NOTICE: " + e.Message);
            return false;
        }

        return true;
    }

    #region IEnumerable<IRegion> Members

    /// <summary>
    ///     Returns an enumerator that iterates over all of the regions in the underlying dimension.
    /// </summary>
    /// <returns>An enumerator instance.</returns>
    public IEnumerator<IRegion> GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion

    #region IEnumerable Members

    /// <summary>
    ///     Returns an enumerator that iterates over all of the regions in the underlying dimension.
    /// </summary>
    /// <returns>An enumerator instance.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    #endregion


    protected abstract IRegion CreateRegionCore(int rx, int rz);

    protected abstract RegionFile CreateRegionFileCore(int rx, int rz);

    protected abstract void DeleteRegionCore(IRegion region);

    public abstract IRegion GetRegion(string filename);

    /// <summary>
    ///     Get the current region directory path.
    /// </summary>
    /// <returns>The path to the region directory.</returns>
    public string GetRegionPath()
    {
        return RegionPath;
    }


    private struct Enumerator : IEnumerator<IRegion>
    {
        private readonly List<IRegion> _regions;
        private int _pos;

        public Enumerator(RegionManager rm)
        {
            _regions = new List<IRegion>();
            _pos = -1;

            if (!Directory.Exists(rm.GetRegionPath())) throw new DirectoryNotFoundException();

            var files = new List<string>(Directory.GetFiles(rm.GetRegionPath()));
            _regions.Capacity = files.Count;

            files.Sort(RegionSort);

            foreach (var file in files)
                try
                {
                    var r = rm.GetRegion(file);
                    _regions.Add(r);
                }
                catch (ArgumentException)
                {
                }
        }

        public bool MoveNext()
        {
            _pos++;
            return _pos < _regions.Count;
        }

        public void Reset()
        {
            _pos = -1;
        }

        void IDisposable.Dispose()
        {
        }

        object IEnumerator.Current => Current;

        IRegion IEnumerator<IRegion>.Current => Current;

        public IRegion Current
        {
            get
            {
                try
                {
                    return _regions[_pos];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private int RegionSort(string A, string B)
        {
            var R = new Regex(".+r\\.(?<x>-?\\d+)\\.(?<y>-?\\d+)\\.(mca|mcr)", RegexOptions.None);
            var MC = R.Match(A);
            if (!MC.Success)
                return 0;

            var AX = int.Parse(MC.Groups["x"].Value);
            var AZ = int.Parse(MC.Groups["y"].Value);

            MC = R.Match(B);
            if (!MC.Success)
                return 0;

            var BX = int.Parse(MC.Groups["x"].Value);
            var BZ = int.Parse(MC.Groups["y"].Value);

            if (AZ < BZ)
                return -1;
            if (AZ > BZ)
                return 1;
            if (AX < BX)
                return -1;
            if (AX > BX)
                return 1;

            return 0;
        }
    }
}