# unity-SVO
unity implementation of SVO and Morton encoder

The SVO is based on this paper [3D flight navigation using sparse voxel octrees](https://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter21_3D_Flight_Navigation_Using_Sparse_Voxel_Octrees.pdf)

Something interesting about this implementation is that the octree is constructed from the 'ground' up, rather than from the 'top' down. It begins with the lowest level of the tree - the leaf nodes - which contain contain the collision geometry. It then moves up the tree layer by layer adding the parent nodes, and links bewtween leaf nodes - neighbours.

The Morton encoder is based on this blog [Morton encoding/decoding through bit interleaving: Implementations](https://forceflow.be/2013/10/07/morton-encodingdecoding-through-bit-interleaving-implementations/)

Morton code is used to map the 3D coordinates of the SVO onto 1D values, while preserving the locality of the 3D coordinates. This helps with storage and retrieval. I used the 'magic bits' implementation as described in the blog.  
