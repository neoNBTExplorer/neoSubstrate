using System;
using System.Collections.Generic;

namespace Substrate.Core;
// Rules:
// - Water must be calculated in steps breadth-first
// - If there are any "holes" within 5 steps (manhattan distance) of a water tile, only the edges
//   that can be part of a shortest path to the closest hole(s) are part of the outflow.
// - Any blocks in the water tile's outflow are added to the queue
// - A water source's strength is calculated as strongest inflow - 1.

public class BlockFluid
{
    public delegate IBoundedDataBlockCollection NeighborLookupHandler(int relx, int rely, int relz);

    private readonly IBoundedDataBlockCollection _blockset;

    private readonly Dictionary<ChunkKey, IBoundedDataBlockCollection> _chunks;

    private readonly int _xdim;
    private readonly int _ydim;
    private readonly int _zdim;

    public BlockFluid(IBoundedDataBlockCollection blockset)
    {
        _blockset = blockset;

        _xdim = _blockset.XDim;
        _ydim = _blockset.YDim;
        _zdim = _blockset.ZDim;

        _chunks = new Dictionary<ChunkKey, IBoundedDataBlockCollection>();
    }

    public BlockFluid(BlockFluid bl)
    {
        _blockset = bl._blockset;

        _xdim = bl._xdim;
        _ydim = bl._ydim;
        _zdim = bl._zdim;

        _chunks = new Dictionary<ChunkKey, IBoundedDataBlockCollection>();
    }

    public event NeighborLookupHandler ResolveNeighbor;

    public void ResetWater(IDataArray blocks, IDataArray data)
    {
        for (var i = 0; i < blocks.Length; i++)
            if ((blocks[i] == BlockInfo.StationaryWater.ID || blocks[i] == BlockInfo.Water.ID) && data[i] != 0)
            {
                blocks[i] = (byte)BlockInfo.Air.ID;
                data[i] = 0;
            }
            else if (blocks[i] == BlockInfo.Water.ID)
            {
                blocks[i] = (byte)BlockInfo.StationaryWater.ID;
            }
    }

    public void ResetLava(IDataArray blocks, IDataArray data)
    {
        for (var i = 0; i < blocks.Length; i++)
            if ((blocks[i] == BlockInfo.StationaryLava.ID || blocks[i] == BlockInfo.Lava.ID) && data[i] != 0)
            {
                blocks[i] = (byte)BlockInfo.Air.ID;
                data[i] = 0;
            }
            else if (blocks[i] == BlockInfo.Lava.ID)
            {
                blocks[i] = (byte)BlockInfo.StationaryLava.ID;
            }
    }

    public void UpdateWater(int x, int y, int z)
    {
        DoWater(x, y, z);
        _chunks.Clear();
    }

    public void UpdateLava(int x, int y, int z)
    {
        DoLava(x, y, z);
        _chunks.Clear();
    }

    public void RebuildWater()
    {
        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        var buildQueue = new List<BlockKey>();

        for (var x = 0; x < xdim; x++)
        for (var z = 0; z < zdim; z++)
        for (var y = 0; y < ydim; y++)
        {
            var info = _blockset.GetInfo(x, y, z);
            if (info.ID == BlockInfo.StationaryWater.ID && _blockset.GetData(x, y, z) == 0)
                buildQueue.Add(new BlockKey(x, y, z));
        }

        foreach (var key in buildQueue) DoWater(key.x, key.y, key.z);

        _chunks.Clear();
    }

    public void RebuildLava()
    {
        var xdim = _xdim;
        var ydim = _ydim;
        var zdim = _zdim;

        var buildQueue = new List<BlockKey>();

        for (var x = 0; x < xdim; x++)
        for (var z = 0; z < zdim; z++)
        for (var y = 0; y < ydim; y++)
        {
            var info = _blockset.GetInfo(x, y, z);
            if (info.ID == BlockInfo.StationaryLava.ID && _blockset.GetData(x, y, z) == 0)
                buildQueue.Add(new BlockKey(x, y, z));
        }

        foreach (var key in buildQueue) DoLava(key.x, key.y, key.z);

        _chunks.Clear();
    }

    private BlockCoord TranslateCoord(int x, int y, int z)
    {
        var chunk = GetChunk(x, z);

        var lx = (x % _xdim + _xdim) % _xdim;
        var lz = (z % _zdim + _zdim) % _zdim;

        return new BlockCoord(chunk, lx, y, lz);
    }

    private IBoundedDataBlockCollection GetChunk(int x, int z)
    {
        var cx = x / _xdim + (x >> 31);
        var cz = z / _zdim + (z >> 31);

        var key = new ChunkKey(cx, cz);

        IBoundedDataBlockCollection chunk;
        if (!_chunks.TryGetValue(key, out chunk))
        {
            chunk = OnResolveNeighbor(cx, 0, cz);
            _chunks[key] = chunk;
        }

        return chunk;
    }

    private IBoundedDataBlockCollection OnResolveNeighbor(int relX, int relY, int relZ)
    {
        if (ResolveNeighbor != null)
        {
            var n = ResolveNeighbor(relX, relY, relZ);

            if (n == null) return null;

            if (n.XDim != _xdim ||
                n.YDim != _ydim ||
                n.ZDim != _zdim)
                throw new InvalidOperationException("Subscriber returned incompatible IDataBlockCollection");

            return n;
        }

        return null;
    }

    // -----

    private List<BlockKey> TileOutflow(BlockKey key, int reach = 5)
    {
        var searchQueue = new Queue<BlockKey>();
        var traceQueue = new Queue<KeyValuePair<BlockKey, int>>();
        var markTable = new Dictionary<BlockKey, int>();

        searchQueue.Enqueue(key);
        markTable.Add(key, 0);

        // Identify sinks
        while (searchQueue.Count > 0)
        {
            var branch = searchQueue.Dequeue();
            var distance = markTable[branch];

            // Ignore blocks out of range
            if (distance > reach) continue;

            // Ignore invalid blocks
            var branchHigh = TranslateCoord(branch.x, branch.y, branch.z);
            if (branchHigh.chunk == null || branch.y == 0)
            {
                markTable.Remove(branch);
                continue;
            }

            // If we're not the magical source block...
            if (distance > 0)
            {
                // Ignore blocks that block fluid (and thus could not become a fluid)
                var branchHighInfo = branchHigh.chunk.GetInfo(branchHigh.lx, branchHigh.ly, branchHigh.lz);
                if (branchHighInfo.BlocksFluid)
                {
                    markTable.Remove(branch);
                    continue;
                }
            }

            // If we found a hole, add as a sink, mark the sink distance.
            var branchLow = TranslateCoord(branch.x, branch.y - 1, branch.z);
            var branchLowInfo = branchLow.chunk.GetInfo(branchLow.lx, branchLow.ly, branchLow.lz);
            if (!branchLowInfo.BlocksFluid)
            {
                // If we are our own sink, return the only legal outflow
                if (key == branch)
                {
                    var ret = new List<BlockKey>();
                    ret.Add(new BlockKey(branch.x, branch.y - 1, branch.z));
                    return ret;
                }

                reach = distance;
                traceQueue.Enqueue(new KeyValuePair<BlockKey, int>(branch, distance));
                continue;
            }

            // Expand to neighbors
            if (distance < reach)
            {
                BlockKey[] keys =
                {
                    new(branch.x - 1, branch.y, branch.z),
                    new(branch.x + 1, branch.y, branch.z),
                    new(branch.x, branch.y, branch.z - 1),
                    new(branch.x, branch.y, branch.z + 1)
                };

                for (var i = 0; i < 4; i++)
                    if (!markTable.ContainsKey(keys[i]))
                    {
                        searchQueue.Enqueue(keys[i]);
                        markTable.Add(keys[i], distance + 1);
                    }
            }
        }

        // Candidate outflows are marked
        BlockKey[] neighbors =
        {
            new(key.x - 1, key.y, key.z),
            new(key.x + 1, key.y, key.z),
            new(key.x, key.y, key.z - 1),
            new(key.x, key.y, key.z + 1)
        };

        var outflow = new List<BlockKey>();
        foreach (var n in neighbors)
            if (markTable.ContainsKey(n))
                outflow.Add(n);

        // If there's no sinks, all neighbors are valid outflows
        if (traceQueue.Count == 0) return outflow;

        // Trace back from each sink eliminating shortest path marks
        while (traceQueue.Count > 0)
        {
            var tilekv = traceQueue.Dequeue();
            var tile = tilekv.Key;

            var distance = tilekv.Value;
            markTable.Remove(tile);

            BlockKey[] keys =
            {
                new(tile.x - 1, tile.y, tile.z),
                new(tile.x + 1, tile.y, tile.z),
                new(tile.x, tile.y, tile.z - 1),
                new(tile.x, tile.y, tile.z + 1)
            };

            for (var i = 0; i < 4; i++)
            {
                int nval;
                if (!markTable.TryGetValue(keys[i], out nval)) continue;

                if (nval < distance)
                {
                    markTable.Remove(keys[i]);
                    traceQueue.Enqueue(new KeyValuePair<BlockKey, int>(keys[i], nval));
                }
            }
        }

        // Remove any candidates that are still marked
        foreach (var n in neighbors)
            if (markTable.ContainsKey(n))
                outflow.Remove(n);

        return outflow;
    }

    private int TileInflow(BlockKey key)
    {
        // Check if water is falling on us
        if (key.y < _ydim - 1)
        {
            var up = TranslateCoord(key.x, key.y + 1, key.z);
            var upInfo = up.chunk.GetInfo(up.lx, up.ly, up.lz);

            if (upInfo.State == BlockState.FLUID)
                return up.chunk.GetData(up.lx, up.ly, up.lz) | (int)LiquidState.FALLING;
        }

        // Otherwise return the min inflow of our neighbors + step
        BlockKey[] keys =
        {
            new(key.x - 1, key.y, key.z),
            new(key.x + 1, key.y, key.z),
            new(key.x, key.y, key.z - 1),
            new(key.x, key.y, key.z + 1)
        };

        var minFlow = 16;

        // XXX: Might have different neighboring fluids
        for (var i = 0; i < 4; i++)
        {
            var neighbor = TranslateCoord(keys[i].x, keys[i].y, keys[i].z);
            if (neighbor.chunk == null) continue;

            var neighborInfo = neighbor.chunk.GetInfo(neighbor.lx, neighbor.ly, neighbor.lz);

            if (neighborInfo.State == BlockState.FLUID)
            {
                var flow = neighbor.chunk.GetData(neighbor.lx, neighbor.ly, neighbor.lz);
                var flowFall = (flow & (int)LiquidState.FALLING) != 0;

                if (flowFall)
                {
                    if (keys[i].y == 0) continue;

                    var low = TranslateCoord(keys[i].x, keys[i].y - 1, keys[i].z);
                    var lowinfo = low.chunk.GetInfo(low.lx, low.ly, low.lz);

                    if (lowinfo.BlocksFluid) return 0;
                    continue;
                }

                if (flow < minFlow) minFlow = flow;
            }
        }

        return minFlow;
    }

    private void DoWater(int x, int y, int z)
    {
        var flowQueue = new Queue<BlockKey>();

        var prikey = new BlockKey(x, y, z);
        flowQueue.Enqueue(prikey);

        var outflow = TileOutflow(prikey);
        foreach (var outkey in outflow) flowQueue.Enqueue(outkey);

        while (flowQueue.Count > 0)
        {
            var key = flowQueue.Dequeue();

            var curflow = 16;
            var inflow = TileInflow(key);

            var tile = TranslateCoord(key.x, key.y, key.z);
            var tileInfo = tile.chunk.GetInfo(tile.lx, tile.ly, tile.lz);
            if (tileInfo.ID == BlockInfo.StationaryWater.ID || tileInfo.ID == BlockInfo.Water.ID)
                curflow = tile.chunk.GetData(tile.lx, tile.ly, tile.lz);
            else if (tileInfo.BlocksFluid) continue;

            var curFall = (curflow & (int)LiquidState.FALLING) != 0;
            var inFall = (inflow & (int)LiquidState.FALLING) != 0;

            // We won't update from the following states
            if (curflow == 0 || curflow == inflow || curFall) continue;

            var newflow = curflow;

            // Update from inflow if necessary
            if (inFall)
                newflow = inflow;
            else if (inflow >= 7)
                newflow = 16;
            else
                newflow = inflow + 1;

            // If we haven't changed the flow, don't propagate
            if (newflow == curflow) continue;

            // Update flow, add or remove water tile as necessary
            if (newflow < 16 && curflow == 16)
            {
                // If we're overwriting lava, replace with appropriate stone type and abort propagation
                if (tileInfo.ID == BlockInfo.StationaryLava.ID || tileInfo.ID == BlockInfo.Lava.ID)
                    if ((newflow & (int)LiquidState.FALLING) != 0)
                    {
                        var odata = tile.chunk.GetData(tile.lx, tile.ly, tile.lz);
                        if (odata == 0)
                            tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.Obsidian.ID);
                        else
                            tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.Cobblestone.ID);
                        tile.chunk.SetData(tile.lx, tile.ly, tile.lz, 0);
                        continue;
                    }

                // Otherwise replace the tile with our water flow
                tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.StationaryWater.ID);
                tile.chunk.SetData(tile.lx, tile.ly, tile.lz, newflow);
            }
            else if (newflow == 16)
            {
                tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.Air.ID);
                tile.chunk.SetData(tile.lx, tile.ly, tile.lz, 0);
            }
            else
            {
                tile.chunk.SetData(tile.lx, tile.ly, tile.lz, newflow);
            }

            // Process outflows
            outflow = TileOutflow(key);

            foreach (var nkey in outflow) flowQueue.Enqueue(nkey);
        }
    }

    private void DoLava(int x, int y, int z)
    {
        var flowQueue = new Queue<BlockKey>();

        var prikey = new BlockKey(x, y, z);
        flowQueue.Enqueue(prikey);

        var outflow = TileOutflow(prikey);
        foreach (var outkey in outflow) flowQueue.Enqueue(outkey);

        while (flowQueue.Count > 0)
        {
            var key = flowQueue.Dequeue();

            var curflow = 16;
            var inflow = TileInflow(key);

            var tile = TranslateCoord(key.x, key.y, key.z);
            var tileInfo = tile.chunk.GetInfo(tile.lx, tile.ly, tile.lz);
            if (tileInfo.ID == BlockInfo.StationaryLava.ID || tileInfo.ID == BlockInfo.Lava.ID)
                curflow = tile.chunk.GetData(tile.lx, tile.ly, tile.lz);
            else if (tileInfo.BlocksFluid) continue;

            var curFall = (curflow & (int)LiquidState.FALLING) != 0;
            var inFall = (inflow & (int)LiquidState.FALLING) != 0;

            // We won't update from the following states
            if (curflow == 0 || curflow == inflow || curFall) continue;

            var newflow = curflow;

            // Update from inflow if necessary
            if (inFall)
                newflow = inflow;
            else if (inflow >= 6)
                newflow = 16;
            else
                newflow = inflow + 2;

            // If we haven't changed the flow, don't propagate
            if (newflow == curflow) continue;

            // Update flow, add or remove lava tile as necessary
            if (newflow < 16 && curflow == 16)
            {
                // If we're overwriting water, replace with appropriate stone type and abort propagation
                if (tileInfo.ID == BlockInfo.StationaryWater.ID || tileInfo.ID == BlockInfo.Water.ID)
                    if ((newflow & (int)LiquidState.FALLING) == 0)
                    {
                        tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.Cobblestone.ID);
                        tile.chunk.SetData(tile.lx, tile.ly, tile.lz, 0);
                        continue;
                    }

                tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.StationaryLava.ID);
                tile.chunk.SetData(tile.lx, tile.ly, tile.lz, newflow);
            }
            else if (newflow == 16)
            {
                tile.chunk.SetID(tile.lx, tile.ly, tile.lz, BlockInfo.Air.ID);
                tile.chunk.SetData(tile.lx, tile.ly, tile.lz, 0);
            }
            else
            {
                tile.chunk.SetData(tile.lx, tile.ly, tile.lz, newflow);
            }

            // Process outflows
            outflow = TileOutflow(key);

            foreach (var nkey in outflow) flowQueue.Enqueue(nkey);
        }
    }

    internal class BlockCoord
    {
        internal IBoundedDataBlockCollection chunk;
        internal int lx;
        internal int ly;
        internal int lz;

        internal BlockCoord(IBoundedDataBlockCollection _chunk, int _lx, int _ly, int _lz)
        {
            chunk = _chunk;
            lx = _lx;
            ly = _ly;
            lz = _lz;
        }
    }
}