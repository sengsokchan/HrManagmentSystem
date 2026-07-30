class SignedInUser {
  const SignedInUser({
    required this.id,
    required this.email,
    required this.role,
    required this.employeeId,
    required this.employeeName,
    required this.permissions,
    required this.mustChangePassword,
  });

  final int id;
  final String email;
  final String role;
  final int? employeeId;
  final String? employeeName;
  final List<String> permissions;
  final bool mustChangePassword;

  bool get isManager =>
      role.toLowerCase() == 'manager' || can('leave.approve.manager');

  bool get isAdmin =>
      role.toLowerCase().contains('admin') ||
      can('roles.read') ||
      can('employees.write');

  String get displayName =>
      (employeeName != null && employeeName!.trim().isNotEmpty)
          ? employeeName!.trim()
          : email;

  bool can(String permission) =>
      permissions.any((item) => item.toLowerCase() == permission.toLowerCase());

  factory SignedInUser.fromJson(Map<String, dynamic> json) {
    final rawPermissions = json['permissions'];
    final permissions = rawPermissions is List
        ? rawPermissions.map((item) => item.toString()).toList()
        : <String>[];

    return SignedInUser(
      id: json['id'] as int? ?? 0,
      email: json['email']?.toString() ?? '',
      role: json['role']?.toString() ?? '',
      employeeId: json['employeeId'] as int?,
      employeeName: json['employeeName']?.toString(),
      permissions: permissions,
      mustChangePassword: json['mustChangePassword'] == true,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'role': role,
        'employeeId': employeeId,
        'employeeName': employeeName,
        'permissions': permissions,
        'mustChangePassword': mustChangePassword,
      };
}

class SignInResponse {
  const SignInResponse({
    required this.token,
    required this.user,
    required this.mustChangePassword,
  });

  final String token;
  final SignedInUser user;
  final bool mustChangePassword;

  factory SignInResponse.fromJson(Map<String, dynamic> json) {
    final userJson = Map<String, dynamic>.from(json['user'] as Map);
    return SignInResponse(
      token: json['token']?.toString() ?? '',
      user: SignedInUser.fromJson(userJson),
      mustChangePassword: json['mustChangePassword'] == true,
    );
  }
}
