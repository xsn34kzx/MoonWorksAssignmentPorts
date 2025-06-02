using System;
using MoonWorks;
using MoonTools.ECS;

using MoonWorksAssignmentPorts.Components;
using MoonWorksAssignmentPorts.Relations;
using MoonWorksAssignmentPorts.Systems;

namespace MoonWorksAssignmentPorts;

public class SimpleScene : Assignment
{
    World World;
    Space Space;
    Renderer Renderer;

    override public void Init()
    {
        Window.SetTitle("Simple Scene");

        World = new World();
        Space = new(World);
        Renderer = new(World, GraphicsDevice, RootTitleStorage, Window.SwapchainFormat);

        var sun = World.CreateEntity();
        World.Set(sun, new Position(0, 0));
        World.Set(sun, new CanPulsate(3, 3, 0.1f, 1.5f));
        World.Set(sun, new Sprite(3145f / 3726f, 0, 581f / 3726f, 565f / 2500f));

        var earth = World.CreateEntity();
        World.Set(earth, new Position(0, 0));
        World.Set(earth, new Scale(0.75f, 0.75f));
        World.Set(earth, new CanOrbit(2.5f, 1.5f));
        World.Set(earth, new Sprite(3145f / 3726f, 566 / 2500f, 513f / 3726f, 519f / 2500f));
        World.Relate(earth, sun, new RelativePosition()); 

        var moon = World.CreateEntity();
        World.Set(moon, new Position(0, 0));
        World.Set(moon, new Scale(0.25f, 0.25f));
        World.Set(moon, new CanOrbit(0.75f, -2f));
        World.Set(moon, new Sprite(2501f / 3726f, 0, 643f / 3726f, 642f / 2500f));
        World.Relate(moon, earth, new RelativePosition());

        var universe = World.CreateEntity();
        World.Set(universe, new Position(0, 0));
        World.Set(universe, new Scale(8, 8));
        World.Set(universe, new CanRotate(0.2f));
        World.Set(universe, new Sprite(0, 0, 2500f / 3726f, 1));
    }

    override public void Update(TimeSpan delta)
    {
        Space.Update(delta);
        World.FinishUpdate();
    }

    override public void Draw(double alpha)
    {
        Renderer.Render(Window);
    }

    override public void Destroy()
    {
        World.Dispose();
    }
}
