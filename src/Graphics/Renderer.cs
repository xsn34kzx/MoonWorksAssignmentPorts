using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Storage;
using MoonTools.ECS;

using MoonWorksAssignmentPorts.Components;

public class Renderer : MoonTools.ECS.Renderer
{
    GraphicsDevice GraphicsDevice;
    Texture SpriteAtlasTexture;
    Sampler LinearSampler;

    SpriteBatch SpriteBatch;

    MoonTools.ECS.Filter SpriteFilter;

    public Renderer(World world, GraphicsDevice graphicsDevice, TitleStorage titleStorage, TextureFormat swapchainFormat) : base(world)
    {
        GraphicsDevice = graphicsDevice;

        var resourceUploader = new ResourceUploader(GraphicsDevice);
        SpriteAtlasTexture = resourceUploader.CreateTexture2DFromCompressed(
            titleStorage,
            "/Content/Textures/space.png",
            TextureFormat.R8G8B8A8Unorm,
            TextureUsageFlags.Sampler
        );

        resourceUploader.Upload();
        resourceUploader.Dispose();

        LinearSampler = Sampler.Create(GraphicsDevice, SamplerCreateInfo.LinearClamp);

        SpriteBatch = new(graphicsDevice, titleStorage, swapchainFormat);

        SpriteFilter = FilterBuilder.Include<Sprite>().Build();
    }

    public void Render(Window window)
    {
        Matrix4x4 cameraMatrix = Matrix4x4.CreateOrthographic(8, 6, -1, 1);

        var cmdBuf = GraphicsDevice.AcquireCommandBuffer();
        Texture swapchainTexture = cmdBuf.AcquireSwapchainTexture(window);
        if (swapchainTexture != null)
        {
            SpriteBatch.Begin();
            foreach (var entity in SpriteFilter.Entities)
            {
                var position = Get<Position>(entity);
                var scale = Get<Scale>(entity);
                var sprite = Get<Sprite>(entity);

                SpriteBatch.Add(
                    new Vector3(position.X, position.Y, 0),
                    Has<Rotation>(entity) ? Get<Rotation>(entity).Angle : 0,
                    new Vector2(scale.Width, scale.Height),
                    sprite.U,
                    sprite.V,
                    sprite.Width,
                    sprite.Height,
                    new Vector4(1)
                );
            }
            SpriteBatch.Upload(cmdBuf);

            var renderPass = cmdBuf.BeginRenderPass(
                new ColorTargetInfo(swapchainTexture, Color.Black)
            );

            cmdBuf.PushVertexUniformData(cameraMatrix);
            
            SpriteBatch.Render(renderPass, SpriteAtlasTexture, LinearSampler);

            cmdBuf.EndRenderPass(renderPass);
        }

        GraphicsDevice.Submit(cmdBuf);
    }
}
