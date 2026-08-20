import 'package:flutter/material.dart';

import '../../core/api/api_client.dart';
import '../auth/session.dart';
import 'captain_repository.dart';
import 'models.dart';
import 'order_page.dart';

class TableDashboardPage extends StatefulWidget {
  const TableDashboardPage({
    super.key,
    required this.session,
    required this.onLogout,
  });

  final CaptainSession session;
  final VoidCallback onLogout;

  @override
  State<TableDashboardPage> createState() => _TableDashboardPageState();
}

class _TableDashboardPageState extends State<TableDashboardPage> {
  late final ApiClient _api;
  late final CaptainRepository _repository;
  List<DiningTableModel> _tables = const [];
  bool _busy = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _api = ApiClient(
      baseUrl: widget.session.baseUrl,
      token: widget.session.token,
    );
    _repository = CaptainRepository(_api);
    _load();
  }

  @override
  void dispose() {
    _api.close();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _busy = true;
      _error = null;
    });

    try {
      final tables = await _repository.getTables();
      if (mounted) setState(() => _tables = tables);
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _openTable(DiningTableModel table) async {
    if (table.status == 'CleaningPending') {
      await _markTableReady(table);
      return;
    }

    if (table.status == 'Reserved') {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(_statusHelp(table.status))));
      return;
    }

    CaptainOrderModel? order;
    if (table.status != 'Available') {
      try {
        order = await _repository.getTableOrder(table.id);
      } on ApiException catch (error) {
        if (mounted) {
          ScaffoldMessenger.of(
            context,
          ).showSnackBar(SnackBar(content: Text(error.message)));
        }
        return;
      }
    }

    if (!mounted) return;
    await Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => OrderPage(
          table: table,
          initialOrder: order,
          repository: _repository,
        ),
      ),
    );
    await _load();
  }

  Future<void> _markTableReady(DiningTableModel table) async {
    if (_busy) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Table cleaned?'),
        content: Text(
          'Confirm that table ${table.tableNumber} has been cleaned and is ready '
          'for the next customer.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Mark table ready'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _busy = true);
    try {
      await _repository.markTableReady(table.id);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Table ${table.tableNumber} is now available.')),
      );
      await _load();
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('SULTHAN ERP', style: TextStyle(fontWeight: FontWeight.w800)),
            Text('Captain • Table status', style: TextStyle(fontSize: 12)),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            onPressed: _busy ? null : _load,
            icon: const Icon(Icons.refresh),
          ),
          PopupMenuButton<String>(
            onSelected: (value) {
              if (value == 'logout') widget.onLogout();
            },
            itemBuilder: (_) => [
              PopupMenuItem(
                enabled: false,
                child: Text(widget.session.fullName),
              ),
              const PopupMenuItem(value: 'logout', child: Text('Sign out')),
            ],
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: CustomScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          slivers: [
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
                child: Wrap(
                  spacing: 12,
                  runSpacing: 8,
                  children: const [
                    _Legend('Available', Color(0xFF16A34A)),
                    _Legend('Occupied', Color(0xFFF59E0B)),
                    _Legend('Bill requested', Color(0xFFDC2626)),
                    _Legend('Payment pending', Color(0xFF4F46E5)),
                    _Legend('Cleaning', Color(0xFF0284C7)),
                    _Legend('Reserved', Color(0xFF64748B)),
                  ],
                ),
              ),
            ),
            if (_busy)
              const SliverFillRemaining(
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_error != null)
              SliverFillRemaining(
                hasScrollBody: false,
                child: _ErrorView(message: _error!, retry: _load),
              )
            else if (_tables.isEmpty)
              const SliverFillRemaining(
                hasScrollBody: false,
                child: Center(child: Text('No dining tables configured.')),
              )
            else
              SliverPadding(
                padding: const EdgeInsets.all(16),
                sliver: SliverGrid.builder(
                  itemCount: _tables.length,
                  gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                    maxCrossAxisExtent: 220,
                    mainAxisExtent: 150,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                  ),
                  itemBuilder: (context, index) {
                    final table = _tables[index];
                    final color = _statusColor(table.status);
                    return Card(
                      clipBehavior: Clip.antiAlias,
                      child: InkWell(
                        onTap: () => _openTable(table),
                        child: Container(
                          decoration: BoxDecoration(
                            border: Border(
                              top: BorderSide(color: color, width: 7),
                            ),
                          ),
                          padding: const EdgeInsets.all(14),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'TABLE ${table.tableNumber}',
                                style: Theme.of(context).textTheme.titleLarge
                                    ?.copyWith(fontWeight: FontWeight.w800),
                              ),
                              const Spacer(),
                              Text(
                                '${table.tableType} • ${table.capacity} seats',
                              ),
                              const SizedBox(height: 6),
                              Text(
                                _statusLabel(table.status),
                                style: TextStyle(
                                  color: color,
                                  fontWeight: FontWeight.w700,
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
      ),
    );
  }
}

class _Legend extends StatelessWidget {
  const _Legend(this.label, this.color);
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      CircleAvatar(radius: 5, backgroundColor: color),
      const SizedBox(width: 5),
      Text(label),
    ],
  );
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.retry});
  final String message;
  final VoidCallback retry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off, size: 44),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 16),
          FilledButton(onPressed: retry, child: const Text('Try again')),
        ],
      ),
    ),
  );
}

Color _statusColor(String status) => switch (status) {
  'Available' => const Color(0xFF16A34A),
  'Occupied' => const Color(0xFFF59E0B),
  'BillRequested' => const Color(0xFFDC2626),
  'PaymentPending' => const Color(0xFF4F46E5),
  'CleaningPending' => const Color(0xFF0284C7),
  _ => const Color(0xFF64748B),
};

String _statusLabel(String status) => switch (status) {
  'BillRequested' => 'BILL REQUESTED',
  'PaymentPending' => 'PAYMENT PENDING',
  'CleaningPending' => 'CLEANING PENDING',
  _ => status.toUpperCase(),
};

String _statusHelp(String status) => switch (status) {
  'CleaningPending' =>
    'Payment completed. Tap the table after cleaning to mark it ready.',
  'Reserved' => 'This table is reserved.',
  _ => status,
};
