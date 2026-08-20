import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import 'captain_repository.dart';
import 'models.dart';

class OrderPage extends StatefulWidget {
  const OrderPage({
    super.key,
    required this.table,
    required this.initialOrder,
    required this.repository,
  });

  final DiningTableModel table;
  final CaptainOrderModel? initialOrder;
  final CaptainRepository repository;

  @override
  State<OrderPage> createState() => _OrderPageState();
}

class _OrderPageState extends State<OrderPage> {
  List<CategoryModel> _categories = const [];
  List<MenuItemModel> _menuItems = const [];
  final Map<int, int> _cart = {};
  final Map<int, String> _itemNotes = {};
  final Map<int, String> _servingStyles = {};
  final TextEditingController _searchController = TextEditingController();
  CaptainOrderModel? _order;
  int? _categoryId;
  String _searchText = '';
  late String _tableStatus;
  bool _busy = true;
  String? _error;

  bool get _canEdit =>
      _tableStatus == 'Available' || _tableStatus == 'Occupied';

  int get _cartCount => _cart.values.fold(0, (sum, value) => sum + value);

  double get _cartTotal => _cart.entries.fold(0, (sum, entry) {
    final item = _menuItems.where((x) => x.id == entry.key).firstOrNull;
    return sum + (item?.priceFor(widget.table) ?? 0) * entry.value;
  });

  List<MenuItemModel> get _filteredItems {
    final query = _searchText.trim().toLowerCase();
    return _menuItems.where((item) {
      final matchesCategory =
          _categoryId == null || item.categoryId == _categoryId;
      final matchesSearch =
          query.isEmpty ||
          item.name.toLowerCase().contains(query) ||
          (item.tamilName?.toLowerCase().contains(query) ?? false);
      return matchesCategory && matchesSearch;
    }).toList();
  }

  @override
  void initState() {
    super.initState();
    _order = widget.initialOrder;
    _tableStatus = widget.table.status;
    _loadMenu();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadMenu() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final result = await Future.wait([
        widget.repository.getCategories(),
        widget.repository.getMenuItems(),
      ]);
      if (mounted) {
        setState(() {
          _categories = result[0] as List<CategoryModel>;
          _menuItems = result[1] as List<MenuItemModel>;
        });
      }
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _changeQuantity(int menuItemId, int delta) {
    if (!_canEdit) return;
    setState(() {
      final next = (_cart[menuItemId] ?? 0) + delta;
      if (next <= 0) {
        _cart.remove(menuItemId);
        _itemNotes.remove(menuItemId);
        _servingStyles.remove(menuItemId);
      } else if (next <= 100) {
        _cart[menuItemId] = next;
      }
    });
  }

  bool _isSoup(MenuItemModel item) {
    final categoryName = _categories
        .where((category) => category.id == item.categoryId)
        .map((category) => category.name)
        .firstOrNull;
    final searchValue =
        '${categoryName ?? ''} ${item.name} ${item.tamilName ?? ''}'
            .toLowerCase();
    return searchValue.contains('soup');
  }

  String? _buildKotNote(int menuItemId) {
    final parts = <String>[];
    final servingStyle = _servingStyles[menuItemId]?.trim();
    final note = _itemNotes[menuItemId]?.trim();

    if (servingStyle?.isNotEmpty == true && servingStyle != 'Regular') {
      parts.add('Serving: $servingStyle');
    }
    if (note?.isNotEmpty == true) parts.add(note!);

    return parts.isEmpty ? null : parts.join(' | ');
  }

  String? _instructionSummary(int menuItemId) {
    final servingStyle = _servingStyles[menuItemId];
    final note = _itemNotes[menuItemId];
    final parts = <String>[
      if (servingStyle != null && servingStyle != 'Regular') servingStyle,
      if (note?.trim().isNotEmpty == true) note!.trim(),
    ];
    return parts.isEmpty ? null : parts.join(' • ');
  }

  Future<void> _editItemInstructions(MenuItemModel item) async {
    if (!_canEdit || (_cart[item.id] ?? 0) <= 0) return;

    final isSoup = _isSoup(item);
    final result = await showDialog<ItemInstructionsResult>(
      context: context,
      builder: (_) => ItemInstructionsDialog(
        itemName: item.name,
        isSoup: isSoup,
        initialServingStyle: _servingStyles[item.id] ?? 'Regular',
        initialNotes: _itemNotes[item.id] ?? '',
      ),
    );

    if (result == null || !mounted) return;
    setState(() {
      if (result.notes.isEmpty) {
        _itemNotes.remove(item.id);
      } else {
        _itemNotes[item.id] = result.notes;
      }

      if (!isSoup || result.servingStyle == 'Regular') {
        _servingStyles.remove(item.id);
      } else {
        _servingStyles[item.id] = result.servingStyle;
      }
    });
  }

  Future<void> _reviewCart() async {
    if (_cart.isEmpty || !_canEdit) return;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheetContext) => StatefulBuilder(
        builder: (context, setSheetState) {
          final lines = _cart.entries.toList();
          return SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    _order == null ? 'First KOT' : 'Additional KOT',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Flexible(
                    child: ListView.separated(
                      shrinkWrap: true,
                      itemCount: lines.length,
                      separatorBuilder: (_, _) => const Divider(),
                      itemBuilder: (_, index) {
                        final entry = lines[index];
                        final item = _menuItems.firstWhere(
                          (x) => x.id == entry.key,
                        );
                        final instructions = _instructionSummary(item.id);
                        return ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(item.name),
                          isThreeLine: true,
                          onTap: () async {
                            await _editItemInstructions(item);
                            setSheetState(() {});
                          },
                          subtitle: Text(
                            '₹ ${item.priceFor(widget.table).toStringAsFixed(2)}\n'
                            '${instructions ?? (_isSoup(item) ? 'Tap for serving style / note' : 'Tap to add a kitchen note')}',
                          ),
                          trailing: _QuantityControl(
                            quantity: entry.value,
                            onMinus: () {
                              _changeQuantity(item.id, -1);
                              setSheetState(() {});
                              if (_cart.isEmpty) Navigator.pop(sheetContext);
                            },
                            onPlus: () {
                              _changeQuantity(item.id, 1);
                              setSheetState(() {});
                            },
                          ),
                        );
                      },
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'New items total: ₹ ${_cartTotal.toStringAsFixed(2)}',
                    textAlign: TextAlign.right,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: _busy
                        ? null
                        : () async {
                            Navigator.pop(sheetContext);
                            await _sendKot();
                          },
                    icon: const Icon(Icons.receipt_long),
                    label: const Text('Confirm and send KOT'),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Future<void> _sendKot() async {
    if (_cart.isEmpty || _busy) return;
    setState(() => _busy = true);
    try {
      final kotNotes = <int, String>{};
      for (final menuItemId in _cart.keys) {
        final note = _buildKotNote(menuItemId);
        if (note != null) kotNotes[menuItemId] = note;
      }

      final result = _order == null
          ? await widget.repository.createOrder(
              widget.table.id,
              Map.of(_cart),
              notes: kotNotes,
            )
          : await widget.repository.addItems(
              _order!.orderId,
              Map.of(_cart),
              notes: kotNotes,
            );

      if (!mounted) return;
      setState(() {
        _order = result;
        _tableStatus = 'Occupied';
        _cart.clear();
        _itemNotes.clear();
        _servingStyles.clear();
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${result.kotNumber} sent to kitchen.')),
      );
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _requestBill() async {
    final order = _order;
    if (order == null || _busy || _tableStatus != 'Occupied') return;

    final confirmed = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        title: const Text('Request bill?'),
        content: Text(
          'Table ${widget.table.tableNumber} will be locked for payment. '
          'Print the customer bill now?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Print Bill'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _busy = true);
    try {
      await widget.repository.requestBill(order.orderId);
      if (!mounted) return;
      setState(() => _tableStatus = 'BillRequested');
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer bill queued for printing.')),
      );
      Navigator.pop(context);
    } on ApiException catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.message)));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _showCurrentOrder() async {
    final order = _order;
    if (order == null) return;
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Bill ${order.billNumber}',
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
              Text('${order.items.length} line(s) • ${order.captainName}'),
              const SizedBox(height: 12),
              Flexible(
                child: ListView.separated(
                  shrinkWrap: true,
                  itemCount: order.items.length,
                  separatorBuilder: (_, _) => const Divider(),
                  itemBuilder: (_, index) {
                    final item = order.items[index];
                    return ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(item.itemName),
                      subtitle: item.notes == null ? null : Text(item.notes!),
                      trailing: Text(
                        '${item.quantity} × ₹ ${item.price.toStringAsFixed(2)}',
                      ),
                    );
                  },
                ),
              ),
              const Divider(),
              Text(
                'Total ₹ ${order.grandTotal.toStringAsFixed(2)}',
                textAlign: TextAlign.right,
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Table ${widget.table.tableNumber}'),
            Text(
              _tableStatus == 'BillRequested'
                  ? 'Waiting for Cashier to print the bill'
                  : _tableStatus == 'PaymentPending'
                  ? 'Waiting for Cashier payment'
                  : _order == null
                  ? 'New table order'
                  : 'Add items or request bill',
              style: const TextStyle(fontSize: 12),
            ),
          ],
        ),
        actions: [
          if (_order != null)
            IconButton(
              tooltip: 'Current order',
              onPressed: _showCurrentOrder,
              icon: const Icon(Icons.receipt_long),
            ),
        ],
      ),
      body: _busy && _menuItems.isEmpty
          ? const Center(child: CircularProgressIndicator())
          : _error != null
          ? Center(
              child: FilledButton(
                onPressed: _loadMenu,
                child: Text('Retry: $_error'),
              ),
            )
          : Column(
              children: [
                if (_order != null)
                  Material(
                    color: const Color(0xFFE0F2FE),
                    child: ListTile(
                      onTap: _showCurrentOrder,
                      leading: const Icon(Icons.receipt_long),
                      title: Text('Bill ${_order!.billNumber}'),
                      subtitle: Text(
                        '${_order!.items.length} existing line(s)',
                      ),
                      trailing: Text(
                        '₹ ${_order!.grandTotal.toStringAsFixed(2)}',
                        style: const TextStyle(fontWeight: FontWeight.w800),
                      ),
                    ),
                  ),
                if (_tableStatus == 'BillRequested' ||
                    _tableStatus == 'PaymentPending')
                  Material(
                    color: const Color(0xFFFEE2E2),
                    child: ListTile(
                      leading: const Icon(Icons.lock, color: Color(0xFFDC2626)),
                      title: Text(
                        _tableStatus == 'BillRequested'
                            ? 'Bill requested — waiting for Cashier print'
                            : 'Bill printed — waiting for payment',
                      ),
                    ),
                  ),
                Padding(
                  padding: const EdgeInsets.fromLTRB(12, 10, 12, 2),
                  child: TextField(
                    controller: _searchController,
                    enabled: _canEdit,
                    textInputAction: TextInputAction.search,
                    onChanged: (value) => setState(() => _searchText = value),
                    decoration: InputDecoration(
                      hintText: 'Search menu items',
                      prefixIcon: const Icon(Icons.search),
                      suffixIcon: _searchText.isEmpty
                          ? null
                          : IconButton(
                              tooltip: 'Clear search',
                              onPressed: () {
                                FocusScope.of(context).unfocus();
                                _searchController.clear();
                                setState(() => _searchText = '');
                              },
                              icon: const Icon(Icons.close),
                            ),
                      border: const OutlineInputBorder(),
                      isDense: true,
                    ),
                  ),
                ),
                SizedBox(
                  height: 58,
                  child: ListView(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.all(8),
                    children: [
                      ChoiceChip(
                        label: const Text('All'),
                        selected: _categoryId == null,
                        onSelected: (_) => setState(() => _categoryId = null),
                      ),
                      const SizedBox(width: 8),
                      ..._categories.map(
                        (category) => Padding(
                          padding: const EdgeInsets.only(right: 8),
                          child: ChoiceChip(
                            label: Text(category.name),
                            selected: _categoryId == category.id,
                            onSelected: (_) =>
                                setState(() => _categoryId = category.id),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: GridView.builder(
                    padding: const EdgeInsets.all(12),
                    gridDelegate:
                        const SliverGridDelegateWithMaxCrossAxisExtent(
                          maxCrossAxisExtent: 220,
                          mainAxisExtent: 185,
                          crossAxisSpacing: 10,
                          mainAxisSpacing: 10,
                        ),
                    itemCount: _filteredItems.length,
                    itemBuilder: (_, index) {
                      final item = _filteredItems[index];
                      final quantity = _cart[item.id] ?? 0;
                      final instructions = _instructionSummary(item.id);
                      return Card(
                        color: Colors.white,
                        clipBehavior: Clip.antiAlias,
                        child: InkWell(
                          onTap: _canEdit
                              ? () => _changeQuantity(item.id, 1)
                              : null,
                          child: Padding(
                            padding: const EdgeInsets.all(12),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  item.name,
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                                if (item.tamilName?.isNotEmpty == true)
                                  Text(
                                    item.tamilName!,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                if (instructions != null)
                                  Text(
                                    instructions,
                                    maxLines: 2,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                      color: Color(0xFFB45309),
                                      fontSize: 11,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                const Spacer(),
                                Row(
                                  children: [
                                    Text(
                                      '₹ ${item.priceFor(widget.table).toStringAsFixed(2)}',
                                      style: const TextStyle(
                                        color: Color(0xFF15803D),
                                        fontWeight: FontWeight.w800,
                                      ),
                                    ),
                                    const Spacer(),
                                    if (quantity > 0)
                                      IconButton(
                                        tooltip: _isSoup(item)
                                            ? 'Serving style / kitchen note'
                                            : 'Kitchen note',
                                        visualDensity: VisualDensity.compact,
                                        onPressed: () =>
                                            _editItemInstructions(item),
                                        icon: Icon(
                                          instructions == null
                                              ? Icons.edit_note_outlined
                                              : Icons.edit_note,
                                          color: const Color(0xFF7C3AED),
                                        ),
                                      ),
                                  ],
                                ),
                                const SizedBox(height: 3),
                                Align(
                                  alignment: Alignment.centerRight,
                                  child: _InlineQuantityControl(
                                    quantity: quantity,
                                    enabled: _canEdit,
                                    onMinus: () => _changeQuantity(item.id, -1),
                                    onPlus: () => _changeQuantity(item.id, 1),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              if (_order != null && _tableStatus == 'Occupied') ...[
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _busy ? null : _requestBill,
                    icon: const Icon(Icons.point_of_sale),
                    label: const Text('Request bill'),
                  ),
                ),
                const SizedBox(width: 10),
              ],
              Expanded(
                flex: 2,
                child: FilledButton.icon(
                  onPressed: _cart.isEmpty || !_canEdit || _busy
                      ? null
                      : _reviewCart,
                  icon: const Icon(Icons.shopping_cart_checkout),
                  label: Text(
                    _cart.isEmpty
                        ? 'Select menu items'
                        : 'Review KOT • $_cartCount • ₹ ${_cartTotal.toStringAsFixed(2)}',
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class ItemInstructionsResult {
  const ItemInstructionsResult({
    required this.servingStyle,
    required this.notes,
  });

  final String servingStyle;
  final String notes;
}

class ItemInstructionsDialog extends StatefulWidget {
  const ItemInstructionsDialog({
    super.key,
    required this.itemName,
    required this.isSoup,
    required this.initialServingStyle,
    required this.initialNotes,
  });

  final String itemName;
  final bool isSoup;
  final String initialServingStyle;
  final String initialNotes;

  @override
  State<ItemInstructionsDialog> createState() => _ItemInstructionsDialogState();
}

class _ItemInstructionsDialogState extends State<ItemInstructionsDialog> {
  static const _servingStyles = ['Regular', '1 by 2', '1 by 3', '1 by 4'];

  late final TextEditingController _notesController;
  late String _servingStyle;

  @override
  void initState() {
    super.initState();
    _notesController = TextEditingController(text: widget.initialNotes);
    _servingStyle = _servingStyles.contains(widget.initialServingStyle)
        ? widget.initialServingStyle
        : 'Regular';
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  void _save() {
    Navigator.of(context).pop(
      ItemInstructionsResult(
        servingStyle: _servingStyle,
        notes: _notesController.text.trim(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: Text('${widget.itemName} instructions'),
    content: SingleChildScrollView(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (widget.isSoup) ...[
            Text(
              'Soup serving style',
              style: Theme.of(
                context,
              ).textTheme.labelLarge?.copyWith(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                for (final style in _servingStyles)
                  ChoiceChip(
                    label: Text(style),
                    selected: _servingStyle == style,
                    onSelected: (_) => setState(() => _servingStyle = style),
                  ),
              ],
            ),
            const SizedBox(height: 16),
          ],
          TextField(
            controller: _notesController,
            autofocus: !widget.isSoup,
            maxLength: 180,
            minLines: 2,
            maxLines: 4,
            textInputAction: TextInputAction.done,
            onSubmitted: (_) => _save(),
            decoration: const InputDecoration(
              labelText: 'Kitchen note (optional)',
              hintText: 'Example: less salt, no onion',
              prefixIcon: Icon(Icons.edit_note),
              border: OutlineInputBorder(),
            ),
          ),
        ],
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.of(context).pop(),
        child: const Text('Cancel'),
      ),
      FilledButton(onPressed: _save, child: const Text('Save')),
    ],
  );
}

class _InlineQuantityControl extends StatelessWidget {
  const _InlineQuantityControl({
    required this.quantity,
    required this.enabled,
    required this.onMinus,
    required this.onPlus,
  });

  final int quantity;
  final bool enabled;
  final VoidCallback onMinus;
  final VoidCallback onPlus;

  @override
  Widget build(BuildContext context) => DecoratedBox(
    decoration: BoxDecoration(
      color: const Color(0xFFF1F5F9),
      borderRadius: BorderRadius.circular(18),
      border: Border.all(color: const Color(0xFFCBD5E1)),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        IconButton(
          tooltip: 'Decrease quantity',
          onPressed: enabled && quantity > 0 ? onMinus : null,
          visualDensity: VisualDensity.compact,
          constraints: const BoxConstraints.tightFor(width: 34, height: 34),
          padding: EdgeInsets.zero,
          icon: const Icon(Icons.remove, size: 19),
        ),
        SizedBox(
          width: 30,
          child: Text(
            '$quantity',
            textAlign: TextAlign.center,
            style: const TextStyle(fontWeight: FontWeight.w800),
          ),
        ),
        IconButton(
          tooltip: 'Increase quantity',
          onPressed: enabled ? onPlus : null,
          visualDensity: VisualDensity.compact,
          constraints: const BoxConstraints.tightFor(width: 34, height: 34),
          padding: EdgeInsets.zero,
          style: IconButton.styleFrom(
            backgroundColor: const Color(0xFF2563EB),
            foregroundColor: Colors.white,
            disabledBackgroundColor: const Color(0xFFCBD5E1),
          ),
          icon: const Icon(Icons.add, size: 19),
        ),
      ],
    ),
  );
}

class _QuantityControl extends StatelessWidget {
  const _QuantityControl({
    required this.quantity,
    required this.onMinus,
    required this.onPlus,
  });

  final int quantity;
  final VoidCallback onMinus;
  final VoidCallback onPlus;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      IconButton.filledTonal(
        onPressed: onMinus,
        icon: const Icon(Icons.remove),
      ),
      SizedBox(
        width: 36,
        child: Text(
          '$quantity',
          textAlign: TextAlign.center,
          style: const TextStyle(fontWeight: FontWeight.w800),
        ),
      ),
      IconButton.filled(onPressed: onPlus, icon: const Icon(Icons.add)),
    ],
  );
}
