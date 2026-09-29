import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';
import '../services/crop_activity_service.dart';
import '../widgets/activity_card.dart';
import 'edit_activity_screen.dart';

/// Screen enabling farmers to view, filter, search, inspect, and delete
/// their past recorded crop activities.
class PastActivitiesScreen extends StatefulWidget {
  final int? initialCycleId;

  const PastActivitiesScreen({
    super.key,
    this.initialCycleId,
  });

  @override
  State<PastActivitiesScreen> createState() => _PastActivitiesScreenState();
}

class _PastActivitiesScreenState extends State<PastActivitiesScreen> {
  bool _isLoading = true;
  String? _errorMessage;

  List<CultivationCycleSummary> _cycles = [];
  int? _selectedCycleId;

  String _selectedCategory = 'All';
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

  List<CropActivityDto> _allActivities = [];

  final List<String> _categories = [
    'All',
    'Fertilizer',
    'Irrigation',
    'Pesticide',
    'Other',
  ];

  @override
  void initState() {
    super.initState();
    _selectedCycleId = widget.initialCycleId;
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text.trim().toLowerCase();
      });
    });
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final cycles = await CropActivityService.getCycles();
      final activities = await CropActivityService.getAllActivities(
        cycleId: _selectedCycleId,
      );

      if (mounted) {
        setState(() {
          _cycles = cycles;
          _allActivities = activities;
          _isLoading = false;
        });
      }
    } catch (err) {
      if (mounted) {
        setState(() {
          _errorMessage = 'Failed to load activities: $err';
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _handleDelete(CropActivityDto activity) async {
    try {
      await CropActivityService.deleteActivity(activity.id);
      setState(() {
        _allActivities.removeWhere((a) => a.id == activity.id);
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${activity.activityType} activity removed.'),
            backgroundColor: AppColors.forest,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } catch (err) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to delete activity: $err'),
            backgroundColor: AppColors.errorText,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    }
  }

  Future<void> _handleEdit(CropActivityDto activity) async {
    CropActivityDto? result;
    try {
      result = await context.push<CropActivityDto>(
        '/activities/${activity.id}/edit',
        extra: activity,
      );
    } catch (_) {
      if (!mounted) return;
      result = await Navigator.of(context).push<CropActivityDto>(
        MaterialPageRoute(
          builder: (_) => EditActivityScreen(
            activityId: activity.id,
            initialActivity: activity,
          ),
        ),
      );
    }

    if (result != null && mounted) {
      setState(() {
        final index = _allActivities.indexWhere((a) => a.id == result!.id);
        if (index != -1) {
          _allActivities[index] = result!;
        } else {
          _allActivities.insert(0, result!);
        }
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('${result.activityType} activity updated successfully.'),
          backgroundColor: AppColors.forest,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  List<CropActivityDto> get _filteredActivities {
    return _allActivities.where((activity) {
      // Category filter
      if (_selectedCategory != 'All' &&
          activity.activityType.toLowerCase() != _selectedCategory.toLowerCase()) {
        return false;
      }

      // Cycle filter (if specified)
      if (_selectedCycleId != null &&
          activity.cultivationCycleId != _selectedCycleId) {
        return false;
      }

      // Keyword search
      if (_searchQuery.isNotEmpty) {
        final field = (activity.fieldName ?? '').toLowerCase();
        final cycle = (activity.cycleName ?? '').toLowerCase();
        final logger = activity.loggedByUserName.toLowerCase();
        final details = activity.detailsJson.toLowerCase();
        final type = activity.activityType.toLowerCase();

        final matches = field.contains(_searchQuery) ||
            cycle.contains(_searchQuery) ||
            logger.contains(_searchQuery) ||
            details.contains(_searchQuery) ||
            type.contains(_searchQuery);

        if (!matches) return false;
      }

      return true;
    }).toList();
  }

  Map<String, int> get _categoryCounts {
    final map = <String, int>{
      'All': _allActivities.length,
      'Fertilizer': 0,
      'Irrigation': 0,
      'Pesticide': 0,
      'Other': 0,
    };
    for (final act in _allActivities) {
      final t = act.activityType;
      if (map.containsKey(t)) {
        map[t] = (map[t] ?? 0) + 1;
      } else {
        map['Other'] = (map['Other'] ?? 0) + 1;
      }
    }
    return map;
  }

  @override
  Widget build(BuildContext context) {
    final filtered = _filteredActivities;
    final counts = _categoryCounts;

    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: AppBar(
        backgroundColor: AppColors.forest,
        foregroundColor: AppColors.cream,
        elevation: 0,
        title: const Text(
          'Crop Activities',
          style: TextStyle(
            fontFamily: 'serif',
            fontWeight: FontWeight.bold,
            fontSize: 20,
          ),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.auto_awesome_rounded, color: AppColors.gold),
            tooltip: 'AI Field Advisor',
            onPressed: () {
              final targetCycle = _selectedCycleId ?? (_cycles.isNotEmpty ? _cycles.first.id : null);
              final route = targetCycle != null
                  ? '/activities/advisor?cycleId=$targetCycle'
                  : '/activities/advisor';
              context.push(route);
            },
          ),
          IconButton(
            icon: const Icon(Icons.add_circle_outline_rounded),
            tooltip: 'Record Activity',
            onPressed: () async {
              final targetCycle = _selectedCycleId ?? (_cycles.isNotEmpty ? _cycles.first.id : null);
              final route = targetCycle != null
                  ? '/activities/new?cycleId=$targetCycle'
                  : '/activities/new';
              await context.push(route);
              _loadData();
            },
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: AppColors.forest,
        foregroundColor: AppColors.cream,
        icon: const Icon(Icons.add_task_rounded),
        label: const Text(
          'Record Activity',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        onPressed: () async {
          final targetCycle = _selectedCycleId ?? (_cycles.isNotEmpty ? _cycles.first.id : null);
          final route = targetCycle != null
              ? '/activities/new?cycleId=$targetCycle'
              : '/activities/new';
          await context.push(route);
          _loadData();
        },
      ),
      body: RefreshIndicator(
        onRefresh: _loadData,
        color: AppColors.forest,
        backgroundColor: AppColors.cream,
        child: _isLoading
            ? const Center(
                child: CircularProgressIndicator(color: AppColors.forest),
              )
            : _errorMessage != null
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.error_outline_rounded,
                              size: 48, color: AppColors.errorText),
                          const SizedBox(height: 12),
                          Text(
                            _errorMessage!,
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                              fontSize: 14,
                              color: AppColors.inkSoft,
                            ),
                          ),
                          const SizedBox(height: 16),
                          ElevatedButton(
                            onPressed: _loadData,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppColors.forest,
                              foregroundColor: AppColors.cream,
                            ),
                            child: const Text('Try Again'),
                          ),
                        ],
                      ),
                    ),
                  )
                : CustomScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    slivers: [
                      // Top filter header section
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // Cycle Selector Dropdown
                              if (_cycles.isNotEmpty) ...[
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 14, vertical: 4),
                                  decoration: BoxDecoration(
                                    color: Colors.white,
                                    borderRadius: BorderRadius.circular(10),
                                    border: Border.all(color: AppColors.line),
                                  ),
                                  child: DropdownButtonHideUnderline(
                                    child: DropdownButton<int?>(
                                      value: _selectedCycleId,
                                      isExpanded: true,
                                      hint: const Text(
                                        'All Cultivation Cycles',
                                        style: TextStyle(
                                          fontSize: 13,
                                          fontWeight: FontWeight.bold,
                                          color: AppColors.forest,
                                        ),
                                      ),
                                      icon: const Icon(Icons.arrow_drop_down,
                                          color: AppColors.forest),
                                      items: [
                                        const DropdownMenuItem<int?>(
                                          value: null,
                                          child: Text(
                                            'All Cultivation Cycles',
                                            style: TextStyle(
                                              fontSize: 13,
                                              fontWeight: FontWeight.bold,
                                              color: AppColors.forest,
                                            ),
                                          ),
                                        ),
                                        ..._cycles.map(
                                          (cycle) => DropdownMenuItem<int?>(
                                            value: cycle.id,
                                            child: Text(
                                              '${cycle.fieldName} · ${cycle.displayName}',
                                              style: const TextStyle(
                                                fontSize: 13,
                                                color: AppColors.ink,
                                              ),
                                            ),
                                          ),
                                        ),
                                      ],
                                      onChanged: (val) {
                                        setState(() {
                                          _selectedCycleId = val;
                                        });
                                      },
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 12),
                              ],

                              // Search Bar
                              Container(
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(10),
                                  border: Border.all(color: AppColors.line),
                                ),
                                child: TextField(
                                  controller: _searchController,
                                  decoration: InputDecoration(
                                    hintText:
                                        'Search activities, fertilizers, pests...',
                                    hintStyle: const TextStyle(
                                      fontSize: 13,
                                      color: AppColors.inkSoft,
                                    ),
                                    prefixIcon: const Icon(
                                      Icons.search,
                                      size: 20,
                                      color: AppColors.inkSoft,
                                    ),
                                    suffixIcon: _searchQuery.isNotEmpty
                                        ? IconButton(
                                            icon: const Icon(Icons.clear, size: 18),
                                            onPressed: () {
                                              _searchController.clear();
                                            },
                                          )
                                        : null,
                                    border: InputBorder.none,
                                    contentPadding:
                                        const EdgeInsets.symmetric(vertical: 12),
                                  ),
                                ),
                              ),
                              const SizedBox(height: 12),

                              // AI Field Advisor Quick Banner
                              InkWell(
                                onTap: () {
                                  final targetCycle = _selectedCycleId ??
                                      (_cycles.isNotEmpty
                                          ? _cycles.first.id
                                          : null);
                                  final route = targetCycle != null
                                      ? '/activities/advisor?cycleId=$targetCycle'
                                      : '/activities/advisor';
                                  context.push(route);
                                },
                                borderRadius: BorderRadius.circular(10),
                                child: Container(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 12, vertical: 10),
                                  decoration: BoxDecoration(
                                    color: AppColors.forestDeep,
                                    borderRadius: BorderRadius.circular(10),
                                    boxShadow: [
                                      BoxShadow(
                                        color: Colors.black.withAlpha(20),
                                        blurRadius: 6,
                                        offset: const Offset(0, 2),
                                      ),
                                    ],
                                  ),
                                  child: Row(
                                    children: [
                                      Container(
                                        padding: const EdgeInsets.all(6),
                                        decoration: BoxDecoration(
                                          color: AppColors.gold.withAlpha(30),
                                          shape: BoxShape.circle,
                                        ),
                                        child: const Icon(
                                            Icons.auto_awesome_rounded,
                                            size: 16,
                                            color: AppColors.gold),
                                      ),
                                      const SizedBox(width: 10),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: const [
                                            Text(
                                              'AI Field Advisor Available',
                                              style: TextStyle(
                                                fontSize: 13,
                                                fontWeight: FontWeight.bold,
                                                color: AppColors.cream,
                                              ),
                                            ),
                                            Text(
                                              'View DOA diagnostics & stage recommendations',
                                              style: TextStyle(
                                                fontSize: 11,
                                                color: AppColors.shootLight,
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                      Container(
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 8, vertical: 4),
                                        decoration: BoxDecoration(
                                          color: AppColors.gold,
                                          borderRadius:
                                              BorderRadius.circular(6),
                                        ),
                                        child: const Text(
                                          'Explore AI',
                                          style: TextStyle(
                                            fontSize: 10,
                                            fontWeight: FontWeight.bold,
                                            color: AppColors.forestDeep,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                              const SizedBox(height: 12),

                              // Category Filter Pills
                              SizedBox(
                                height: 38,
                                child: ListView.separated(
                                  scrollDirection: Axis.horizontal,
                                  itemCount: _categories.length,
                                  separatorBuilder: (_, __) =>
                                      const SizedBox(width: 8),
                                  itemBuilder: (ctx, idx) {
                                    final cat = _categories[idx];
                                    final isSelected = _selectedCategory == cat;
                                    final count = counts[cat] ?? 0;

                                    return InkWell(
                                      onTap: () {
                                        setState(() {
                                          _selectedCategory = cat;
                                        });
                                      },
                                      borderRadius: BorderRadius.circular(20),
                                      child: Container(
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 14, vertical: 8),
                                        decoration: BoxDecoration(
                                          color: isSelected
                                              ? AppColors.forest
                                              : Colors.white,
                                          borderRadius:
                                              BorderRadius.circular(20),
                                          border: Border.all(
                                            color: isSelected
                                                ? AppColors.forest
                                                : AppColors.line,
                                          ),
                                        ),
                                        child: Row(
                                          mainAxisSize: MainAxisSize.min,
                                          children: [
                                            Text(
                                              cat,
                                              style: TextStyle(
                                                fontSize: 12,
                                                fontWeight: isSelected
                                                    ? FontWeight.bold
                                                    : FontWeight.w500,
                                                color: isSelected
                                                    ? AppColors.cream
                                                    : AppColors.ink,
                                              ),
                                            ),
                                            const SizedBox(width: 6),
                                            Container(
                                              padding: const EdgeInsets.symmetric(
                                                  horizontal: 6, vertical: 1),
                                              decoration: BoxDecoration(
                                                color: isSelected
                                                    ? AppColors.cream.withAlpha(50)
                                                    : AppColors.creamDeep,
                                                borderRadius:
                                                    BorderRadius.circular(10),
                                              ),
                                              child: Text(
                                                '$count',
                                                style: TextStyle(
                                                  fontSize: 10,
                                                  fontWeight: FontWeight.bold,
                                                  color: isSelected
                                                      ? AppColors.cream
                                                      : AppColors.inkSoft,
                                                ),
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                    );
                                  },
                                ),
                              ),
                              const SizedBox(height: 16),
                            ],
                          ),
                        ),
                      ),

                      // Activities List / Empty State
                      if (filtered.isEmpty)
                        SliverFillRemaining(
                          hasScrollBody: false,
                          child: Center(
                            child: Padding(
                              padding: const EdgeInsets.all(24),
                              child: Column(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  Container(
                                    padding: const EdgeInsets.all(20),
                                    decoration: BoxDecoration(
                                      color: AppColors.forest.withAlpha(20),
                                      shape: BoxShape.circle,
                                    ),
                                    child: const Icon(
                                      Icons.history_edu_rounded,
                                      size: 48,
                                      color: AppColors.forest,
                                    ),
                                  ),
                                  const SizedBox(height: 16),
                                  const Text(
                                    'No Activities Found',
                                    style: TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.bold,
                                      color: AppColors.ink,
                                      fontFamily: 'serif',
                                    ),
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    _searchQuery.isNotEmpty ||
                                            _selectedCategory != 'All'
                                        ? 'No records match your selected filters. Try clearing filters or search terms.'
                                        : 'No farm activities have been recorded yet for this cultivation cycle.',
                                    textAlign: TextAlign.center,
                                    style: const TextStyle(
                                      fontSize: 13,
                                      color: AppColors.inkSoft,
                                    ),
                                  ),
                                  const SizedBox(height: 20),
                                  ElevatedButton.icon(
                                    onPressed: () async {
                                      final targetCycle = _selectedCycleId ??
                                          (_cycles.isNotEmpty
                                              ? _cycles.first.id
                                              : null);
                                      final route = targetCycle != null
                                          ? '/activities/new?cycleId=$targetCycle'
                                          : '/activities/new';
                                      await context.push(route);
                                      _loadData();
                                    },
                                    icon: const Icon(Icons.add_task_rounded,
                                        size: 18),
                                    label: const Text('Record Activity'),
                                    style: ElevatedButton.styleFrom(
                                      backgroundColor: AppColors.forest,
                                      foregroundColor: AppColors.cream,
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 20, vertical: 12),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        )
                      else
                        SliverPadding(
                          padding: const EdgeInsets.fromLTRB(16, 0, 16, 80),
                          sliver: SliverList(
                            delegate: SliverChildBuilderDelegate(
                              (context, index) {
                                final activity = filtered[index];
                                return ActivityCard(
                                  activity: activity,
                                  onEdit: () => _handleEdit(activity),
                                  onDelete: () => _handleDelete(activity),
                                );
                              },
                              childCount: filtered.length,
                            ),
                          ),
                        ),
                    ],
                  ),
      ),
    );
  }
}
