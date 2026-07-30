using HrManagementSystem.Domain;

namespace HrManagementSystem.Application.Services;

public sealed class ReferenceDataService(IHrRepository repository) : IReferenceDataService
{
    public IEnumerable<Department> GetDepartments() => repository.Departments;
    public IEnumerable<Position> GetPositions() => repository.Positions;
    public IEnumerable<Branch> GetBranches() => repository.Branches;
}
