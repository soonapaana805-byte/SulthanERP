class DiningTableModel {
  const DiningTableModel({
    required this.id,
    required this.tableNumber,
    required this.tableType,
    required this.capacity,
    required this.status,
    required this.displayOrder,
    required this.isActive,
  });

  factory DiningTableModel.fromJson(Map<String, dynamic> json) {
    return DiningTableModel(
      id: _int(json['id']),
      tableNumber: json['tableNumber']?.toString() ?? '',
      tableType: json['tableType']?.toString() ?? '',
      capacity: _int(json['capacity']),
      status: json['status']?.toString() ?? 'Available',
      displayOrder: _int(json['displayOrder']),
      isActive: json['isActive'] != false,
    );
  }

  final int id;
  final String tableNumber;
  final String tableType;
  final int capacity;
  final String status;
  final int displayOrder;
  final bool isActive;
}

class CategoryModel {
  const CategoryModel({
    required this.id,
    required this.name,
    required this.displayOrder,
    required this.isActive,
  });

  factory CategoryModel.fromJson(Map<String, dynamic> json) => CategoryModel(
    id: _int(json['id']),
    name: json['name']?.toString() ?? '',
    displayOrder: _int(json['displayOrder']),
    isActive: json['isActive'] != false,
  );

  final int id;
  final String name;
  final int displayOrder;
  final bool isActive;
}

class MenuItemModel {
  const MenuItemModel({
    required this.id,
    required this.name,
    required this.tamilName,
    required this.categoryId,
    required this.acPrice,
    required this.nonAcPrice,
    required this.isAvailable,
    required this.displayOrder,
  });

  factory MenuItemModel.fromJson(Map<String, dynamic> json) => MenuItemModel(
    id: _int(json['id']),
    name: json['name']?.toString() ?? '',
    tamilName: json['tamilName']?.toString(),
    categoryId: _int(json['categoryId']),
    acPrice: _double(json['acPrice']),
    nonAcPrice: _double(json['nonACPrice']),
    isAvailable: json['isAvailable'] == true && json['isActive'] != false,
    displayOrder: _int(json['displayOrder']),
  );

  final int id;
  final String name;
  final String? tamilName;
  final int categoryId;
  final double acPrice;
  final double nonAcPrice;
  final bool isAvailable;
  final int displayOrder;

  double priceFor(DiningTableModel table) =>
      table.tableType.toUpperCase() == 'AC' ? acPrice : nonAcPrice;
}

class CaptainOrderModel {
  const CaptainOrderModel({
    required this.orderId,
    required this.billNumber,
    required this.tableNumber,
    required this.captainName,
    required this.grandTotal,
    required this.kotNumber,
    required this.items,
  });

  factory CaptainOrderModel.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'];
    return CaptainOrderModel(
      orderId: _int(json['orderId']),
      billNumber: json['billNumber']?.toString() ?? '',
      tableNumber: json['tableNumber']?.toString() ?? '',
      captainName: json['captainName']?.toString() ?? '',
      grandTotal: _double(json['grandTotal']),
      kotNumber: json['kitchenTicketNumber']?.toString() ?? '',
      items: rawItems is List
          ? rawItems
                .whereType<Map<String, dynamic>>()
                .map(CaptainOrderItemModel.fromJson)
                .toList()
          : const [],
    );
  }

  final int orderId;
  final String billNumber;
  final String tableNumber;
  final String captainName;
  final double grandTotal;
  final String kotNumber;
  final List<CaptainOrderItemModel> items;
}

class CaptainOrderItemModel {
  const CaptainOrderItemModel({
    required this.menuItemId,
    required this.itemName,
    required this.price,
    required this.quantity,
    required this.notes,
  });

  factory CaptainOrderItemModel.fromJson(Map<String, dynamic> json) =>
      CaptainOrderItemModel(
        menuItemId: _int(json['menuItemId']),
        itemName: json['itemName']?.toString() ?? '',
        price: _double(json['price']),
        quantity: _int(json['quantity']),
        notes: json['notes']?.toString(),
      );

  final int menuItemId;
  final String itemName;
  final double price;
  final int quantity;
  final String? notes;
}

int _int(dynamic value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  return int.tryParse(value?.toString() ?? '') ?? 0;
}

double _double(dynamic value) {
  if (value is num) return value.toDouble();
  return double.tryParse(value?.toString() ?? '') ?? 0;
}
