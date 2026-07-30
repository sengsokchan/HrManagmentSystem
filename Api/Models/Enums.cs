namespace HrManagementSystem.Domain;

public enum EmployeeStatus
{
    Active,
    Inactive,
    OnLeave,
    Resigned
}

public enum AttendanceStatus
{
    Present,
    Late,
    Absent,
    HalfDay
}

public enum LeaveStatus
{
    Pending,
    ManagerApproved,
    Approved,
    Rejected,
    Cancelled
}

public enum PayrollStatus
{
    Draft,
    Approved,
    Paid
}
