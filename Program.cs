using System;
using MoonWorks;
using MoonWorks.Graphics;

namespace MoonWorksAssignmentPorts;

public class Program : Game
{
    //Assignment[] Assignments;
    int AssignmentIndex = 0;

    public Program(
            AppInfo appInfo,
            WindowCreateInfo windowCreateInfo,
            FramePacingSettings framePacingSettings,
            bool debugMode
    ) : base(
            appInfo,
            windowCreateInfo,
            framePacingSettings,
            ShaderFormat.SPIRV | ShaderFormat.DXBC | ShaderFormat.DXIL | ShaderFormat.MSL,
            debugMode
        )
    {
        ShaderCross.Initialize();
    }

    override protected void Update(TimeSpan delta)
    {
    }

    override protected void Draw(double alpha)
    {
    }

    static void Main(string[] args)
    {
        var windowCreateInfo = new WindowCreateInfo(
            "MoonWorksAssignmentPorts",
            960,
            720,
            ScreenMode.Windowed
        );
        
        var framePacingSettings = FramePacingSettings.CreateCapped(60, 120);

        var game = new Program(
                new AppInfo("", "MoonWorksAssignmentPorts"),
                windowCreateInfo,
                framePacingSettings,
                false);

        game.Run();
    }
}


