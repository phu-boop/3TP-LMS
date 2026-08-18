import axios from '@/utils/axios';
import { API_ENDPOINTS } from '@/constants/apiEndpoints';

export interface AnalyticsSummary {
  totalLearners: number;
  oneYearSummary: { monthStart: string; totalActiveSeconds: number }[];
  avgStudyHoursDay: number;
  avgStudyHoursWeek: number;
  avgStudyHoursMonth: number;
}

export interface RegionalAnalytics {
  regionName: string;
  learnerCount: number;
  totalStudyHours: number;
}

export interface TopSchoolAnalytics {
  schoolId: string;
  schoolName: string;
  region: string;
  avgStudyHoursPerDay: number;
}

export interface TopTenantAnalytics {
  tenantId: string;
  tenantName: string;
  avgStudyHoursPerDay: number;
}

export interface Province {
  id: number;
  name: string;
}

export interface TrendAnalytics {
  timeSlot: string;
  learnerCount: number;
  totalStudyHours: number;
}

const analyticsService = {
  getSummary: (fromDate: string, toDate: string, provinceName?: string, tenantId?: string) => 
    axios.get<AnalyticsSummary>(API_ENDPOINTS.analyticsSummary, {
      params: { fromDate, toDate, provinceName, tenantId }
    }),

  getRegional: (fromDate: string, toDate: string, provinceName?: string, tenantId?: string) => 
    axios.get<RegionalAnalytics[]>(API_ENDPOINTS.analyticsRegional, {
      params: { fromDate, toDate, provinceName, tenantId }
    }),

  getTopSchools: (fromDate: string, toDate: string, provinceName?: string, tenantId?: string) => 
    axios.get<TopSchoolAnalytics[]>(API_ENDPOINTS.analyticsTopSchools, {
      params: { fromDate, toDate, provinceName, tenantId }
    }),

  getTopTenants: (fromDate: string, toDate: string, provinceName?: string) => 
    axios.get<TopTenantAnalytics[]>(API_ENDPOINTS.analyticsTopTenants, {
      params: { fromDate, toDate, provinceName }
    }),

  getProvinces: () => 
    axios.get<Province[]>(API_ENDPOINTS.provincesList),

  getTrend: (fromDate: string, toDate: string, groupBy: string, provinceName?: string) => 
    axios.get<TrendAnalytics[]>(API_ENDPOINTS.analyticsTrend, {
      params: { fromDate, toDate, groupBy, provinceName }
    }),
};

export default analyticsService;
