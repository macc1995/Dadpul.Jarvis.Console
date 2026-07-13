namespace Dadpul.Jarvis.Console.Tools.Lights;

internal sealed class VirtualLight
{
    public bool IsOn { get; private set; } = true;

    public void TurnOn()
    {
        IsOn = true;
    }

    public void TurnOff()
    {
        IsOn = false;
    }
}