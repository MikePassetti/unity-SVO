# unity-SVO
unity implementation of SVO and Morton encoder

The SVO is based on this paper [3D flight navigation using sparse voxel octrees](https://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter21_3D_Flight_Navigation_Using_Sparse_Voxel_Octrees.pdf)

Something interesting about this implementation is that the octree is constructed from the 'ground' up, rather than from the 'top' down. It begins with the lowest level of the tree - the leaf nodes - which contain contain the collision geometry. It then moves up the tree layer by layer adding the parent nodes, and links bewtween leaf nodes - neighbours.

The Morton encoder is based on this blog [Morton encoding/decoding through bit interleaving: Implementations](https://forceflow.be/2013/10/07/morton-encodingdecoding-through-bit-interleaving-implementations/)

Morton code is used to map the 3D coordinates of the SVO onto 1D values, while preserving the locality of the 3D coordinates. This helps with storage and retrieval. I used the 'magic bits' implementation as described in the blog.

Can increase/decrease resolution of leaf nodes (amount of voxels) by adjusting voxelSize variable:

#### Voxel Size = 1
<img width="2560" height="1303" alt="SVO-voxels-size1" src="https://github.com/user-attachments/assets/98029d6b-9380-4be5-83a8-a9c75f81848f" />

#### Voxel Size = 2
<img width="2560" height="1303" alt="SVO-voxels-size2" src="https://github.com/user-attachments/assets/2a34e10d-84fd-4a3e-a3ea-69228d1f35ec" />

#### Voxel Size = 4
<img width="2560" height="1303" alt="SVO-voxels-size4(1)" src="https://github.com/user-attachments/assets/fb63616c-1ffc-4328-a3fe-519ad8fc4838" />


