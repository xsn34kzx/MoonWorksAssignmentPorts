using System;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using System.Numerics;
using Buffer = MoonWorks.Graphics.Buffer;

namespace MoonWorksAssignmentPorts;

public class SimpleScene : Assignment
{
    GraphicsPipeline RenderPipeline;
    Texture SpriteAtlasTexture;
    Sampler Sampler;
    TransferBuffer SpriteDataTransferBuffer;
    Buffer SpriteDataBuffer;

    const int SPRITE_COUNT = 4;

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    struct SpriteInstance
    {
        [FieldOffset(0)]
        public Vector3 Position;

        [FieldOffset(12)]
        public float Rotation;

        [FieldOffset(16)]
        public Vector2 Size;

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

    TimeSpan TotalAccumulatedTime = TimeSpan.Zero;

    override public void Init()
    {
        Window.SetTitle("Simple Scene");

        Shader vertShader = ShaderCross.Create(
            GraphicsDevice,
            RootTitleStorage,
            "/Content/Shaders/PullSpriteBatch.vert.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Vertex
        );

        Shader fragShader = ShaderCross.Create(
            GraphicsDevice,
            RootTitleStorage,
            "/Content/Shaders/TextureQuadColor.frag.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Fragment
        );

        GraphicsPipelineCreateInfo pipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = Window.SwapchainFormat,
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

        RenderPipeline = GraphicsPipeline.Create(GraphicsDevice, pipelineCreateInfo);

        var resourceUploader = new ResourceUploader(GraphicsDevice);
        SpriteAtlasTexture = resourceUploader.CreateTexture2DFromCompressed(
            RootTitleStorage,
            "/Content/Textures/space.png",
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler
        );

        Sampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);

        resourceUploader.Upload();
        resourceUploader.Dispose();

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
    }

    override public void Update(TimeSpan delta)
    {
        TotalAccumulatedTime += delta;

        var earthTheta = (float)TotalAccumulatedTime.TotalMilliseconds / 1000;
        var moonTheta = -earthTheta * 2.0f;
        var sunTheta = earthTheta * 1.5f;
        var universeTheta = earthTheta * 0.2f;

        var data = SpriteDataTransferBuffer.Map<SpriteInstance>(true);

        // Universe
        data[0].Position = new Vector3(0);
        data[0].Rotation = universeTheta;
        data[0].Size = new Vector2(8f);
        data[0].TexU = 0;
        data[0].TexV = 0;
        data[0].TexW = 2500f / 3726f;
        data[0].TexH = 1;
        data[0].Color = new Vector4(1);

        // Sun
        data[1].Position = new Vector3(0);
        data[1].Rotation = 0;
        data[1].Size = new Vector2(3) * (1f - 0.1f * MathF.Cos(sunTheta));
        data[1].TexU = 3145f / 3726f;
        data[1].TexV = 0;
        data[1].TexW = 581f / 3726f;
        data[1].TexH = 565f / 2500f;
        data[1].Color = new Vector4(1);
        
        // Earth
        var earthOrbitRadius = 2.5f;
        var earthPosition = new Vector3(
            earthOrbitRadius * MathF.Cos(earthTheta),
            earthOrbitRadius * MathF.Sin(earthTheta),
            0
        );
        data[2].Position = earthPosition;
        data[2].Rotation = 0;
        data[2].Size = new Vector2(0.75f);
        data[2].TexU = 3145f / 3726f;
        data[2].TexV = 566f / 2500f;
        data[2].TexW = 513f / 3726f;
        data[2].TexH = 519f / 2500f;
        data[2].Color = new Vector4(1);
        
        // Moon
        var moonOrbitRadius = 0.75f;
        data[3].Position = new Vector3(
            moonOrbitRadius * MathF.Cos(moonTheta),
            moonOrbitRadius * MathF.Sin(moonTheta),
            0
        ) + earthPosition;
        data[3].Rotation = 0;
        data[3].Size = new Vector2(0.25f);
        data[3].TexU = 2501f / 3726f;
        data[3].TexV = 0;
        data[3].TexW = 643f / 3726f;
        data[3].TexH = 642f / 2500f;
        data[3].Color = new Vector4(1);

        SpriteDataTransferBuffer.Unmap();
    }

    override public void Draw(double alpha)
    {
        Matrix4x4 cameraMatrix =
            Matrix4x4.CreateOrthographicOffCenter(
                -4f, 4f, -3f, 3f, -1f, 1
            );

        CommandBuffer cmdBuf = GraphicsDevice.AcquireCommandBuffer();
        Texture swapchainTexture = cmdBuf.AcquireSwapchainTexture(Window);
        if (swapchainTexture != null)
        {
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

    override public void Destroy()
    {
        RenderPipeline.Dispose();
        Sampler.Dispose();
        SpriteAtlasTexture.Dispose();
        SpriteDataTransferBuffer.Dispose();
        SpriteDataBuffer.Dispose();
    }
}
