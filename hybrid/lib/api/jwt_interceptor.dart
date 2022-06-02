import 'package:dio/dio.dart';
import 'package:hybrid/config/app_cache.dart';
import 'package:hybrid/services/account_service.dart';
import 'package:jwt_decoder/jwt_decoder.dart';

class JwtInterceptor extends Interceptor {
  final Dio dio;
  final AppCache _appCache = AppCache();
  final AccountService _accountService = AccountService();

  JwtInterceptor({required this.dio});

  @override
  void onRequest(
      RequestOptions options, RequestInterceptorHandler handler) async {
    String? accessToken = await _appCache.getUserToken();
    if (accessToken != null) {
      // check if token is expired
      // try refresh token
      options.headers['Authorization'] = 'Bearer $accessToken';
    }
    await refreshToken(options);

    return handler.next(options);
  }

  refreshToken(RequestOptions options) async {
    if (!options.uri.toString().contains('account')) {
      String? localToken = await _appCache.getUserToken();
      if (localToken == null) return;

      bool isNotExpired = JwtDecoder.isExpired(localToken);
      var remainingTime = JwtDecoder.getRemainingTime(localToken);
      if (!isNotExpired && remainingTime.inMinutes < 60) {
        await _accountService.tryRefreshingToken();
      }
    }
  }
}
