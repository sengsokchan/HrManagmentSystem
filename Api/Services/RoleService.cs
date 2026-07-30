namespace HrManagementSystem.Application.Services;

public sealed class RoleService(IHrRepository repository) : IRoleService
{
    public IEnumerable<RoleView> GetRoles() =>
        repository.Roles.Select(role => new RoleView(role.Id, role.Name, repository.GetPermissions(role.Name)));
}
