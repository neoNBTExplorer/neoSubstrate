using System;
using System.IO;
using Substrate.Core;

namespace Substrate;

public class BetaRegionManager : RegionManager
{
    public BetaRegionManager(string regionDir, ChunkCache cache)
        : base(regionDir, cache)
    {
    }

    protected override IRegion CreateRegionCore(int rx, int rz)
    {
        return new BetaRegion(this, ChunkCache, rx, rz);
    }

    protected override RegionFile CreateRegionFileCore(int rx, int rz)
    {
        var fp = "r." + rx + "." + rz + ".mcr";
        return new RegionFile(Path.Combine(RegionPath, fp));
    }

    protected override void DeleteRegionCore(IRegion region)
    {
        var r = region as BetaRegion;
        if (r != null) r.Dispose();
    }

    public override IRegion GetRegion(string filename)
    {
        int rx, rz;
        if (!BetaRegion.ParseFileName(filename, out rx, out rz))
            throw new ArgumentException("Malformed region file name: " + filename, "filename");

        return GetRegion(rx, rz);
    }
}
