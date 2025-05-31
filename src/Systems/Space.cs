using System;
using System.Numerics;
using MoonWorks;
using MoonTools.ECS;

using MoonWorksAssignmentPorts.Components;
using MoonWorksAssignmentPorts.Relations;

namespace MoonWorksAssignmentPorts.Systems;

public class Space : MoonTools.ECS.System
{
    TimeSpan TotalTime;

    Filter CanOrbitFilter;
    Filter CanPulsateFilter;
    Filter CanRotateFilter;

    public Space(World world) : base(world)
    {
        TotalTime = TimeSpan.Zero;
        CanOrbitFilter = FilterBuilder.Include<CanOrbit>().Build();
        CanPulsateFilter = FilterBuilder.Include<CanPulsate>().Build();
        CanRotateFilter = FilterBuilder.Include<CanRotate>().Build();
    }

    override public void Update(TimeSpan delta)
    {
        TotalTime += delta;
        var theta = (float) TotalTime.TotalMilliseconds / 1000f;

        foreach (var entity in CanOrbitFilter.Entities)
        {
            var pos = new Vector2(0);

            foreach (var other in OutRelations<RelativePosition>(entity))
            {
                var otherPos = Get<Position>(other);
                pos += new Vector2(otherPos.X, otherPos.Y);
            }

            var (r, omega) = Get<CanOrbit>(entity);
            pos.X += r * MathF.Cos(omega * theta);
            pos.Y += r * MathF.Sin(omega * theta);

            Set<Position>(entity, new Position(pos.X, pos.Y));
        }

        foreach (var entity in CanRotateFilter.Entities)
        {
            float omega = Get<CanRotate>(entity).AngularFrequency;
            Set<Rotation>(entity, new Rotation(omega * theta));
        }

        foreach (var entity in CanPulsateFilter.Entities)
        {
            var (initW, initH, alpha, omega) = Get<CanPulsate>(entity);
            float scalar = 1f - alpha * MathF.Cos(omega * theta);

            Set<Scale>(entity, new Scale(initW * scalar, initH * scalar));
        }
    }
}
