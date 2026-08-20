import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import 'session.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key, required this.onLoggedIn});

  final ValueChanged<CaptainSession> onLoggedIn;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _serverController = TextEditingController(text: 'http://10.0.2.2:5195');
  final _userController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _hidePassword = true;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _serverController.dispose();
    _userController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (_busy || !_formKey.currentState!.validate()) return;

    setState(() {
      _busy = true;
      _error = null;
    });

    ApiClient? client;
    try {
      client = ApiClient(baseUrl: _serverController.text);
      final response = await client.post('Auth/login', {
        'userName': _userController.text.trim(),
        'password': _passwordController.text,
      });

      if (response is! Map<String, dynamic>) {
        throw const ApiException('Unexpected login response.');
      }

      final role = response['role']?.toString() ?? '';
      if (role != 'Captain' && role != 'Admin') {
        throw const ApiException(
          'This app can only be used by a Captain account.',
        );
      }

      final token = response['token']?.toString() ?? '';
      if (token.isEmpty) {
        throw const ApiException('Login token was not returned.');
      }

      widget.onLoggedIn(
        CaptainSession(
          baseUrl: client.baseUrl,
          token: token,
          fullName: response['fullName']?.toString() ?? 'Captain',
          role: role,
        ),
      );
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      client?.close();
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Card(
                color: Colors.white,
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Icon(
                          Icons.restaurant_menu,
                          size: 54,
                          color: Color(0xFF0284C7),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'SULTHAN ERP',
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.headlineSmall
                              ?.copyWith(fontWeight: FontWeight.w800),
                        ),
                        const Text(
                          'Captain ordering',
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 28),
                        TextFormField(
                          controller: _serverController,
                          keyboardType: TextInputType.url,
                          decoration: const InputDecoration(
                            labelText: 'Server address',
                            prefixIcon: Icon(Icons.lan_outlined),
                            helperText:
                                'Phone: use cashier PC IPv4, e.g. http://192.168.1.20:5195',
                          ),
                          validator: (value) =>
                              value == null || value.trim().isEmpty
                              ? 'Server address is required.'
                              : null,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: _userController,
                          textInputAction: TextInputAction.next,
                          decoration: const InputDecoration(
                            labelText: 'Username',
                            prefixIcon: Icon(Icons.person_outline),
                          ),
                          validator: (value) =>
                              value == null || value.trim().isEmpty
                              ? 'Username is required.'
                              : null,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: _passwordController,
                          obscureText: _hidePassword,
                          onFieldSubmitted: (_) => _login(),
                          decoration: InputDecoration(
                            labelText: 'Password',
                            prefixIcon: const Icon(Icons.lock_outline),
                            suffixIcon: IconButton(
                              onPressed: () => setState(
                                () => _hidePassword = !_hidePassword,
                              ),
                              icon: Icon(
                                _hidePassword
                                    ? Icons.visibility_outlined
                                    : Icons.visibility_off_outlined,
                              ),
                            ),
                          ),
                          validator: (value) => value == null || value.isEmpty
                              ? 'Password is required.'
                              : null,
                        ),
                        if (_error != null) ...[
                          const SizedBox(height: 14),
                          Text(
                            _error!,
                            style: const TextStyle(color: Colors.red),
                          ),
                        ],
                        const SizedBox(height: 20),
                        FilledButton.icon(
                          onPressed: _busy ? null : _login,
                          icon: _busy
                              ? const SizedBox.square(
                                  dimension: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : const Icon(Icons.login),
                          label: const Text('Sign in'),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
