using System;
using System.Collections;
using System.Collections.Generic;

namespace Substrate.Core;

public class BlockLight
{
    public delegate IBoundedLitBlockCollection NeighborLookupHandler(int relx, int rely, int relz);

    private readonly IBoundedLitBlockCollection _blockset;

    // Maintains internal state of multi-block relighting algorithms
    private readonly BitArray _lightbit;
    private readonly Queue<BlockKey> _update;

    private readonly int _xdim;
    private readonly int _ydim;
    private readonly int _zdim;

    public BlockLight(IBoundedLitBlockCollection blockset)
    {
        _blockset = blockset;

        _xdim = _blockset.XDim;
        _ydim = _blockset.YDim;
        _zdim = _blockset.ZDim;

        _lightbit = new BitArray(_blockset.XDim * 3 * _blockset.ZDim * 3 * _blockset.YDim);
        _update = new Queue<BlockKey>();
    }

    public BlockLight(BlockLight bl)
    {
        _blockset = bl._blockset;

        _xdim = bl._xdim;
        _ydim = bl._ydim;
        _zdim = bl._zdim;

        _lightbit = new BitArray(_blockset.XDim * 3 * _blockset.ZDim * 3 * _blockset.YDim);
        _update = new Queue<BlockKey>();
    }

    public event NeighborLookupHandler ResolveNeighbor;

    public void UpdateBlockLight(int lx, int ly, int lz)
    {
        var primary = new BlockKey(lx, ly, lz);
        _update.Enqueue(primary);

        //BlockInfo info = _blockset.GetInfo(lx, ly, lz);

        //if (info.Luminance > BlockInfo.MIN_LUMINANCE || info.TransmitsLight) {
        if (ly > 0) QueueRelight(new BlockKey(lx, ly - 1, lz));
        if (ly < _ydim - 1) QueueRelight(new BlockKey(lx, ly + 1, lz));

        QueueRelight(new BlockKey(lx - 1, ly, lz));
        QueueRelight(new BlockKey(lx + 1, ly, lz));
        QueueRelight(new BlockKey(lx, ly, lz - 1));
        QueueRelight(new BlockKey(lx, ly, lz + 1));
        //}

        UpdateBlockLight();
    }

    public void UpdateBlockSkyLight(int lx, int ly, int lz)
    {
        var primary = new BlockKey(lx, ly, lz);
        _update.Enqueue(primary);

        UpdateBlockSkyLight();
    }

    public void UpdateHeightMap(int lx, int ly, int lz)
    {
        var info = _blockset.GetInfo(lx, ly, lz);
        var h = Math.Min(ly + 1, _ydim - 1);

        var height = _blockset.GetHeight(lx, lz);
        if (h < height) return;

        if (h == height && !info.ObscuresLight)
        {
            for (var i = ly - 1; i >= 0; i--)
            {
                var info2 = _blockset.GetInfo(lx, i, lz);
                if (info2.ObscuresLight)
                {
                    _blockset.SetHeight(lx, lz, Math.Min(i + 1, _ydim - 1));
                    break;
                }
            }

            UpdateBlockSkyLight(lx, h, lz);
        }
        else if (h > height && info.ObscuresLight)
        {
            _blockset.SetHeight(lx, lz, h);
            UpdateBlockSkyLight(lx, h, lz);
        }
    }


    public void RebuildBlockLight()
    {
        var chunkMap = LocalBlockLightMap();

        // Because the JIT is less intelligent than I hoped
        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        for (var x = 0; x < xdim; x++)
        for (var z = 0; z < zdim; z++)
        for (var y = 0; y < ydim; y++)
        {
            var info = _blockset.GetInfo(x, y, z);
            if (info.Luminance > 0) SpreadBlockLight(chunkMap, x, y, z);
        }
    }

    public void RebuildBlockSkyLight()
    {
        var chunkMap = LocalBlockLightMap();
        var heightMap = LocalHeightMap(chunkMap);

        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        // Optimization - only need to queue at level of highest neighbor's height
        for (var x = 0; x < xdim; x++)
        for (var z = 0; z < zdim; z++)
        {
            var xi = x + xdim;
            var zi = z + zdim;

            var h = heightMap[xi, zi];
            h = Math.Max(h, heightMap[xi, zi - 1]);
            h = Math.Max(h, heightMap[xi - 1, zi]);
            h = Math.Max(h, heightMap[xi + 1, zi]);
            h = Math.Max(h, heightMap[xi, zi + 1]);

            for (var y = h + 1; y < ydim; y++) _blockset.SetSkyLight(x, y, z, BlockInfo.MAX_LUMINANCE);

            //QueueRelight(new BlockKey(x, h, z));
            SpreadSkyLight(chunkMap, heightMap, x, h, z);
        }
    }

    public void RebuildHeightMap()
    {
        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        for (var x = 0; x < xdim; x++)
        for (var z = 0; z < zdim; z++)
        for (var y = ydim - 1; y >= 0; y--)
        {
            var info = _blockset.GetInfo(x, y, z);
            if (info.ObscuresLight)
            {
                _blockset.SetHeight(x, z, Math.Min(y + 1, ydim - 1));
                break;
            }
        }
    }


    public void StitchBlockLight()
    {
        var map = LocalBlockLightMap();

        if (map[1, 0] != null) StitchBlockLight(map[1, 0], BlockCollectionEdge.EAST);
        if (map[0, 1] != null) StitchBlockLight(map[0, 1], BlockCollectionEdge.NORTH);
        if (map[1, 2] != null) StitchBlockLight(map[1, 2], BlockCollectionEdge.WEST);
        if (map[2, 1] != null) StitchBlockLight(map[2, 1], BlockCollectionEdge.SOUTH);
    }

    // TODO: Revise to cache the specified chunk into local map
    public void StitchBlockLight(IBoundedLitBlockCollection chunk, BlockCollectionEdge edge)
    {
        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        if (chunk.XDim != xdim ||
            chunk.YDim != ydim ||
            chunk.ZDim != zdim)
            throw new InvalidOperationException("BlockLight must have same dimensions to be stitched");

        switch (edge)
        {
            case BlockCollectionEdge.EAST:
                for (var x = 0; x < xdim; x++)
                for (var y = 0; y < ydim; y++)
                    TestBlockLight(chunk, x, y, 0, x, y, zdim - 1);

                break;

            case BlockCollectionEdge.NORTH:
                for (var z = 0; z < zdim; z++)
                for (var y = 0; y < ydim; y++)
                    TestBlockLight(chunk, 0, y, z, xdim - 1, y, z);

                break;

            case BlockCollectionEdge.WEST:
                for (var x = 0; x < xdim; x++)
                for (var y = 0; y < ydim; y++)
                    TestBlockLight(chunk, x, y, zdim - 1, x, y, 0);

                break;

            case BlockCollectionEdge.SOUTH:
                for (var z = 0; z < zdim; z++)
                for (var y = 0; y < ydim; y++)
                    TestBlockLight(chunk, xdim - 1, y, z, 0, y, z);

                break;
        }

        UpdateBlockLight();
    }

    public void StitchBlockSkyLight()
    {
        var map = LocalBlockLightMap();

        if (map[1, 0] != null) StitchBlockSkyLight(map[1, 0], BlockCollectionEdge.EAST);
        if (map[0, 1] != null) StitchBlockSkyLight(map[0, 1], BlockCollectionEdge.NORTH);
        if (map[1, 2] != null) StitchBlockSkyLight(map[1, 2], BlockCollectionEdge.WEST);
        if (map[2, 1] != null) StitchBlockSkyLight(map[2, 1], BlockCollectionEdge.SOUTH);
    }

    public void StitchBlockSkyLight(IBoundedLitBlockCollection chunk, BlockCollectionEdge edge)
    {
        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        if (chunk.XDim != xdim ||
            chunk.YDim != ydim ||
            chunk.ZDim != zdim)
            throw new InvalidOperationException("BlockLight must have same dimensions to be stitched");

        switch (edge)
        {
            case BlockCollectionEdge.EAST:
                for (var x = 0; x < xdim; x++)
                for (var y = 0; y < ydim; y++)
                    TestSkyLight(chunk, x, y, 0, x, y, zdim - 1);

                break;

            case BlockCollectionEdge.NORTH:
                for (var z = 0; z < zdim; z++)
                for (var y = 0; y < ydim; y++)
                    TestSkyLight(chunk, 0, y, z, xdim - 1, y, z);

                break;

            case BlockCollectionEdge.WEST:
                for (var x = 0; x < xdim; x++)
                for (var y = 0; y < ydim; y++)
                    TestSkyLight(chunk, x, y, zdim - 1, x, y, 0);

                break;

            case BlockCollectionEdge.SOUTH:
                for (var z = 0; z < zdim; z++)
                for (var y = 0; y < ydim; y++)
                    TestSkyLight(chunk, xdim - 1, y, z, 0, y, z);

                break;
        }

        UpdateBlockSkyLight();
    }


    private void UpdateBlockLight()
    {
        var chunkMap = LocalBlockLightMap();

        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        while (_update.Count > 0)
        {
            var k = _update.Dequeue();
            var index = LightBitmapIndex(k);
            _lightbit[index] = false;

            var xi = k.x + xdim;
            var zi = k.z + zdim;

            var cc = chunkMap[xi / xdim, zi / zdim];
            if (cc == null) continue;

            var lle = NeighborLight(chunkMap, k.x, k.y, k.z - 1);
            var lln = NeighborLight(chunkMap, k.x - 1, k.y, k.z);
            var lls = NeighborLight(chunkMap, k.x, k.y, k.z + 1);
            var llw = NeighborLight(chunkMap, k.x + 1, k.y, k.z);
            var lld = NeighborLight(chunkMap, k.x, k.y - 1, k.z);
            var llu = NeighborLight(chunkMap, k.x, k.y + 1, k.z);

            var x = xi % xdim;
            var y = k.y;
            var z = zi % zdim;

            var lightval = cc.GetBlockLight(x, y, z);
            var info = cc.GetInfo(x, y, z);

            var light = Math.Max(info.Luminance, 0);
            light = Math.Max(light, lle);
            light = Math.Max(light, lln);
            light = Math.Max(light, lls);
            light = Math.Max(light, llw);
            light = Math.Max(light, lld);
            light = Math.Max(light, llu);

            light = Math.Max(light - info.Opacity, 0);

            if (light != lightval)
            {
                //Console.WriteLine("Block Light: ({0},{1},{2}) " + lightval + " -> " + light, k.x, k.y, k.z);

                cc.SetBlockLight(x, y, z, light);

                if (info.TransmitsLight)
                {
                    if (k.y > 0) QueueRelight(new BlockKey(k.x, k.y - 1, k.z));
                    if (k.y < ydim - 1) QueueRelight(new BlockKey(k.x, k.y + 1, k.z));

                    QueueRelight(new BlockKey(k.x - 1, k.y, k.z));
                    QueueRelight(new BlockKey(k.x + 1, k.y, k.z));
                    QueueRelight(new BlockKey(k.x, k.y, k.z - 1));
                    QueueRelight(new BlockKey(k.x, k.y, k.z + 1));
                }
            }
        }
    }

    private void UpdateBlockSkyLight()
    {
        var chunkMap = LocalBlockLightMap();

        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        while (_update.Count > 0)
        {
            var k = _update.Dequeue();
            var index = LightBitmapIndex(k);
            _lightbit[index] = false;

            var xi = k.x + xdim;
            var zi = k.z + zdim;

            var cc = chunkMap[xi / xdim, zi / zdim];
            if (cc == null) continue;

            var x = xi % xdim;
            var y = k.y;
            var z = zi % zdim;

            var lightval = cc.GetSkyLight(x, y, z);
            var info = cc.GetInfo(x, y, z);

            var light = BlockInfo.MIN_LUMINANCE;

            if (cc.GetHeight(x, z) <= y)
            {
                light = BlockInfo.MAX_LUMINANCE;
            }
            else
            {
                var lle = NeighborSkyLight(chunkMap, k.x, k.y, k.z - 1);
                var lln = NeighborSkyLight(chunkMap, k.x - 1, k.y, k.z);
                var lls = NeighborSkyLight(chunkMap, k.x, k.y, k.z + 1);
                var llw = NeighborSkyLight(chunkMap, k.x + 1, k.y, k.z);
                var lld = NeighborSkyLight(chunkMap, k.x, k.y - 1, k.z);
                var llu = NeighborSkyLight(chunkMap, k.x, k.y + 1, k.z);

                light = Math.Max(light, lle);
                light = Math.Max(light, lln);
                light = Math.Max(light, lls);
                light = Math.Max(light, llw);
                light = Math.Max(light, lld);
                light = Math.Max(light, llu);
            }

            light = Math.Max(light - info.Opacity, 0);

            if (light != lightval)
            {
                //Console.WriteLine("Block SkyLight: ({0},{1},{2}) " + lightval + " -> " + light, k.x, k.y, k.z);

                cc.SetSkyLight(x, y, z, light);

                if (info.TransmitsLight)
                {
                    if (k.y > 0) QueueRelight(new BlockKey(k.x, k.y - 1, k.z));
                    if (k.y < ydim - 1) QueueRelight(new BlockKey(k.x, k.y + 1, k.z));

                    QueueRelight(new BlockKey(k.x - 1, k.y, k.z));
                    QueueRelight(new BlockKey(k.x + 1, k.y, k.z));
                    QueueRelight(new BlockKey(k.x, k.y, k.z - 1));
                    QueueRelight(new BlockKey(k.x, k.y, k.z + 1));
                }
            }
        }
    }

    private void SpreadBlockLight(IBoundedLitBlockCollection[,] chunkMap, int lx, int ly, int lz)
    {
        var primary = _blockset.GetInfo(lx, ly, lz);
        var primaryLight = _blockset.GetBlockLight(lx, ly, lz);
        var priLum = Math.Max(primary.Luminance - primary.Opacity, 0);

        if (primaryLight < priLum) _blockset.SetBlockLight(lx, ly, lz, priLum);

        if (primaryLight > primary.Luminance - 1 && !primary.TransmitsLight) return;

        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        var spread = new Queue<LightRecord>();
        if (ly > 0) spread.Enqueue(new LightRecord(lx, ly - 1, lz, primary.Luminance - 1));
        if (ly < ydim - 1) spread.Enqueue(new LightRecord(lx, ly + 1, lz, primary.Luminance - 1));

        spread.Enqueue(new LightRecord(lx - 1, ly, lz, primary.Luminance - 1));
        spread.Enqueue(new LightRecord(lx + 1, ly, lz, primary.Luminance - 1));
        spread.Enqueue(new LightRecord(lx, ly, lz - 1, primary.Luminance - 1));
        spread.Enqueue(new LightRecord(lx, ly, lz + 1, primary.Luminance - 1));

        while (spread.Count > 0)
        {
            var rec = spread.Dequeue();

            var xi = rec.x + xdim;
            var zi = rec.z + zdim;

            var cc = chunkMap[xi / xdim, zi / zdim];
            if (cc == null) continue;

            var x = xi % xdim;
            var y = rec.y;
            var z = zi % zdim;

            var info = cc.GetInfo(x, y, z);
            var light = cc.GetBlockLight(x, y, z);

            var dimStr = Math.Max(rec.str - info.Opacity, 0);

            if (dimStr > light)
            {
                cc.SetBlockLight(x, y, z, dimStr);

                if (info.TransmitsLight)
                {
                    var strength = info.Opacity > 0 ? dimStr : dimStr - 1;

                    if (rec.y > 0) spread.Enqueue(new LightRecord(rec.x, rec.y - 1, rec.z, strength));
                    if (rec.y < ydim - 1) spread.Enqueue(new LightRecord(rec.x, rec.y + 1, rec.z, strength));

                    spread.Enqueue(new LightRecord(rec.x - 1, rec.y, rec.z, strength));
                    spread.Enqueue(new LightRecord(rec.x + 1, rec.y, rec.z, strength));
                    spread.Enqueue(new LightRecord(rec.x, rec.y, rec.z - 1, strength));
                    spread.Enqueue(new LightRecord(rec.x, rec.y, rec.z + 1, strength));
                }
            }
        }
    }

    private void SpreadSkyLight(IBoundedLitBlockCollection[,] chunkMap, int[,] heightMap, int lx, int ly, int lz)
    {
        var primary = _blockset.GetInfo(lx, ly, lz);
        var primaryLight = _blockset.GetSkyLight(lx, ly, lz);
        var priLum = Math.Max(BlockInfo.MAX_LUMINANCE - primary.Opacity, 0);

        if (primaryLight < priLum) _blockset.SetSkyLight(lx, ly, lz, priLum);

        if (primaryLight > BlockInfo.MAX_LUMINANCE - 1 || !primary.TransmitsLight) return;

        var spread = new Queue<LightRecord>();

        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        var lxi = lx + xdim;
        var lzi = lz + zdim;

        var strength = primary.Opacity > 0 ? priLum : priLum - 1;

        if (ly > 0)
        {
            if (heightMap[lxi, lzi] > ly - 1)
                spread.Enqueue(new LightRecord(lx, ly - 1, lz, strength));
            else
                spread.Enqueue(new LightRecord(lx, ly - 1, lz, priLum));
        }

        if (ly < ydim - 1)
            if (heightMap[lxi, lzi] > ly + 1)
                spread.Enqueue(new LightRecord(lx, ly + 1, lz, strength));

        if (heightMap[lxi - 1, lzi] > ly) spread.Enqueue(new LightRecord(lx - 1, ly, lz, strength));
        if (heightMap[lxi + 1, lzi] > ly) spread.Enqueue(new LightRecord(lx + 1, ly, lz, strength));
        if (heightMap[lxi, lzi - 1] > ly) spread.Enqueue(new LightRecord(lx, ly, lz - 1, strength));
        if (heightMap[lxi, lzi + 1] > ly) spread.Enqueue(new LightRecord(lx, ly, lz + 1, strength));

        while (spread.Count > 0)
        {
            var rec = spread.Dequeue();

            var xi = rec.x + xdim;
            var zi = rec.z + zdim;

            var cc = chunkMap[xi / xdim, zi / zdim];
            if (cc == null) continue;

            var x = xi % xdim;
            var y = rec.y;
            var z = zi % zdim;

            var info = cc.GetInfo(x, y, z);
            var light = cc.GetSkyLight(x, y, z);

            var dimStr = Math.Max(rec.str - info.Opacity, 0);

            if (dimStr > light)
            {
                cc.SetSkyLight(x, y, z, dimStr);

                if (info.TransmitsLight)
                {
                    strength = info.Opacity > 0 ? dimStr : dimStr - 1;

                    if (rec.y > 0)
                    {
                        if (heightMap[xi, zi] > rec.y - 1)
                            spread.Enqueue(new LightRecord(rec.x, rec.y - 1, rec.z, strength));
                        else
                            spread.Enqueue(new LightRecord(rec.x, rec.y - 1, rec.z, dimStr));
                    }

                    if (rec.y < ydim - 1)
                        if (heightMap[xi, zi] > rec.y + 1)
                            spread.Enqueue(new LightRecord(rec.x, rec.y + 1, rec.z, strength));

                    if (heightMap[xi - 1, zi] > rec.y)
                        spread.Enqueue(new LightRecord(rec.x - 1, rec.y, rec.z, strength));
                    if (heightMap[xi + 1, zi] > rec.y)
                        spread.Enqueue(new LightRecord(rec.x + 1, rec.y, rec.z, strength));
                    if (heightMap[xi, zi - 1] > rec.y)
                        spread.Enqueue(new LightRecord(rec.x, rec.y, rec.z - 1, strength));
                    if (heightMap[xi, zi + 1] > rec.y)
                        spread.Enqueue(new LightRecord(rec.x, rec.y, rec.z + 1, strength));
                }
            }
        }
    }


    private int LightBitmapIndex(BlockKey key)
    {
        var x = key.x + _xdim;
        var y = key.y;
        var z = key.z + _zdim;

        var zstride = _ydim;
        var xstride = _zdim * 3 * zstride;

        return x * xstride + z * zstride + y;
    }

    private void QueueRelight(BlockKey key)
    {
        if (key.x < -15 || key.x >= 31 || key.z < -15 || key.z >= 31) return;

        var index = LightBitmapIndex(key);

        if (!_lightbit[index])
        {
            _lightbit[index] = true;
            _update.Enqueue(key);
        }
    }


    private IBoundedLitBlockCollection LocalChunk(int lx, int ly, int lz)
    {
        if (ly < 0 || ly >= _ydim) return null;

        if (lx < 0)
        {
            if (lz < 0) return OnResolveNeighbor(-1, 0, -1);

            if (lz >= _zdim) return OnResolveNeighbor(-1, 0, 1);
            return OnResolveNeighbor(-1, 0, 0);
        }

        if (lx >= _xdim)
        {
            if (lz < 0) return OnResolveNeighbor(1, 0, -1);

            if (lz >= _zdim) return OnResolveNeighbor(1, 0, 1);
            return OnResolveNeighbor(1, 0, 0);
        }

        if (lz < 0) return OnResolveNeighbor(0, 0, -1);

        if (lz >= _zdim) return OnResolveNeighbor(0, 0, 1);
        return _blockset;
    }

    private int NeighborLight(IBoundedLitBlockCollection[,] chunkMap, int x, int y, int z)
    {
        if (y < 0 || y >= _ydim) return 0;

        var xdim = _xdim;
        var zdim = _zdim;

        var xi = x + xdim;
        var zi = z + zdim;

        var src = chunkMap[xi / xdim, zi / zdim];
        if (src == null) return 0;

        x = xi % xdim;
        z = zi % zdim;

        var info = src.GetInfo(x, y, z);
        if (!info.TransmitsLight) return info.Luminance;

        var light = src.GetBlockLight(x, y, z);

        return Math.Max(info.Opacity > 0 ? light : light - 1, info.Luminance - 1);
    }

    private int NeighborSkyLight(IBoundedLitBlockCollection[,] chunkMap, int x, int y, int z)
    {
        if (y < 0 || y >= _ydim) return 0;

        var xdim = _xdim;
        var zdim = _zdim;

        var xi = x + xdim;
        var zi = z + zdim;

        var src = chunkMap[xi / xdim, zi / zdim];
        if (src == null) return 0;

        x = xi % xdim;
        z = zi % zdim;

        var info = src.GetInfo(x, y, z);
        if (!info.TransmitsLight) return BlockInfo.MIN_LUMINANCE;

        var light = src.GetSkyLight(x, y, z);

        return info.Opacity > 0 ? light : light - 1;
    }

    private int NeighborHeight(int x, int z)
    {
        var src = LocalChunk(x, 0, z);
        if (src == null) return _ydim - 1;

        x = (x + _xdim * 2) % _xdim;
        z = (z + _zdim * 2) % _zdim;

        return src.GetHeight(x, z);
    }


    private void TestBlockLight(IBoundedLitBlockCollection chunk, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        var light1 = _blockset.GetBlockLight(x1, y1, z1);
        var light2 = chunk.GetBlockLight(x2, y2, z2);
        var lum1 = _blockset.GetInfo(x1, y1, z1).Luminance;
        var lum2 = chunk.GetInfo(x2, y2, z2).Luminance;

        var v1 = Math.Max(light1, lum1);
        var v2 = Math.Max(light2, lum2);
        if (Math.Abs(v1 - v2) > 1) QueueRelight(new BlockKey(x1, y1, z1));
    }

    private void TestSkyLight(IBoundedLitBlockCollection chunk, int x1, int y1, int z1, int x2, int y2, int z2)
    {
        var light1 = _blockset.GetSkyLight(x1, y1, z1);
        var light2 = chunk.GetSkyLight(x2, y2, z2);

        if (Math.Abs(light1 - light2) > 1) QueueRelight(new BlockKey(x1, y1, z1));
    }


    private IBoundedLitBlockCollection[,] LocalBlockLightMap()
    {
        var map = new IBoundedLitBlockCollection[3, 3];

        map[0, 0] = OnResolveNeighbor(-1, 0, -1);
        map[0, 1] = OnResolveNeighbor(-1, 0, 0);
        map[0, 2] = OnResolveNeighbor(-1, 0, 1);
        map[1, 0] = OnResolveNeighbor(0, 0, -1);
        map[1, 1] = _blockset;
        map[1, 2] = OnResolveNeighbor(0, 0, 1);
        map[2, 0] = OnResolveNeighbor(1, 0, -1);
        map[2, 1] = OnResolveNeighbor(1, 0, 0);
        map[2, 2] = OnResolveNeighbor(1, 0, 1);

        return map;
    }

    private int[,] LocalHeightMap(IBoundedLitBlockCollection[,] chunkMap)
    {
        var xdim = _xdim;
        var zdim = _zdim;

        var map = new int[3 * xdim, 3 * zdim];

        for (var xi = 0; xi < 3; xi++)
        {
            var xoff = xi * xdim;
            for (var zi = 0; zi < 3; zi++)
            {
                var zoff = zi * zdim;
                if (chunkMap[xi, zi] == null) continue;

                for (var x = 0; x < xdim; x++)
                {
                    var xx = xoff + x;
                    for (var z = 0; z < zdim; z++)
                    {
                        var zz = zoff + z;
                        map[xx, zz] = chunkMap[xi, zi].GetHeight(x, z);
                    }
                }
            }
        }

        return map;
    }


    private IBoundedLitBlockCollection OnResolveNeighbor(int relX, int relY, int relZ)
    {
        if (ResolveNeighbor != null)
        {
            var n = ResolveNeighbor(relX, relY, relZ);

            if (n == null) return null;

            if (n.XDim != _xdim ||
                n.YDim != _ydim ||
                n.ZDim != _zdim)
                throw new InvalidOperationException("Subscriber returned incompatible ILitBlockCollection");

            return n;
        }

        return null;
    }

    private struct LightRecord
    {
        public readonly int x;
        public readonly int y;
        public readonly int z;
        public readonly int str;

        public LightRecord(int _x, int _y, int _z, int s)
        {
            x = _x;
            y = _y;
            z = _z;
            str = s;
        }
    }
}