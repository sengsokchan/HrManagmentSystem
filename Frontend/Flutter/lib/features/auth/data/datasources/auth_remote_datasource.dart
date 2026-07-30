import '../../../../core/network/api_client.dart';
import '../../domain/entities/signed_in_user.dart';

class AuthRemoteDataSource {
  AuthRemoteDataSource(this._apiClient);

  final ApiClient _apiClient;

  Future<SignInResponse> signIn(String email, String password) async {
    final response = await _apiClient.postMap(
      '/api/auth/login',
      {'email': email, 'password': password},
      skipAuth: true,
    );
    return SignInResponse.fromJson(response);
  }
}
