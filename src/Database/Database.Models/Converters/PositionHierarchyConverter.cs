using System.Diagnostics.CodeAnalysis;
using Project.Core.Models.Position;
using Project.Core.Models.PositionHistory;

namespace Database.Models.Converters;

public class PositionHierarchyConverter
{
    [return: NotNullIfNotNull("positionHierarchy")]
    public static PositionHierarchyDb? Convert(PositionHierarchy? positionHierarchy)
    {
        if (positionHierarchy == null)
            return null;

        return new PositionHierarchyDb(
            positionHierarchy.PositionId,
            positionHierarchy.ParentId,
            positionHierarchy.Title,
            positionHierarchy.Level);
    }

    [return: NotNullIfNotNull("positionHierarchyDb")]
    public static PositionHierarchy? Convert(PositionHierarchyDb? positionHierarchyDb)
    {
        if (positionHierarchyDb == null)
            return null;

        return new PositionHierarchy(
            positionHierarchyDb.PositionId,
            positionHierarchyDb.ParentId,
            positionHierarchyDb.Title,
            positionHierarchyDb.Level);
    }

    [return: NotNullIfNotNull("positionHierarchy")]
    public static PositionHierarchyMongoDb? ConvertMongo(PositionHierarchy? positionHierarchy)
    {
        if (positionHierarchy == null)
            return null;

        return new PositionHierarchyMongoDb(
            positionHierarchy.PositionId,
            positionHierarchy.ParentId,
            positionHierarchy.Title,
            positionHierarchy.Level);
    }

    [return: NotNullIfNotNull("positionHierarchyDb")]
    public static PositionHierarchy? ConvertMongo(PositionHierarchyMongoDb? positionHierarchyDb)
    {
        if (positionHierarchyDb == null)
            return null;

        return new PositionHierarchy(
            positionHierarchyDb.PositionId,
            positionHierarchyDb.ParentId,
            positionHierarchyDb.Title,
            positionHierarchyDb.Level);
    }
}