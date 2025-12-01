using System;
using TreeSplitting.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TreeSplitting.Rendering
{
    public class WoodWorkItemRenderer : IRenderer, IDisposable
    {
        private ICoreClientAPI api;
        private BlockPos pos;
        private BEChoppingBlock be;
        private MeshRef meshRef;
        private MeshRef overlayMeshRef;
        private Matrixf ModelMat = new Matrixf();

        public double RenderOrder => 0.5;
        public int RenderRange => 24;

        public WoodWorkItemRenderer(BEChoppingBlock be, BlockPos pos, ICoreClientAPI api)
        {
            this.api = api;
            this.pos = pos;
            this.be = be;
        }

        public void RegenMesh(ItemStack workItem, byte[,,] voxels, byte[,,] targetVoxels = null)
        {
            if (workItem == null)
            {
                meshRef?.Dispose();
                meshRef = null;
                overlayMeshRef?.Dispose();
                overlayMeshRef = null;
                return;
            }

            CollectibleObject collectible = workItem.Item ?? (CollectibleObject)workItem.Block;
            ITexPositionSource texSource;
            if (collectible is Block block)
                texSource = api.Tesselator.GetTextureSource(block);
            else
                texSource = api.Tesselator.GetTextureSource((Item)collectible);

            TextureAtlasPosition texRing = texSource["up"] ?? texSource["top"];
            TextureAtlasPosition texBark = texSource["bark"] ?? texSource["side"];

            if (texRing == null) texRing = texSource["all"] ?? texSource["base"] ?? api.BlockTextureAtlas.UnknownTexturePosition;
            if (texBark == null) texBark = texRing;

            MeshData mesh = new MeshData(24, 36);
            
            float pixel = 1f / 16f; 
            float yOffset = 10f / 16f; 

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (voxels[x, y, z] != 0)
                        {
                            float px = x * pixel;
                            float py = y * pixel + yOffset;
                            float pz = z * pixel;
                            
                            bool drawUp = (y == 15) || (voxels[x, y + 1, z] == 0);
                            bool drawDown = (y == 0) || (voxels[x, y - 1, z] == 0);
                            bool drawNorth = (z == 0) || (voxels[x, y, z - 1] == 0);
                            bool drawSouth = (z == 15) || (voxels[x, y, z + 1] == 0);
                            bool drawWest = (x == 0) || (voxels[x - 1, y, z] == 0);
                            bool drawEast = (x == 15) || (voxels[x + 1, y, z] == 0);

                            if (drawUp)    AddFace(mesh, BlockFacing.UP, px, py, pz, pixel, x, y, z, texRing);
                            if (drawDown)  AddFace(mesh, BlockFacing.DOWN, px, py, pz, pixel, x, y, z, texRing);
                            if (drawNorth) AddFace(mesh, BlockFacing.NORTH, px, py, pz, pixel, x, y, z, texBark);
                            if (drawSouth) AddFace(mesh, BlockFacing.SOUTH, px, py, pz, pixel, x, y, z, texBark);
                            if (drawWest)  AddFace(mesh, BlockFacing.WEST, px, py, pz, pixel, x, y, z, texBark);
                            if (drawEast)  AddFace(mesh, BlockFacing.EAST, px, py, pz, pixel, x, y, z, texBark);
                        }
                    }
                }
            }

            if (meshRef != null) meshRef.Dispose();
            meshRef = api.Render.UploadMesh(mesh);

            GenerateOverlay(voxels, targetVoxels, yOffset, pixel);
        }

        private void AddFace(MeshData mesh, BlockFacing face, float x, float y, float z, float s, int vx, int vy, int vz, TextureAtlasPosition tex)
        {
            float x1 = x, y1 = y, z1 = z;
            float x2 = x + s, y2 = y + s, z2 = z + s;
            
            float mapX = vx / 16f;
            float mapY = vy / 16f;
            float mapZ = vz / 16f;
            float mapS = 1f / 16f;

            float uMin=0, vMin=0, uMax=0, vMax=0;

            if (face == BlockFacing.UP)
            {
                uMin = mapX; vMin = mapZ; uMax = mapX + mapS; vMax = mapZ + mapS;
                AddQuad(mesh, x1, y2, z1, x2, y2, z1, x2, y2, z2, x1, y2, z2, uMin, vMin, uMax, vMax, tex);
            }
            else if (face == BlockFacing.DOWN)
            {
                uMin = mapX; vMin = mapZ; uMax = mapX + mapS; vMax = mapZ + mapS;
                AddQuad(mesh, x1, y1, z2, x2, y1, z2, x2, y1, z1, x1, y1, z1, uMin, vMin, uMax, vMax, tex);
            }
            else if (face == BlockFacing.NORTH)
            {
                uMin = mapX; vMin = 1f - (mapY + mapS); uMax = mapX + mapS; vMax = 1f - mapY;
                AddQuad(mesh, x1, y2, z1, x2, y2, z1, x2, y1, z1, x1, y1, z1, uMin, vMin, uMax, vMax, tex);
            }
            else if (face == BlockFacing.SOUTH)
            {
                uMin = mapX; vMin = 1f - (mapY + mapS); uMax = mapX + mapS; vMax = 1f - mapY;
                AddQuad(mesh, x2, y2, z2, x1, y2, z2, x1, y1, z2, x2, y1, z2, uMin, vMin, uMax, vMax, tex);
            }
            else if (face == BlockFacing.WEST)
            {
                uMin = mapZ; vMin = 1f - (mapY + mapS); uMax = mapZ + mapS; vMax = 1f - mapY;
                AddQuad(mesh, x1, y2, z2, x1, y2, z1, x1, y1, z1, x1, y1, z2, uMin, vMin, uMax, vMax, tex);
            }
            else if (face == BlockFacing.EAST)
            {
                uMin = mapZ; vMin = 1f - (mapY + mapS); uMax = mapZ + mapS; vMax = 1f - mapY;
                AddQuad(mesh, x2, y2, z1, x2, y2, z2, x2, y1, z2, x2, y1, z1, uMin, vMin, uMax, vMax, tex);
            }
        }

        private void AddQuad(MeshData mesh, float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, float uMin, float vMin, float uMax, float vMax, TextureAtlasPosition tex)
        {
            int i = mesh.VerticesCount;
            
            mesh.AddVertex(x0, y0, z0, Interp(tex.x1, tex.x2, uMin), Interp(tex.y1, tex.y2, vMin), ColorUtil.WhiteArgb);
            mesh.AddVertex(x1, y1, z1, Interp(tex.x1, tex.x2, uMax), Interp(tex.y1, tex.y2, vMin), ColorUtil.WhiteArgb);
            mesh.AddVertex(x2, y2, z2, Interp(tex.x1, tex.x2, uMax), Interp(tex.y1, tex.y2, vMax), ColorUtil.WhiteArgb);
            mesh.AddVertex(x3, y3, z3, Interp(tex.x1, tex.x2, uMin), Interp(tex.y1, tex.y2, vMax), ColorUtil.WhiteArgb);
            
            mesh.AddIndex(i+0); mesh.AddIndex(i+1); mesh.AddIndex(i+2);
            mesh.AddIndex(i+0); mesh.AddIndex(i+2); mesh.AddIndex(i+3);
        }
        
        private float Interp(float min, float max, float t) { return min + (max - min) * t; }

        private void GenerateOverlay(byte[,,] voxels, byte[,,] targetVoxels, float yOffset, float pixel)
        {
            if (overlayMeshRef != null) { overlayMeshRef.Dispose(); overlayMeshRef = null; }
            if (targetVoxels == null) return;

            MeshData overlayMesh = new MeshData(24, 36);
            overlayMesh.SetMode(EnumDrawMode.Lines);
            int greenCol = ColorUtil.ToRgba(255, 0, 255, 0);

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                         if (targetVoxels[x, y, z] != 0 && voxels[x, y, z] != 0)
                         {
                                float px = x * pixel;
                                float py = y * pixel + yOffset;
                                float pz = z * pixel;
                                AddWireframeBox(overlayMesh, px, py, pz, px+pixel, py+pixel, pz+pixel, greenCol);
                         }
                    }
                }
            }
             if (overlayMesh.VerticesCount > 0)
            {
                overlayMeshRef = api.Render.UploadMesh(overlayMesh);
            }
        }

        private void AddWireframeBox(MeshData mesh, float x1, float y1, float z1, float x2, float y2, float z2, int color)
        {
            int i = mesh.VerticesCount;
            mesh.AddVertex(x1, y1, z1, 0, 0, color); 
            mesh.AddVertex(x2, y1, z1, 0, 0, color); 
            mesh.AddVertex(x2, y1, z2, 0, 0, color); 
            mesh.AddVertex(x1, y1, z2, 0, 0, color); 
            mesh.AddVertex(x1, y2, z1, 0, 0, color); 
            mesh.AddVertex(x2, y2, z1, 0, 0, color); 
            mesh.AddVertex(x2, y2, z2, 0, 0, color); 
            mesh.AddVertex(x1, y2, z2, 0, 0, color); 

            mesh.AddIndex(i+0); mesh.AddIndex(i+1); mesh.AddIndex(i+1); mesh.AddIndex(i+2);
            mesh.AddIndex(i+2); mesh.AddIndex(i+3); mesh.AddIndex(i+3); mesh.AddIndex(i+0);
            mesh.AddIndex(i+4); mesh.AddIndex(i+5); mesh.AddIndex(i+5); mesh.AddIndex(i+6);
            mesh.AddIndex(i+6); mesh.AddIndex(i+7); mesh.AddIndex(i+7); mesh.AddIndex(i+4);
            mesh.AddIndex(i+0); mesh.AddIndex(i+4); mesh.AddIndex(i+1); mesh.AddIndex(i+5);
            mesh.AddIndex(i+2); mesh.AddIndex(i+6); mesh.AddIndex(i+3); mesh.AddIndex(i+7);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshRef == null) return;
            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;
            rpi.GlDisableCullFace();
            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z).Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            prog.Tex2D = api.BlockTextureAtlas.Positions[0].atlasTextureId;
            rpi.RenderMesh(meshRef);
            if (overlayMeshRef != null)
            {
                prog.ExtraGlow = 255;
                rpi.RenderMesh(overlayMeshRef);
                prog.ExtraGlow = 0;
            }
            prog.Stop();
        }

        public void Dispose() 
        { 
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque); 
            meshRef?.Dispose(); 
            overlayMeshRef?.Dispose(); 
        }
    }
}