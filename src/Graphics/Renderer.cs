using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using Buffer = MoonWorks.Graphics.Buffer; 
using MoonTools.ECS;

using MoonWorksAssignmentPorts.Components;

public class Renderer : MoonTools.ECS.Renderer
{
    GraphicsDevice GraphicsDevice;
    GraphicsPipeline RenderPipeline;
    Texture SpriteAtlasTexture;
    Sampler Sampler;
    TransferBuffer SpriteDataTransferBuffer;
    Buffer SpriteDataBuffer;

    MoonTools.ECS.Filter SpriteFilter;

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct SpriteInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float Rotation;

        [FieldOffset(16)]
        public Vector2 Scale;

        [FieldOffset(24)]
        public float TexU;

        [FieldOffset(28)]
        public float TexV;

        [FieldOffset(32)]
        public float TexW;

        [FieldOffset(36)]
        public float TexH;

        [FieldOffset(48)]
        public Vector4 Color;
    }

    const int SPRITE_COUNT = 4;

    public Renderer(World world, GraphicsDevice graphicsDevice, TitleStorage titleStorage, TextureFormat swapchainFormat) : base(world)
    {
        GraphicsDevice = graphicsDevice;

        Shader vertShader = ShaderCross.Create(
            GraphicsDevice,
            titleStorage,
            "/Content/Shaders/PullSpriteBatch.vert.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Vertex
        );

        Shader fragShader = ShaderCross.Create(
            GraphicsDevice,
            titleStorage,
            "/Content/Shaders/TextureQuadColor.frag.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Fragment
        );

        var renderPipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = swapchainFormat,
                        BlendState = ColorTargetBlendState.NonPremultipliedAlphaBlend
                    }
                ]
            },
            DepthStencilState = DepthStencilState.Disable,
            MultisampleState = MultisampleState.None,
            PrimitiveType = PrimitiveType.TriangleList,
            RasterizerState = RasterizerState.CCW_CullNone,
            VertexInputState = VertexInputState.Empty,
            VertexShader = vertShader,
            FragmentShader = fragShader
        };

        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, renderPipelineCreateInfo);

        var resourceUploader = new ResourceUploader(GraphicsDevice);
        SpriteAtlasTexture = resourceUploader.CreateTexture2DFromCompressed(
            titleStorage,
            "/Content/Textures/space.png",
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler
        );

        resourceUploader.Upload();
        resourceUploader.Dispose();

        Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);

        SpriteDataTransferBuffer = TransferBuffer.Create<SpriteInstance>(
            GraphicsDevice,
            TransferBufferUsage.Upload,
            SPRITE_COUNT
        );

        SpriteDataBuffer = Buffer.Create<SpriteInstance>(
            GraphicsDevice,
            BufferUsageFlags.GraphicsStorageRead,
            SPRITE_COUNT
        );

        SpriteFilter = FilterBuilder.Include<Sprite>().Build();
    }

    public void Render(Window window)
    {
        Matrix4x4 cameraMatrix = Matrix4x4.CreateOrthographic(8, 6, -1, 1);

        var cmdBuf = GraphicsDevice.AcquireCommandBuffer();
        Texture swapchainTexture = cmdBuf.AcquireSwapchainTexture(window);
        if (swapchainTexture != null)
        {
            var data = SpriteDataTransferBuffer.Map<SpriteInstance>(true);
            int instanceIndex = 0;

            foreach (var entity in SpriteFilter.Entities)
            {
                var position = Get<Position>(entity);
                var scale = Get<Scale>(entity);
                var sprite = Get<Sprite>(entity);

                data[instanceIndex].Position = new Vector3(position.X, position.Y, 0);
                data[instanceIndex].Rotation = Has<Rotation>(entity) ? Get<Rotation>(entity).Angle : 0;
                data[instanceIndex].Scale = new Vector2(scale.Width, scale.Height);
                data[instanceIndex].TexU = sprite.U;
                data[instanceIndex].TexV = sprite.V;
                data[instanceIndex].TexW = sprite.Width;
                data[instanceIndex].TexH = sprite.Height;
                data[instanceIndex].Color = new Vector4(1);

                instanceIndex += 1;
            }

            SpriteDataTransferBuffer.Unmap();

            var copyPass = cmdBuf.BeginCopyPass();
            copyPass.UploadToBuffer(SpriteDataTransferBuffer, SpriteDataBuffer, true);
            cmdBuf.EndCopyPass(copyPass);

            var renderPass = cmdBuf.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            cmdBuf.PushVertexUniformData(cameraMatrix);
            
            renderPass.BindGraphicsPipeline(RenderPipeline);
            renderPass.BindFragmentSamplers(new TextureSamplerBinding(SpriteAtlasTexture, Sampler));
            renderPass.BindVertexStorageBuffers(SpriteDataBuffer);
            renderPass.DrawPrimitives(SPRITE_COUNT * 6, 1, 0, 0);

            cmdBuf.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(cmdBuf);
    }
}
