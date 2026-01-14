using System;
using System.Collections.Generic;
using UnityEngine;

namespace SvoBuilder {
    public class Octree : MonoBehaviour {
        public float voxelSize = 2f;
        public LayerMask geometryLayer;
        
        // for manually setting world bounds
        public Vector3 voxelRegionCenter = Vector3.zero; // where to center the voxel grid
        public Vector3 voxelRegionSize = new Vector3(32, 16, 32); // total dimensions of the grid to voxelize

        public struct SvoNode
        {
            public int layer;
            public int nodeIndex;
            public ulong mortonCode;
            public int firstChildIndex;
            public int[] neighbours;
            public bool hasChildren;
            public bool isLeaf;
            public ulong leafData; // 64-bit voxel grid
        }

        Dictionary<int, List<SvoNode>> svoLayers = new(); // Layer index to list of nodes
        Dictionary<int, Dictionary<ulong, int>> mortonNodeIndexByLayer = new(); // collection of morton codes indexed by layer
        HashSet<ulong> solidVoxels = new(); // morton codes of voxels
        HashSet<ulong> allVoxels = new();

        Vector3 gridOrigin = Vector3.zero; // Set dynamically based on geometry bounds

        private void Awake() {
            GenerateSvo();
        }

        void GenerateSvo() {
            RasterizeGeometry();
            BuildSvoLayers();
            LinkNeighbours();
        }
        void RasterizeGeometry() {
            // Define voxel region manually
            Bounds voxelBounds = new(voxelRegionCenter, voxelRegionSize);
            gridOrigin = voxelBounds.min;
            Vector3 max = voxelBounds.max;

            // filter by layer to not loop over layers unnecessarily  
            Collider[] colliders = Physics.OverlapBox(voxelRegionCenter, voxelRegionSize * 0.5f, Quaternion.identity, geometryLayer);

            for (float x = gridOrigin.x; x < max.x; x += voxelSize)
            {
                for (float y = gridOrigin.y; y < max.y; y += voxelSize)
                {
                    for (float z = gridOrigin.z; z < max.z; z += voxelSize)
                    {
                        Vector3 point = new(x + voxelSize / 2, y + voxelSize / 2, z + voxelSize / 2);

                        bool isSolid = false;

                        // check terrain height seperately
                        if (Terrain.activeTerrain != null &&
                            ((1 << Terrain.activeTerrain.gameObject.layer) & geometryLayer) != 0) {
                            float terrainHeight = Terrain.activeTerrain.SampleHeight(point);
                            if (point.y < terrainHeight) {
                                isSolid = true;
                            }
                        }

                        // Physics.CheckBox to determine solid occupancy for non terrain geometry
                        if (!isSolid) {
                            isSolid = Physics.CheckBox(
                                center: point,
                                halfExtents: Vector3.one * (voxelSize / 2f), // slight safety margin
                                orientation: Quaternion.identity,
                                layerMask: geometryLayer
                                );
                        }

                        Vector3 local = point - gridOrigin;
                        int xi = Mathf.FloorToInt(local.x / voxelSize);
                        int yi = Mathf.FloorToInt(local.y / voxelSize);
                        int zi = Mathf.FloorToInt(local.z / voxelSize);
                        ulong morton = MortonEncoder.EncodeMorton(xi, yi, zi);

                        allVoxels.Add(morton);
                        if (isSolid)
                        {
                            solidVoxels.Add(morton);
                        }
                    }
                }
            }
            Debug.Log($"solid voxels: {solidVoxels.Count}");
        } 

        void BuildSvoLayers() {
            int maxLayer = 4; // depth of the octree

            // prepares one List<SvoNode> per layer, indexed by layer
            for (int layer = 0; layer < maxLayer; layer++) {
                svoLayers[layer] = new List<SvoNode>();
                mortonNodeIndexByLayer[layer] = new Dictionary<ulong, int>();
            }

            // leaf node (layer 0)
            foreach (var morton in allVoxels) {
                var nodeIndex = svoLayers[0].Count;
                SvoNode node = new() {
                    layer = 0,
                    nodeIndex = nodeIndex,
                    mortonCode = morton,
                    isLeaf = true,
                    hasChildren = false,
                    leafData = solidVoxels.Contains(morton) ? ulong.MaxValue : 0UL
                };
                svoLayers[0].Add(node);
                mortonNodeIndexByLayer[0][morton] = nodeIndex;
            }

            // build parent layers
            for (int layer = 1; layer < maxLayer; layer++) {
                var lowerLayer = svoLayers[layer - 1];
                var currentLayer = svoLayers[layer];
                var lowerLookup = mortonNodeIndexByLayer[layer - 1];
                var currentLookup = mortonNodeIndexByLayer[layer];

                HashSet<ulong> parentMortons = new();

                foreach (var node in lowerLayer) {
                    MortonEncoder.DecodeMorton(node.mortonCode, out int x, out int y, out int z);
                    ulong parentMorton = MortonEncoder.EncodeMorton(x >> 1, y >> 1, z >> 1);
                    parentMortons.Add(parentMorton);
                }

                foreach (var pm in parentMortons) {
                    if (!currentLookup.ContainsKey(pm)) {
                        var parentIndex = currentLayer.Count;
                        SvoNode parent = new() {
                            layer = layer,
                            nodeIndex = parentIndex,
                            mortonCode = pm,
                            isLeaf = false,
                            hasChildren = false,
                            leafData = 0 // a leaf node is any node that contains voxels - layer 0
                        };
                        currentLayer.Add(parent);
                        currentLookup[pm] = parentIndex;
                    }
                }
            }

            // assign parent-child links
            for (int layer = 1; layer < maxLayer; layer++) {
                var lowerLayer = svoLayers[layer - 1];
                var currentLookup = mortonNodeIndexByLayer[layer];

                foreach (var child in lowerLayer) {
                    MortonEncoder.DecodeMorton(child.mortonCode, out int x, out int y, out int z);
                    ulong parentMorton = MortonEncoder.EncodeMorton(x >> 1, y >> 1, z >> 1);
                    int parentIndex = currentLookup[parentMorton];
                    var parent = svoLayers[layer][parentIndex];

                    if (!parent.hasChildren) {
                        parent.firstChildIndex = child.nodeIndex; // simplified - assumes compact layout
                        parent.hasChildren = true;
                        svoLayers[layer][parentIndex] = parent;
                    }
                }
            }
        }

        void LinkNeighbours() {
            // Build neighbor links (x, y, z)
            // Populate SvoNode.neighbors based on Morton code adjacency
            for (int layer = 0; layer < svoLayers.Count; layer++) {
                var nodes = svoLayers[layer];
                var lookup = mortonNodeIndexByLayer[layer];

                for (int i = 0; i < nodes.Count; i++) {
                    var node = nodes[i];
                    MortonEncoder.DecodeMorton(node.mortonCode, out int x, out int y, out int z);

                    int[] neighbourIndices = new int[6];
                    (int dx, int dy, int dz)[] directions = new (int, int, int)[] {
                    ( 1,  0,  0), // +X
                    (-1,  0,  0), // -X
                    ( 0,  1,  0), // +Y
                    ( 0, -1,  0), // -Y
                    ( 0,  0,  1), // +Z
                    ( 0,  0, -1), // -Z
                };

                    // finds 6 face-adjacent neighbours 
                    for (int d = 0; d < 6; d++) {
                        var (dx, dy, dz) = directions[d];
                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;

                        ulong neighbourMorton = MortonEncoder.EncodeMorton(nx, ny, nz);
                        neighbourIndices[d] = lookup.TryGetValue(neighbourMorton, out int ni) ? ni : -1;
                    }

                    node.neighbours = neighbourIndices;
                    nodes[i] = node; // save modified node back into list
                }
            }
        }

        //draws manual region
        void OnDrawGizmos() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(voxelRegionCenter, voxelRegionSize);
        }

        //draws all voxels - use this to see individual voxels: CAUTION, might crash editor if too many voxels
        //void OnDrawGizmos() {
        //    if (!svoLayers.ContainsKey(0)) return;

        //    float size = voxelSize;
        //    var nodes = svoLayers[0];

        //    foreach (var node in nodes) {
        //        MortonEncoder.DecodeMorton(node.mortonCode, out int x, out int y, out int z);
        //        Vector3 center = new Vector3(x, y, z) * size + gridOrigin + Vector3.one * (size / 2f);

        //        Gizmos.color = node.leafData == ulong.MaxValue ? Color.green : Color.cyan;
        //        Gizmos.DrawWireCube(center, Vector3.one * size);
        //    }
        //}

        public List<SvoNode> GetLeafNodes() => svoLayers[0];
        public Dictionary<ulong, int> GetLeafNodeLookup() =>
        mortonNodeIndexByLayer.TryGetValue(0, out var lookup) ? lookup : null;
        public Vector3 GridOrigin => this.gridOrigin;
        public float VoxelSize => this.voxelSize;
    }
}



