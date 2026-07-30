import '../../domain/entities/signed_in_user.dart';
import '../datasources/auth_remote_datasource.dart';

abstract class AuthRepository {
  Future<SignInResponse> signIn(String email, String password);
}

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl(this._dataSource);

  final AuthRemoteDataSource _dataSource;

  @override
  Future<SignInResponse> signIn(String email, String password) =>
      _dataSource.signIn(email, password);
}
