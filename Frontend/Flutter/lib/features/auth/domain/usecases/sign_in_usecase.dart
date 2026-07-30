import '../entities/signed_in_user.dart';
import '../../data/repositories/auth_repository_impl.dart';

class SignInUseCase {
  SignInUseCase(this._repository);

  final AuthRepository _repository;

  Future<SignInResponse> call(String email, String password) =>
      _repository.signIn(email, password);
}
