using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace EHub.FeeModule.StudentFeeProfiles;

[RemoteService(IsEnabled = false)]
public class StudentFeeProfileAppService : ApplicationService, IStudentFeeProfileAppService
{
    private readonly IStudentFeeProfileRepository _repo;
    private readonly StudentFeeProfileManager _manager;

    public StudentFeeProfileAppService(
        IStudentFeeProfileRepository repo,
        StudentFeeProfileManager manager)
    {
        _repo = repo;
        _manager = manager;
    }

    public async Task<StudentFeeProfileDto> GetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        return ObjectMapper.Map<StudentFeeProfile, StudentFeeProfileDto>(entity!);
    }

    public async Task<PagedResultDto<StudentFeeProfileDto>> GetListAsync(GetStudentFeeProfileListInput input)
    {
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            input.Sorting = nameof(StudentFeeProfile.CreationTime) + " DESC";
        }

        var totalCount = await _repo.GetCountAsync(
            input.Filter,
            input.StudentId,
            input.FeeStructureId,
            input.IsActive
        );

        var list = await _repo.GetListAsync(
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            input.Filter,
            input.StudentId,
            input.FeeStructureId,
            input.IsActive
        );

        var items = list.Select(x => ObjectMapper.Map<StudentFeeProfile, StudentFeeProfileDto>(x)).ToList();
        return new PagedResultDto<StudentFeeProfileDto>(totalCount, items);
    }

    public async Task<StudentFeeProfileDto> CreateAsync(CreateUpdateStudentFeeProfileDto input)
    {
        var tenantId = CurrentTenant.Id;

        var entity = await _manager.CreateAsync(
            input.StudentId,
            input.FeeStructureId,
            input.EffectiveFrom,
            input.EffectiveTo,
            input.IsActive
        );

        entity = await _repo.InsertAsync(entity);

        return ObjectMapper.Map<StudentFeeProfile, StudentFeeProfileDto>(entity);
    }

    public async Task UpdateAsync(Guid id, CreateUpdateStudentFeeProfileDto input)
    {
        var entity = await _repo.GetAsync(id);

        await _manager.UpdateAsync(
            entity,
            input.StudentId,
            input.FeeStructureId,
            input.EffectiveFrom,
            input.EffectiveTo,
            input.IsActive
        );

        await _repo.UpdateAsync(entity, autoSave: true);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
    }
}
