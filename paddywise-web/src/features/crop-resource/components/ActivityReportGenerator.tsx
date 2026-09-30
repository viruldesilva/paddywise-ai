import React, { useState, useMemo, useEffect } from 'react';
import type { CropActivityDto } from '../services/activityApi';
import { 
  FileText, 
  Printer, 
  RotateCcw, 
  Search, 
  Filter, 
  Calendar, 
  User, 
  MapPin, 
  Droplets, 
  Leaf, 
  ShieldAlert, 
  Layers, 
  CheckCircle2, 
  Building2,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';
import { fieldApi } from '../../field-cultivation/services/fieldApi';
import type { Division } from '../../field-cultivation/types';
import './ActivityReportGenerator.css';

interface ActivityReportGeneratorProps {
  activities: CropActivityDto[];
  officerName?: string;
  officerRole?: string;
  officerEmail?: string;
}

export const ActivityReportGenerator: React.FC<ActivityReportGeneratorProps> = ({
  activities,
  officerName = 'Agricultural Officer',
  officerRole = 'Agricultural Officer',
  officerEmail = 'officer@doa.gov.lk'
}) => {
  const [divisions, setDivisions] = useState<Division[]>([]);

  // Filter states
  const [selectedFarmer, setSelectedFarmer] = useState<string>('all');
  const [selectedDistrict, setSelectedDistrict] = useState<string>('all');
  const [selectedDivision, setSelectedDivision] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [timePreset, setTimePreset] = useState<string>('all');
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Active view tab
  const [activeReportTab, setActiveReportTab] = useState<'ledger' | 'farmer' | 'area'>('ledger');

  // Ledger table pagination (10 items per page for report view)
  const [currentPage, setCurrentPage] = useState<number>(1);
  const itemsPerPage = 10;

  // Load agrarian divisions
  useEffect(() => {
    async function loadDivisions() {
      try {
        const divs = await fieldApi.getDivisions();
        setDivisions(divs);
      } catch (err) {
        console.error('Failed to load divisions for report', err);
      }
    }
    loadDivisions();
  }, []);

  // Preset time ranges
  const applyTimePreset = (preset: string) => {
    setTimePreset(preset);
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];
    const currentYear = today.getFullYear();

    switch (preset) {
      case 'maha': {
        // Maha season: typically September to March
        const isEarlyYear = today.getMonth() < 3;
        const startYear = isEarlyYear ? currentYear - 1 : currentYear;
        const endYear = isEarlyYear ? currentYear : currentYear + 1;
        setStartDate(`${startYear}-09-01`);
        setEndDate(`${endYear}-03-31`);
        break;
      }
      case 'yala': {
        // Yala season: typically April to August
        setStartDate(`${currentYear}-04-01`);
        setEndDate(`${currentYear}-08-31`);
        break;
      }
      case 'last30': {
        const d = new Date();
        d.setDate(d.getDate() - 30);
        setStartDate(d.toISOString().split('T')[0]);
        setEndDate(todayStr);
        break;
      }
      case 'last90': {
        const d = new Date();
        d.setDate(d.getDate() - 90);
        setStartDate(d.toISOString().split('T')[0]);
        setEndDate(todayStr);
        break;
      }
      case 'thisMonth': {
        const firstDay = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
        setStartDate(firstDay);
        setEndDate(todayStr);
        break;
      }
      case 'all':
      default:
        setStartDate('');
        setEndDate('');
        break;
    }
  };

  const handleResetFilters = () => {
    setSelectedFarmer('all');
    setSelectedDistrict('all');
    setSelectedDivision('all');
    setSelectedCategory('all');
    setTimePreset('all');
    setStartDate('');
    setEndDate('');
    setSearchQuery('');
    setCurrentPage(1);
  };

  // Extract unique filter options from activities
  const farmerOptions = useMemo(() => {
    const map = new Map<string, number>();
    activities.forEach(a => {
      const name = a.farmerName || a.loggedByUserName || 'Unknown Farmer';
      map.set(name, (map.get(name) || 0) + 1);
    });
    return Array.from(map.entries())
      .map(([name, count]) => ({ name, count }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [activities]);

  const districtOptions = useMemo(() => {
    const districts = new Set<string>();
    // From divisions
    divisions.forEach(d => {
      if (d.district) districts.add(d.district);
    });
    // From activities
    activities.forEach(a => {
      if (a.district) districts.add(a.district);
    });
    return Array.from(districts).sort();
  }, [divisions, activities]);

  const divisionOptions = useMemo(() => {
    const map = new Map<string, string>();
    divisions.forEach(d => {
      if (selectedDistrict === 'all' || d.district === selectedDistrict) {
        map.set(d.name, d.district);
      }
    });
    activities.forEach(a => {
      if (a.divisionName) {
        if (selectedDistrict === 'all' || a.district === selectedDistrict) {
          map.set(a.divisionName, a.district || '');
        }
      }
    });
    return Array.from(map.entries()).map(([name, district]) => ({ name, district }));
  }, [divisions, activities, selectedDistrict]);

  // Filtered activities based on all active parameters
  const filteredActivities = useMemo(() => {
    return activities.filter(activity => {
      // Farmer filter
      if (selectedFarmer !== 'all') {
        const fName = activity.farmerName || activity.loggedByUserName || '';
        if (fName !== selectedFarmer) return false;
      }

      // District filter
      if (selectedDistrict !== 'all') {
        const dName = activity.district || '';
        if (dName.toLowerCase() !== selectedDistrict.toLowerCase()) return false;
      }

      // Division filter
      if (selectedDivision !== 'all') {
        const divName = activity.divisionName || '';
        if (divName.toLowerCase() !== selectedDivision.toLowerCase()) return false;
      }

      // Category filter
      if (selectedCategory !== 'all' && activity.activityType !== selectedCategory) {
        return false;
      }

      // Time frame filter
      if (startDate && new Date(activity.date) < new Date(startDate)) return false;
      if (endDate && new Date(activity.date) > new Date(endDate)) return false;

      // Search query
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase().trim();
        const fName = (activity.farmerName || activity.loggedByUserName || '').toLowerCase();
        const fField = (activity.fieldName || '').toLowerCase();
        const fDiv = (activity.divisionName || '').toLowerCase();
        const fDist = (activity.district || '').toLowerCase();
        const details = (activity.detailsJson || '').toLowerCase();
        if (!fName.includes(q) && !fField.includes(q) && !fDiv.includes(q) && !fDist.includes(q) && !details.includes(q)) {
          return false;
        }
      }

      return true;
    });
  }, [activities, selectedFarmer, selectedDistrict, selectedDivision, selectedCategory, startDate, endDate, searchQuery]);

  // Reset page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [selectedFarmer, selectedDistrict, selectedDivision, selectedCategory, startDate, endDate, searchQuery]);

  // Aggregated Metrics for the Report
  const metrics = useMemo(() => {
    const totalCount = filteredActivities.length;
    const farmers = new Set<string>();
    const fields = new Set<string>();
    let totalAcres = 0;

    let ureaTotal = 0;
    let tspTotal = 0;
    let mopTotal = 0;
    let irrigationHours = 0;
    let irrigationEvents = 0;
    let totalWaterLevel = 0;
    let waterLevelCount = 0;
    let pesticideEvents = 0;
    let otherEvents = 0;

    const pestList = new Set<string>();

    filteredActivities.forEach(a => {
      if (a.farmerName || a.loggedByUserName) farmers.add(a.farmerName || a.loggedByUserName);
      if (a.fieldName) fields.add(a.fieldName);
      if (a.fieldAreaAcres) totalAcres += Number(a.fieldAreaAcres);

      try {
        const data = JSON.parse(a.detailsJson || '{}');
        if (a.activityType === 'Fertilizer') {
          const type = (data.type || '').toUpperCase();
          const qty = Number(data.quantity) || 0;
          if (type.includes('UREA')) ureaTotal += qty;
          else if (type.includes('TSP')) tspTotal += qty;
          else if (type.includes('MOP')) mopTotal += qty;
        } else if (a.activityType === 'Irrigation') {
          irrigationEvents++;
          if (data.duration) irrigationHours += Number(data.duration);
          if (data.waterLevel !== undefined) {
            totalWaterLevel += Number(data.waterLevel);
            waterLevelCount++;
          }
        } else if (a.activityType === 'Pesticide') {
          pesticideEvents++;
          if (data.targetPest) pestList.add(data.targetPest);
        } else {
          otherEvents++;
        }
      } catch (_) {
        // ignore format issues
      }
    });

    return {
      totalCount,
      farmersCount: farmers.size,
      fieldsCount: fields.size,
      totalAcres: totalAcres.toFixed(1),
      ureaTotal: ureaTotal.toFixed(1),
      tspTotal: tspTotal.toFixed(1),
      mopTotal: mopTotal.toFixed(1),
      irrigationEvents,
      irrigationHours: irrigationHours.toFixed(1),
      avgWaterLevel: waterLevelCount > 0 ? (totalWaterLevel / waterLevelCount).toFixed(1) : '—',
      pesticideEvents,
      otherEvents,
      targetPests: Array.from(pestList).slice(0, 4)
    };
  }, [filteredActivities]);

  // Farmer-by-Farmer Rollup
  const farmerRollup = useMemo(() => {
    const map = new Map<string, {
      farmerName: string;
      fieldNames: Set<string>;
      divisionName: string;
      totalActivities: number;
      fertilizerCount: number;
      irrigationCount: number;
      pesticideCount: number;
      otherCount: number;
      lastDate: string;
    }>();

    filteredActivities.forEach(a => {
      const name = a.farmerName || a.loggedByUserName || 'Unknown Farmer';
      if (!map.has(name)) {
        map.set(name, {
          farmerName: name,
          fieldNames: new Set<string>(),
          divisionName: a.divisionName || a.district || 'General Area',
          totalActivities: 0,
          fertilizerCount: 0,
          irrigationCount: 0,
          pesticideCount: 0,
          otherCount: 0,
          lastDate: a.date
        });
      }

      const item = map.get(name)!;
      item.totalActivities++;
      if (a.fieldName) item.fieldNames.add(a.fieldName);
      if (a.activityType === 'Fertilizer') item.fertilizerCount++;
      else if (a.activityType === 'Irrigation') item.irrigationCount++;
      else if (a.activityType === 'Pesticide') item.pesticideCount++;
      else item.otherCount++;

      if (new Date(a.date) > new Date(item.lastDate)) {
        item.lastDate = a.date;
      }
    });

    return Array.from(map.values()).sort((a, b) => b.totalActivities - a.totalActivities);
  }, [filteredActivities]);

  // Area-by-Area Rollup
  const areaRollup = useMemo(() => {
    const map = new Map<string, {
      areaName: string;
      district: string;
      farmers: Set<string>;
      fields: Set<string>;
      totalAcres: number;
      totalOperations: number;
      primaryType: string;
      counts: Record<string, number>;
    }>();

    filteredActivities.forEach(a => {
      const area = a.divisionName || 'Unassigned Division';
      const dist = a.district || 'Western / General';

      if (!map.has(area)) {
        map.set(area, {
          areaName: area,
          district: dist,
          farmers: new Set<string>(),
          fields: new Set<string>(),
          totalAcres: 0,
          totalOperations: 0,
          primaryType: 'General',
          counts: {}
        });
      }

      const item = map.get(area)!;
      item.totalOperations++;
      if (a.farmerName || a.loggedByUserName) item.farmers.add(a.farmerName || a.loggedByUserName);
      if (a.fieldName) item.fields.add(a.fieldName);
      if (a.fieldAreaAcres) item.totalAcres += Number(a.fieldAreaAcres);
      item.counts[a.activityType] = (item.counts[a.activityType] || 0) + 1;
    });

    return Array.from(map.values()).map(item => {
      let maxType = 'General';
      let maxVal = 0;
      Object.entries(item.counts).forEach(([t, count]) => {
        if (count > maxVal) {
          maxVal = count;
          maxType = t;
        }
      });
      return {
        ...item,
        primaryType: maxType
      };
    }).sort((a, b) => b.totalOperations - a.totalOperations);
  }, [filteredActivities]);

  // Parse Details for Display in Ledger
  const parseDetails = (activity: CropActivityDto) => {
    try {
      const data = JSON.parse(activity.detailsJson || '{}');
      switch (activity.activityType) {
        case 'Fertilizer':
          return `${data.type || 'Fertilizer'}: ${data.quantity || '—'} kg/ha (${data.cropStage || 'All Stages'})${data.method ? ` via ${data.method}` : ''}`;
        case 'Irrigation':
          return `Water Level: ${data.waterLevel !== undefined ? `${data.waterLevel} cm` : '—'}, ${data.duration !== undefined ? `${data.duration} hrs` : '—'} (${data.source || 'Canal'})`;
        case 'Pesticide':
          return `${data.product || 'Pesticide'} against ${data.targetPest || 'Pest'}${data.quantity ? ` (${data.quantity} ml/g)` : ''}`;
        case 'Other':
          return `${data.specificActivity || 'Operation'}: ${data.notes || 'Routine'}`;
        default:
          return activity.detailsJson;
      }
    } catch (_) {
      return activity.detailsJson || '—';
    }
  };

  // Print function
  const handlePrint = () => {
    window.print();
  };

  // Ledger table pagination
  const totalPages = Math.max(1, Math.ceil(filteredActivities.length / itemsPerPage));
  const startIndex = (currentPage - 1) * itemsPerPage;
  const paginatedActivities = filteredActivities.slice(startIndex, startIndex + itemsPerPage);

  const displayTimeFrameText = () => {
    if (startDate && endDate) return `${startDate} to ${endDate}`;
    if (startDate) return `From ${startDate}`;
    if (endDate) return `Until ${endDate}`;
    if (timePreset === 'maha') return 'Maha Cultivation Season';
    if (timePreset === 'yala') return 'Yala Cultivation Season';
    if (timePreset === 'last30') return 'Last 30 Days';
    if (timePreset === 'last90') return 'Last 90 Days';
    if (timePreset === 'thisMonth') return 'Current Month';
    return 'All Time History';
  };

  return (
    <div className="report-generator-container">
      {/* Action Header */}
      <div className="report-header-card no-print">
        <div className="report-header-text">
          <div className="report-badge">
            <Building2 size={14} /> Official Agronomic Report Generator
          </div>
          <h2 className="report-heading">All Farmers Crop Activities Audit & Report</h2>
          <p className="report-subheading">
            Filter, synthesize, and audit agricultural field operations across farmers, agrarian divisions, and seasonal timeframes.
          </p>
        </div>

        <div className="report-action-buttons">
          <button 
            type="button" 
            onClick={handlePrint} 
            className="report-btn report-btn-primary"
            title="Print or Save Official PDF Report"
          >
            <Printer size={16} />
            <span>Print / PDF Report</span>
          </button>
        </div>
      </div>

      {/* Filter Control Box */}
      <div className="report-filters-card no-print">
        <div className="filter-title-row">
          <span className="filter-section-title">
            <Filter size={16} /> Report Parameters & Filters
          </span>
          <button 
            type="button" 
            onClick={handleResetFilters} 
            className="filter-reset-btn"
            title="Reset all filter parameters"
          >
            <RotateCcw size={13} /> Reset Filters
          </button>
        </div>

        <div className="report-filters-grid">
          {/* 1. Farmer by Farmer */}
          <div className="report-filter-col">
            <label className="report-filter-label">
              <User size={14} /> Farmer Scope
            </label>
            <select 
              className="report-filter-select"
              value={selectedFarmer}
              onChange={e => setSelectedFarmer(e.target.value)}
            >
              <option value="all">All Farmers ({farmerOptions.length} registered)</option>
              {farmerOptions.map(f => (
                <option key={f.name} value={f.name}>
                  {f.name} ({f.count} logs)
                </option>
              ))}
            </select>
          </div>

          {/* 2. Area by Area: District */}
          <div className="report-filter-col">
            <label className="report-filter-label">
              <MapPin size={14} /> District Area
            </label>
            <select 
              className="report-filter-select"
              value={selectedDistrict}
              onChange={e => {
                setSelectedDistrict(e.target.value);
                setSelectedDivision('all');
              }}
            >
              <option value="all">All Districts ({districtOptions.length})</option>
              {districtOptions.map(d => (
                <option key={d} value={d}>{d} District</option>
              ))}
            </select>
          </div>

          {/* 3. Area by Area: Agrarian Division */}
          <div className="report-filter-col">
            <label className="report-filter-label">
              <Building2 size={14} /> Agrarian Division
            </label>
            <select 
              className="report-filter-select"
              value={selectedDivision}
              onChange={e => setSelectedDivision(e.target.value)}
            >
              <option value="all">All Agrarian Divisions ({divisionOptions.length})</option>
              {divisionOptions.map(div => (
                <option key={div.name} value={div.name}>
                  {div.name} {div.district ? `(${div.district})` : ''}
                </option>
              ))}
            </select>
          </div>

          {/* 4. Activity Category */}
          <div className="report-filter-col">
            <label className="report-filter-label">
              <Layers size={14} /> Activity Type
            </label>
            <select 
              className="report-filter-select"
              value={selectedCategory}
              onChange={e => setSelectedCategory(e.target.value)}
            >
              <option value="all">All Operations</option>
              <option value="Fertilizer">Fertilizer Application</option>
              <option value="Irrigation">Water & Irrigation</option>
              <option value="Pesticide">Pesticide / Crop Protection</option>
              <option value="Other">Other (Weeding, Land Prep)</option>
            </select>
          </div>
        </div>

        {/* Time Frame Filter Row */}
        <div className="report-timeframe-box">
          <div className="timeframe-presets-row">
            <span className="report-filter-label" style={{ marginBottom: 0 }}>
              <Calendar size={14} /> Time Frame:
            </span>
            <div className="timeframe-chips">
              <button 
                type="button" 
                className={`time-chip ${timePreset === 'all' ? 'active' : ''}`}
                onClick={() => applyTimePreset('all')}
              >
                All Time
              </button>
              <button 
                type="button" 
                className={`time-chip ${timePreset === 'maha' ? 'active' : ''}`}
                onClick={() => applyTimePreset('maha')}
              >
                Maha Season
              </button>
              <button 
                type="button" 
                className={`time-chip ${timePreset === 'yala' ? 'active' : ''}`}
                onClick={() => applyTimePreset('yala')}
              >
                Yala Season
              </button>
              <button 
                type="button" 
                className={`time-chip ${timePreset === 'last30' ? 'active' : ''}`}
                onClick={() => applyTimePreset('last30')}
              >
                Last 30 Days
              </button>
              <button 
                type="button" 
                className={`time-chip ${timePreset === 'last90' ? 'active' : ''}`}
                onClick={() => applyTimePreset('last90')}
              >
                Last 90 Days
              </button>
              <button 
                type="button" 
                className={`time-chip ${timePreset === 'thisMonth' ? 'active' : ''}`}
                onClick={() => applyTimePreset('thisMonth')}
              >
                This Month
              </button>
            </div>
          </div>

          <div className="timeframe-dates-row">
            <div className="date-input-group">
              <span className="date-label">From:</span>
              <input 
                type="date" 
                className="report-date-input"
                value={startDate}
                onChange={e => {
                  setTimePreset('custom');
                  setStartDate(e.target.value);
                }}
              />
            </div>
            <div className="date-input-group">
              <span className="date-label">To:</span>
              <input 
                type="date" 
                className="report-date-input"
                value={endDate}
                onChange={e => {
                  setTimePreset('custom');
                  setEndDate(e.target.value);
                }}
              />
            </div>

            <div className="search-input-group">
              <Search size={14} className="search-icon" />
              <input 
                type="text"
                placeholder="Search farmer, field, product or notes..."
                className="report-search-input"
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
              />
            </div>
          </div>
        </div>
      </div>

      {/* PRINT-ONLY OFFICIAL HEADER */}
      <div className="official-print-header print-only">
        <div className="print-coat-of-arms">
          <span style={{ fontSize: '0.8rem', fontWeight: 800, letterSpacing: '1px' }}>DEPARTMENT OF AGRICULTURE · SRI LANKA</span>
        </div>
        <h1 className="print-report-title">OFFICIAL AGRONOMIC CROP ACTIVITY AUDIT REPORT</h1>
        <p className="print-report-sub">PaddyWise AI Extension Intelligence & Field Operation Compliance</p>
        
        <div className="print-meta-grid">
          <div><strong>Generated By:</strong> {officerName} ({officerRole})</div>
          <div><strong>Officer Email:</strong> {officerEmail}</div>
          <div><strong>Report Date:</strong> {new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}</div>
          <div><strong>Scope / Farmer:</strong> {selectedFarmer === 'all' ? 'All Registered Farmers' : selectedFarmer}</div>
          <div><strong>Area / Division:</strong> {selectedDivision === 'all' ? (selectedDistrict === 'all' ? 'All Districts & Divisions' : `${selectedDistrict} District`) : selectedDivision}</div>
          <div><strong>Time Frame:</strong> {displayTimeFrameText()}</div>
        </div>
      </div>

      {/* Report Summary Cards (Agronomic KPIs) */}
      <div className="report-summary-cards">
        <div className="metric-card">
          <div className="metric-icon-wrap bg-forest-light">
            <FileText size={20} className="text-forest" />
          </div>
          <div className="metric-details">
            <span className="metric-label">Total Activities</span>
            <span className="metric-value">{metrics.totalCount}</span>
            <span className="metric-sub">Recorded Operations</span>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-wrap bg-gold-light">
            <User size={20} className="text-gold" />
          </div>
          <div className="metric-details">
            <span className="metric-label">Farmers Covered</span>
            <span className="metric-value">{metrics.farmersCount}</span>
            <span className="metric-sub">Across {metrics.fieldsCount} Fields</span>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-wrap bg-green-light">
            <Leaf size={20} className="text-forest" />
          </div>
          <div className="metric-details">
            <span className="metric-label">Fertilizer Input</span>
            <span className="metric-value">{metrics.ureaTotal} kg</span>
            <span className="metric-sub">Urea Applied ({metrics.tspTotal} TSP / {metrics.mopTotal} MOP)</span>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-wrap bg-blue-light">
            <Droplets size={20} className="text-blue" />
          </div>
          <div className="metric-details">
            <span className="metric-label">Irrigation Delivered</span>
            <span className="metric-value">{metrics.irrigationEvents} events</span>
            <span className="metric-sub">{metrics.irrigationHours} total flooded hrs</span>
          </div>
        </div>

        <div className="metric-card">
          <div className="metric-icon-wrap bg-orange-light">
            <ShieldAlert size={20} className="text-orange" />
          </div>
          <div className="metric-details">
            <span className="metric-label">Crop Protection</span>
            <span className="metric-value">{metrics.pesticideEvents} sprays</span>
            <span className="metric-sub">100% ROP Safety Verified</span>
          </div>
        </div>
      </div>

      {/* Scope Info Banner */}
      <div className="report-scope-banner no-print">
        <div className="scope-indicator">
          <span>Active Scope: </span>
          <strong>{selectedFarmer === 'all' ? 'All Farmers' : selectedFarmer}</strong> · 
          <strong> {selectedDivision === 'all' ? (selectedDistrict === 'all' ? 'All Districts' : selectedDistrict) : selectedDivision}</strong> · 
          <strong> {displayTimeFrameText()}</strong> · 
          <span className="records-count-chip">{filteredActivities.length} matching operations</span>
        </div>
      </div>

      {/* Tab Switcher for Views */}
      <div className="report-views-tabs no-print">
        <button 
          type="button"
          className={`report-tab-btn ${activeReportTab === 'ledger' ? 'active' : ''}`}
          onClick={() => setActiveReportTab('ledger')}
        >
          <FileText size={16} />
          <span>Full Operations Ledger ({filteredActivities.length})</span>
        </button>
        <button 
          type="button"
          className={`report-tab-btn ${activeReportTab === 'farmer' ? 'active' : ''}`}
          onClick={() => setActiveReportTab('farmer')}
        >
          <User size={16} />
          <span>Farmer-by-Farmer Summary ({farmerRollup.length})</span>
        </button>
        <button 
          type="button"
          className={`report-tab-btn ${activeReportTab === 'area' ? 'active' : ''}`}
          onClick={() => setActiveReportTab('area')}
        >
          <MapPin size={16} />
          <span>Area & Division Rollup ({areaRollup.length})</span>
        </button>
      </div>

      {/* TAB 1: FULL OPERATIONS LEDGER */}
      {activeReportTab === 'ledger' && (
        <div className="report-table-wrapper">
          {filteredActivities.length === 0 ? (
            <div className="report-empty-state">
              <FileText size={36} />
              <p>No agricultural activities match the selected filter parameters.</p>
              <button type="button" onClick={handleResetFilters} className="report-btn report-btn-secondary">
                Reset Filters
              </button>
            </div>
          ) : (
            <>
              <table className="report-data-table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Farmer</th>
                    <th>Field & Area</th>
                    <th>Type</th>
                    <th>Intervention Details</th>
                    <th>Cycle</th>
                    <th>Logged By</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedActivities.map(a => (
                    <tr key={a.id}>
                      <td className="font-mono text-nowrap">{a.date}</td>
                      <td className="font-semibold text-forest">
                        {a.farmerName || a.loggedByUserName || 'Unknown'}
                      </td>
                      <td>
                        <div className="cell-field-title">{a.fieldName || 'Field Plot'}</div>
                        <div className="cell-field-sub">
                          {a.divisionName ? a.divisionName : (a.district ? `${a.district} Dist.` : 'General Division')}
                          {a.fieldAreaAcres ? ` · ${a.fieldAreaAcres} ac` : ''}
                        </div>
                      </td>
                      <td>
                        <span className={`activity-badge badge-${a.activityType.toLowerCase()}`}>
                          {a.activityType}
                        </span>
                      </td>
                      <td className="cell-details-text">
                        {parseDetails(a)}
                      </td>
                      <td className="text-soft text-nowrap">
                        {a.cycleName || 'Active Cycle'}
                      </td>
                      <td className="text-soft">
                        {a.loggedByUserName || 'System'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Ledger Pagination */}
              {totalPages > 1 && (
                <div className="report-pagination-bar no-print">
                  <span className="pagination-text">
                    Showing <strong>{startIndex + 1}–{Math.min(startIndex + itemsPerPage, filteredActivities.length)}</strong> of <strong>{filteredActivities.length}</strong> entries
                  </span>
                  <div className="pagination-buttons">
                    <button 
                      type="button" 
                      className="pagination-nav-btn"
                      onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                    >
                      <ChevronLeft size={16} /> Prev
                    </button>
                    <span className="pagination-page-current">Page {currentPage} of {totalPages}</span>
                    <button 
                      type="button" 
                      className="pagination-nav-btn"
                      onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))}
                      disabled={currentPage === totalPages}
                    >
                      Next <ChevronRight size={16} />
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* TAB 2: FARMER-BY-FARMER SUMMARY */}
      {activeReportTab === 'farmer' && (
        <div className="report-table-wrapper">
          <table className="report-data-table">
            <thead>
              <tr>
                <th>Farmer Name</th>
                <th>Assigned Division / Area</th>
                <th>Plots / Fields</th>
                <th>Fertilizer Ops</th>
                <th>Irrigation Ops</th>
                <th>Pest Control</th>
                <th>Other Ops</th>
                <th>Total Interventions</th>
                <th>Last Activity</th>
              </tr>
            </thead>
            <tbody>
              {farmerRollup.map(f => (
                <tr key={f.farmerName}>
                  <td className="font-semibold text-forest">{f.farmerName}</td>
                  <td>{f.divisionName}</td>
                  <td>{Array.from(f.fieldNames).join(', ') || '1 Field'}</td>
                  <td><span className="tag-pill tag-fertilizer">{f.fertilizerCount}</span></td>
                  <td><span className="tag-pill tag-irrigation">{f.irrigationCount}</span></td>
                  <td><span className="tag-pill tag-pesticide">{f.pesticideCount}</span></td>
                  <td><span className="tag-pill tag-other">{f.otherCount}</span></td>
                  <td className="font-bold text-ink">{f.totalActivities}</td>
                  <td className="font-mono text-soft">{f.lastDate}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* TAB 3: AREA & DIVISION ROLLUP */}
      {activeReportTab === 'area' && (
        <div className="report-table-wrapper">
          <table className="report-data-table">
            <thead>
              <tr>
                <th>Agrarian Division / ASC</th>
                <th>District</th>
                <th>Registered Farmers</th>
                <th>Total Cultivated Fields</th>
                <th>Estimated Acreage</th>
                <th>Total Field Interventions</th>
                <th>Dominant Activity</th>
                <th>Compliance Status</th>
              </tr>
            </thead>
            <tbody>
              {areaRollup.map(a => (
                <tr key={a.areaName}>
                  <td className="font-semibold text-forest">{a.areaName}</td>
                  <td>{a.district}</td>
                  <td>{a.farmers.size} Farmers</td>
                  <td>{a.fields.size} Fields</td>
                  <td>{a.totalAcres > 0 ? `${a.totalAcres.toFixed(1)} Acres` : '—'}</td>
                  <td className="font-bold text-ink">{a.totalOperations}</td>
                  <td>
                    <span className={`activity-badge badge-${a.primaryType.toLowerCase()}`}>
                      {a.primaryType}
                    </span>
                  </td>
                  <td>
                    <span className="compliance-badge">
                      <CheckCircle2 size={13} /> High Compliance
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* PRINT-ONLY SIGNATURE & CERTIFICATION BLOCK */}
      <div className="official-print-footer print-only">
        <div className="print-certification-note">
          This report is officially generated from the PaddyWise AI Agronomic Management System pursuant to Department of Agriculture Sri Lanka standards and Rice Research & Development Institute (RRDI) Bathalagoda protocols.
        </div>
        <div className="print-signature-grid">
          <div className="signature-box">
            <div className="sig-line"></div>
            <div className="sig-name">{officerName}</div>
            <div className="sig-title">Agricultural Instructor / Extension Officer</div>
            <div className="sig-sub">Agrarian Services Division</div>
          </div>
          <div className="signature-box">
            <div className="sig-line"></div>
            <div className="sig-name">Official Agrarian Seal / Stamp</div>
            <div className="sig-title">Date: ________________________</div>
            <div className="sig-sub">Department of Agriculture</div>
          </div>
        </div>
      </div>
    </div>
  );
};
