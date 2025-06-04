using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using Buffer = MoonWorks.Graphics.Buffer; 

public class SpriteBatch
{
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

    const int MAX_SPRITE_COUNT = 8192;

    GraphicsPipeline GraphicsPipeline;
    TransferBuffer SpriteDataTransferBuffer;
    Buffer SpriteDataBuffer;
    int InstanceIndex;
    public uint InstanceCount => (uint) InstanceIndex;

    public SpriteBatch(GraphicsDevice graphicsDevice, TitleStorage titleStorage, TextureFormat textureFormat)
    {
        Shader vertShader = ShaderCross.Create(
            graphicsDevice,
            titleStorage,
            "/Content/Shaders/PullSpriteBatch.vert.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Vertex
        );

        Shader fragShader = ShaderCross.Create(
            graphicsDevice,
            titleStorage,
            "/Content/Shaders/TextureQuadColor.frag.hlsl",
            "main",
            ShaderCross.ShaderFormat.HLSL,
            ShaderStage.Fragment
        );

        var pipelineCreateInfo = new GraphicsPipelineCreateInfo
        {
            TargetInfo = new GraphicsPipelineTargetInfo
            {
                ColorTargetDescriptions = [
                    new ColorTargetDescription
                    {
                        Format = textureFormat,
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

        GraphicsPipeline = GraphicsPipeline.Create(graphicsDevice, pipelineCreateInfo);

        vertShader.Dispose();
        fragShader.Dispose();

        SpriteDataTransferBuffer = TransferBuffer.Create<SpriteInstance>(
            graphicsDevice,
            TransferBufferUsage.Upload,
            MAX_SPRITE_COUNT
        );

        SpriteDataBuffer = Buffer.Create<SpriteInstance>(
            graphicsDevice,
            BufferUsageFlags.GraphicsStorageRead,
            MAX_SPRITE_COUNT
        );
    }

    public void Begin()
    {
        SpriteDataTransferBuffer.Map(true);
        InstanceIndex = 0;
    }

    public void Add(
        Vector3 position,
        float rotation,
        Vector2 scale,
        float texU,
        float texV,
        float texWidth,
        float texHeight,
        Vector4 color)
    {
        var instanceSpan = SpriteDataTransferBuffer.MappedSpan<SpriteInstance>();
        instanceSpan[InstanceIndex].Position = position;
        instanceSpan[InstanceIndex].Rotation = rotation;
        instanceSpan[InstanceIndex].Scale = scale;
        instanceSpan[InstanceIndex].TexU = texU;
        instanceSpan[InstanceIndex].TexV = texV;
        instanceSpan[InstanceIndex].TexW = texWidth;
        instanceSpan[InstanceIndex].TexH = texHeight;
        instanceSpan[InstanceIndex].Color = color;
        InstanceIndex += 1;
    }

    public void Upload(MoonWorks.Graphics.CommandBuffer commandBuffer)
    {
        SpriteDataTransferBuffer.Unmap();

        if (InstanceCount > 0)
        {
            var copyPass = commandBuffer.BeginCopyPass();
            copyPass.UploadToBuffer(SpriteDataTransferBuffer, SpriteDataBuffer, true);
            commandBuffer.EndCopyPass(copyPass);
        }
    }

    public void Render(RenderPass renderPass, Texture texture, Sampler sampler)
    {
        if (InstanceCount > 0)
        {
            renderPass.BindGraphicsPipeline(GraphicsPipeline);
            renderPass.BindVertexStorageBuffers(SpriteDataBuffer);
            renderPass.BindFragmentSamplers(new TextureSamplerBinding(texture, sampler));
            renderPass.DrawPrimitives(InstanceCount * 6, 1, 0, 0);
        }
    }
}
