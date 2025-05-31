namespace MoonWorksAssignmentPorts.Components;

public readonly record struct Position(float X, float Y);
public readonly record struct Rotation(float Angle);
public readonly record struct Scale(float Width, float Height);
public readonly record struct Sprite(float U, float V, float Width, float Height);

public readonly record struct CanOrbit(float Radius, float AngularFrequency);
public readonly record struct CanRotate(float AngularFrequency);
public readonly record struct CanPulsate(float InitialWidth, float InitialHeight, float Percentage, float AngularFrequency);
