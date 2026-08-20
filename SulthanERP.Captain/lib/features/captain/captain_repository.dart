import '../../core/api/api_client.dart';
import 'models.dart';

class CaptainRepository {
  CaptainRepository(this._api);

  final ApiClient _api;

  Future<List<DiningTableModel>> getTables() async {
    final response = await _api.get('DiningTables');
    return _asMapList(
        response,
      ).map(DiningTableModel.fromJson).where((table) => table.isActive).toList()
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  }

  Future<List<CategoryModel>> getCategories() async {
    final response = await _api.get('Categories');
    return _asMapList(response)
        .map(CategoryModel.fromJson)
        .where((category) => category.isActive)
        .toList()
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  }

  Future<List<MenuItemModel>> getMenuItems() async {
    final response = await _api.get('MenuItems');
    return _asMapList(
        response,
      ).map(MenuItemModel.fromJson).where((item) => item.isAvailable).toList()
      ..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
  }

  Future<CaptainOrderModel?> getTableOrder(int tableId) async {
    try {
      final response = await _api.get('CaptainOrders/table/$tableId');
      return CaptainOrderModel.fromJson(_asMap(response));
    } on ApiException catch (error) {
      if (error.statusCode == 404) return null;
      rethrow;
    }
  }

  Future<CaptainOrderModel> createOrder(
    int tableId,
    Map<int, int> cart, {
    Map<int, String> notes = const {},
  }) async {
    final response = await _api.post('CaptainOrders', {
      'diningTableId': tableId,
      'items': _requestItems(cart, notes),
    });
    return CaptainOrderModel.fromJson(_asMap(response));
  }

  Future<CaptainOrderModel> addItems(
    int orderId,
    Map<int, int> cart, {
    Map<int, String> notes = const {},
  }) async {
    final response = await _api.post('CaptainOrders/$orderId/items', {
      'items': _requestItems(cart, notes),
    });
    return CaptainOrderModel.fromJson(_asMap(response));
  }

  Future<CaptainOrderModel> requestBill(int orderId) async {
    final response = await _api.post('CaptainOrders/$orderId/request-bill');
    return CaptainOrderModel.fromJson(_asMap(response));
  }

  Future<void> markTableReady(int tableId) async {
    await _api.post('DiningTables/$tableId/mark-clean');
  }

  static List<Map<String, dynamic>> _requestItems(
    Map<int, int> cart,
    Map<int, String> notes,
  ) => cart.entries.where((entry) => entry.value > 0).map((entry) {
    final note = notes[entry.key]?.trim();
    return <String, dynamic>{
      'menuItemId': entry.key,
      'quantity': entry.value,
      if (note?.isNotEmpty == true) 'notes': note,
    };
  }).toList();

  static Map<String, dynamic> _asMap(dynamic value) {
    if (value is Map<String, dynamic>) return value;
    throw const ApiException('Unexpected server response.');
  }

  static List<Map<String, dynamic>> _asMapList(dynamic value) {
    if (value is! List) {
      throw const ApiException('Unexpected server response.');
    }
    return value.whereType<Map<String, dynamic>>().toList();
  }
}
