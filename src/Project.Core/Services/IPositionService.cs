using Project.Core.Models;
using Project.Core.Models.Position;
using Project.Core.Models.PositionHistory;

namespace Project.Core.Services;

public interface IPositionService
{
    Task<BasePosition> AddPositionAsync(Guid? parentId, string title, Guid companyId);
    Task<BasePosition> GetPositionByIdAsync(Guid id);
    Task<BasePosition> GetHeadPositionByCompanyIdAsync(Guid id);
    Task<BasePosition> UpdatePositionTitleAsync(Guid id, string? title);
    Task<BasePosition> UpdatePositionParent(Guid id, Guid? parentId, PositionUpdateMode updateMode);
    Task DeletePositionAsync(Guid id);
    Task<IEnumerable<PositionHierarchy>> GetSubordinatesAsync(Guid parentId);
}