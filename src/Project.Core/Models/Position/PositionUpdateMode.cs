namespace Project.Core.Models.Position;

public enum PositionUpdateMode
{
    UpdateWithSubordinates = 1,
    UpdateWithoutSubordinates = 2
}

public static class PositionUpdateModeExtension
{
    public static int ToInt(this PositionUpdateMode positionUpdateMode)
    {
        return (int)positionUpdateMode;
    }

    public static PositionUpdateMode ToPositionUpdateMode(this int positionUpdateMode)
    {
        return (PositionUpdateMode)positionUpdateMode;
    }
}