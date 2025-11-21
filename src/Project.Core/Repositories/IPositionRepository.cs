using Project.Core.Models;
using Project.Core.Models.Position;
using Project.Core.Models.PositionHistory;

namespace Project.Core.Repositories;

public interface IPositionRepository
{
    Task<BasePosition> AddPositionAsync(CreatePosition position);
    Task<BasePosition> GetPositionByIdAsync(Guid id);
    Task<BasePosition> GetHeadPositionByCompanyIdAsync(Guid id);
    Task<BasePosition> UpdatePositionTitleAsync(Guid id, string? title);
    Task<BasePosition> UpdatePositionParentWithSubordinatesAsync(Guid id, Guid? parentId);
    Task<BasePosition> UpdatePositionParentWithoutSuboridnatesAsync(Guid id, Guid? parentId);
    Task DeletePositionAsync(Guid id);
    Task<IEnumerable<PositionHierarchy>> GetSubordinatesAsync(Guid parentId);
}