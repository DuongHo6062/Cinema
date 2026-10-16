using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminCategoryService
{
    Task<IReadOnlyList<AdminCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<AdminCategoryDto> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminCategoryDto> CreateCategoryAsync(AdminCreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<AdminCategoryDto> UpdateCategoryAsync(int id, AdminUpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
