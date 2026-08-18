import { ReactElement, useState, useEffect, useMemo, useRef } from 'react';
// @mui
import { Container, Grid, Typography, Stack, Button, TextField, MenuItem, Box, Card, CardHeader } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import dynamic from 'next/dynamic';
import merge from 'lodash/merge';
import { getISOWeek, getISOWeekYear, subWeeks, parseISO, format } from 'date-fns';
import { DatePicker } from '@mui/x-date-pickers';
// layouts
import Layout from '@/layouts';
// components
import Page from '@/components/Page';
import Iconify from '@/components/Iconify';
// redux
import { useDispatch, useSelector } from '@/redux/store';
import { fetchAdminSummaryStats } from '@/redux/slices/reportDashboard';
// services
import analyticsService, { TopSchoolAnalytics, TopTenantAnalytics, Province, TrendAnalytics } from '@/services/analyticsService';

const Chart = dynamic(() => import('react-apexcharts'), { ssr: false });
// sections
import AdminDashboardKpiCard from '@/sections/admin/dashboard/AdminDashboardKpiCard';
import AdminDashboardRegionalChart from '@/sections/admin/dashboard/AdminDashboardRegionalChart';
import AdminDashboardTopTenantsChart from '@/sections/admin/dashboard/AdminDashboardTopTenantsChart';

// ----------------------------------------------------------------------

AdminDashboardPage.getLayout = function getLayout(page: ReactElement) {
  return <Layout roles={['SUPER_ADMIN', 'LMS_ADMIN']}>{page}</Layout>;
};

// ----------------------------------------------------------------------

export default function AdminDashboardPage() {
  const dispatch = useDispatch();
  const { summaryStats: stats, isSummaryLoading: isRefreshing } = useSelector((state) => state.reportDashboard);
  const [topSchools, setTopSchools] = useState<TopSchoolAnalytics[]>([]);
  const [topTenants, setTopTenants] = useState<TopTenantAnalytics[]>([]);
  const [provinces, setProvinces] = useState<Province[]>([]);
  const [selectedProvince, setSelectedProvince] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);
  
  const [groupBy, setGroupBy] = useState<string>('day');

  const [dayRange, setDayRange] = useState({
    from: new Date(new Date().setMonth(new Date().getMonth() - 1)).toISOString().split('T')[0],
    to: new Date().toISOString().split('T')[0]
  });

  const [weekRange, setWeekRange] = useState({
    from: `${getISOWeekYear(subWeeks(new Date(), 4))}-W${getISOWeek(subWeeks(new Date(), 4)).toString().padStart(2, '0')}`,
    to: `${getISOWeekYear(new Date())}-W${getISOWeek(new Date()).toString().padStart(2, '0')}`
  });

  const [monthRange, setMonthRange] = useState({
    from: new Date(new Date().setMonth(new Date().getMonth() - 6)).toISOString().slice(0, 7),
    to: new Date().toISOString().slice(0, 7)
  });

  const getWeekDate = (weekStr: string, isStart: boolean) => {
    if (!weekStr) return '';
    const [year, week] = weekStr.split('-W').map(Number);
    const simple = new Date(year, 0, 1 + (week - 1) * 7);
    const dow = simple.getDay();
    const isoStart = new Date(simple.valueOf());
    if (dow <= 4) {
        isoStart.setDate(simple.getDate() - simple.getDay() + 1);
    } else {
        isoStart.setDate(simple.getDate() + 8 - simple.getDay());
    }
    if (isStart) return isoStart.toISOString().split('T')[0];
    const isoEnd = new Date(isoStart.valueOf());
    isoEnd.setDate(isoStart.getDate() + 6);
    return isoEnd.toISOString().split('T')[0];
  };

  const apiDateRange = useMemo(() => {
    if (groupBy === 'week') {
      return {
        fromDate: getWeekDate(weekRange.from, true),
        toDate: getWeekDate(weekRange.to, false)
      };
    }
    
    if (groupBy === 'month') {
      if (!monthRange.from || !monthRange.to) return { fromDate: '', toDate: '' };
      const fromDate = `${monthRange.from}-01`;
      const [tYear, tMonth] = monthRange.to.split('-');
      const end = new Date(Number(tYear), Number(tMonth), 0);
      const toDate = `${monthRange.to}-${end.getDate().toString().padStart(2, '0')}`;
      return { fromDate, toDate };
    }
    
    return {
      fromDate: dayRange.from,
      toDate: dayRange.to
    };
  }, [groupBy, dayRange, weekRange, monthRange]);
  const [trendData, setTrendData] = useState<TrendAnalytics[]>([]);

  useEffect(() => {
    dispatch(fetchAdminSummaryStats());
  }, [dispatch]);

  const fetchChartData = async () => {
    setIsLoading(true);
    try {
      const [topSchoolsRes, topTenantsRes, provincesRes, trendRes] = await Promise.all([
        analyticsService.getTopSchools(apiDateRange.fromDate, apiDateRange.toDate, selectedProvince),
        analyticsService.getTopTenants(apiDateRange.fromDate, apiDateRange.toDate, selectedProvince),
        analyticsService.getProvinces(),
        analyticsService.getTrend(apiDateRange.fromDate, apiDateRange.toDate, groupBy, selectedProvince)
      ]);
      setTopSchools(topSchoolsRes.data);
      setTopTenants(topTenantsRes.data);
      setProvinces(provincesRes.data);
      setTrendData(trendRes.data);
    } catch (err) {
      console.error('Failed to fetch chart data:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchChartData();
  }, [apiDateRange, selectedProvince, groupBy]);

  const theme = useTheme();

  const trendChartOptions = useMemo(() => {
    const labels = trendData.map(d => {
      if (groupBy === 'day') return new Date(d.timeSlot).toLocaleDateString('vi-VN');
      if (groupBy === 'week') return `Tuần ${new Date(d.timeSlot).toLocaleDateString('vi-VN')}`;
      return new Date(d.timeSlot).toLocaleDateString('vi-VN', { month: 'long', year: 'numeric' });
    });
    return {
      chartOptions: merge({
        chart: { toolbar: { show: false } },
        colors: [theme.palette.primary.main],
        stroke: { show: true, curve: 'smooth', width: 2 },
        fill: { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.7, opacityTo: 0.3 } },
        labels,
        xaxis: { type: 'category' },
        tooltip: {
          y: { formatter: (val: number) => `${val.toFixed(2)}` },
        },
      }),
      learnerSeries: [{ name: 'Người học', data: trendData.map(d => d.learnerCount) }],
      hoursSeries: [{ name: 'Giờ học', data: trendData.map(d => Number(d.totalStudyHours.toFixed(2))) }],
    };
  }, [trendData, groupBy, theme]);

  const handleDayFromChange = (val: Date | null) => {
    if (!val || isNaN(val.getTime())) return;
    const fromStr = format(val, 'yyyy-MM-dd');
    setDayRange((prev) => ({ from: fromStr, to: prev.to && fromStr > prev.to ? fromStr : prev.to }));
  };

  const handleDayToChange = (val: Date | null) => {
    if (!val || isNaN(val.getTime())) return;
    const toStr = format(val, 'yyyy-MM-dd');
    setDayRange((prev) => ({ from: prev.from && toStr < prev.from ? toStr : prev.from, to: toStr }));
  };

  const handleWeekFromChange = (val: Date | null) => {
    if (!val || isNaN(val.getTime())) return;
    const fromStr = `${getISOWeekYear(val)}-W${getISOWeek(val).toString().padStart(2, '0')}`;
    setWeekRange((prev) => ({ from: fromStr, to: prev.to && fromStr > prev.to ? fromStr : prev.to }));
  };

  const handleWeekToChange = (val: Date | null) => {
    if (!val || isNaN(val.getTime())) return;
    const toStr = `${getISOWeekYear(val)}-W${getISOWeek(val).toString().padStart(2, '0')}`;
    setWeekRange((prev) => ({ from: prev.from && toStr < prev.from ? toStr : prev.from, to: toStr }));
  };

  const handleMonthFromChange = (val: Date | null) => {
    if (!val || isNaN(val.getTime())) return;
    const fromStr = format(val, 'yyyy-MM');
    setMonthRange((prev) => ({ from: fromStr, to: prev.to && fromStr > prev.to ? fromStr : prev.to }));
  };

  const handleMonthToChange = (val: Date | null) => {
    if (!val || isNaN(val.getTime())) return;
    const toStr = format(val, 'yyyy-MM');
    setMonthRange((prev) => ({ from: prev.from && toStr < prev.from ? toStr : prev.from, to: toStr }));
  };

  return (
    <Page title="Tổng quan Super Admin">
      <Container maxWidth="xl">
        <Typography variant="h4" sx={{ mb: 5 }}>Dashboard Tổng quan Hệ thống</Typography>

        <Grid container spacing={3} sx={{ mb: 5 }}>
          <Grid item xs={12} sm={6} md={3}>
            <AdminDashboardKpiCard 
              title="Tổng số chương trình" 
              value={stats?.activeTenants || 0}
              trend={stats?.trends?.tenants || 'Đang cập nhật'}
              trendDirection={(stats?.trends?.tenantsDirection as 'up' | 'down') || 'up'}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <AdminDashboardKpiCard 
              title="Trường học đang sử dụng" 
              value={stats?.activeSchools || 0}
              trend={stats?.trends?.schools || 'Đang cập nhật'}
              trendDirection={(stats?.trends?.schoolsDirection as 'up' | 'down') || 'up'}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <AdminDashboardKpiCard 
              title="Số phiên đang hoạt động" 
              value={(stats?.activeSessions || 0).toLocaleString()}
              trend={stats?.trends?.sessions || 'Mức tải: Theo dõi realtime'}
              trendDirection={(stats?.trends?.sessionsDirection as 'up' | 'down') || 'up'}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <AdminDashboardKpiCard 
              title="Sức khỏe hệ thống" 
              value={stats?.systemHealth === 'Stable' ? 'Ổn định' : stats?.systemHealth || 'Ổn định'}
              trend="Trạng thái dịch vụ"
              trendDirection={stats?.systemHealth === 'Stable' ? 'up' : 'down'}
              color={stats?.systemHealth === 'Stable' ? 'success.main' : 'error.main'}
            />
          </Grid>
        </Grid>

        <Stack direction="row" spacing={2} justifyContent="flex-end" sx={{ mb: 3 }}>
          <TextField
            select
            size="small"
            label="Tỉnh/Thành"
            value={selectedProvince}
            onChange={(e) => setSelectedProvince(e.target.value)}
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="">Tất cả</MenuItem>
            {provinces.map((p) => (
              <MenuItem key={p.id} value={p.name}>{p.name}</MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label="Nhóm theo"
            value={groupBy}
            onChange={(e) => setGroupBy(e.target.value)}
            sx={{ minWidth: 120 }}
          >
            <MenuItem value="day">Theo ngày</MenuItem>
            <MenuItem value="week">Theo tuần</MenuItem>
            <MenuItem value="month">Theo tháng</MenuItem>
          </TextField>

          {groupBy === 'day' && (
            <>
              <DatePicker
                label="Từ ngày"
                value={dayRange.from ? parseISO(dayRange.from) : null}
                onChange={handleDayFromChange}
                inputFormat="dd/MM/yyyy"
                renderInput={(params) => <TextField {...params} size="small" sx={{ maxWidth: 160 }} />}
              />
              <DatePicker
                label="Đến ngày"
                value={dayRange.to ? parseISO(dayRange.to) : null}
                onChange={handleDayToChange}
                inputFormat="dd/MM/yyyy"
                renderInput={(params) => <TextField {...params} size="small" sx={{ maxWidth: 160 }} />}
              />
            </>
          )}

          {groupBy === 'week' && (
            <>
              <DatePicker
                label="Từ tuần"
                value={weekRange.from ? parseISO(getWeekDate(weekRange.from, true)) : null}
                onChange={handleWeekFromChange}
                inputFormat="'Tuần' I, RRRR"
                renderInput={(params) => <TextField {...params} size="small" sx={{ maxWidth: 160 }} />}
              />
              <DatePicker
                label="Đến tuần"
                value={weekRange.to ? parseISO(getWeekDate(weekRange.to, true)) : null}
                onChange={handleWeekToChange}
                inputFormat="'Tuần' I, RRRR"
                renderInput={(params) => <TextField {...params} size="small" sx={{ maxWidth: 160 }} />}
              />
            </>
          )}

          {groupBy === 'month' && (
            <>
              <DatePicker
                views={['year', 'month']}
                label="Từ tháng"
                value={monthRange.from ? parseISO(`${monthRange.from}-01`) : null}
                onChange={handleMonthFromChange}
                inputFormat="MM/yyyy"
                renderInput={(params) => <TextField {...params} size="small" sx={{ maxWidth: 160 }} />}
              />
              <DatePicker
                views={['year', 'month']}
                label="Đến tháng"
                value={monthRange.to ? parseISO(`${monthRange.to}-01`) : null}
                onChange={handleMonthToChange}
                inputFormat="MM/yyyy"
                renderInput={(params) => <TextField {...params} size="small" sx={{ maxWidth: 160 }} />}
              />
            </>
          )}
          <Button 
            variant="contained" 
            startIcon={<Iconify icon={isLoading || isRefreshing ? 'eva:loader-outline' : 'eva:refresh-fill'} />}
            onClick={() => {
              dispatch(fetchAdminSummaryStats());
              fetchChartData();
            }}
            disabled={isLoading || isRefreshing}
          >
            {isLoading || isRefreshing ? 'Đang tải...' : 'Làm mới'}
          </Button>
        </Stack>

        <Grid container spacing={3}>
          {trendChartOptions.chartOptions.labels && trendChartOptions.chartOptions.labels.length > 0 && (
            <Grid item xs={12}>
              <AdminDashboardRegionalChart 
                title="Thời gian học theo vùng"
                learnerData={trendChartOptions.learnerSeries[0].data}
                hoursData={trendChartOptions.hoursSeries[0].data}
                chartLabels={trendChartOptions.chartOptions.labels}
              />
            </Grid>
          )}

          {topTenants.length > 0 && (
            <Grid item xs={12} md={6}>
              <AdminDashboardTopTenantsChart 
                title="Top Tenant (TB giờ học/ngày)"
                chartLabels={topTenants.map(d => d.tenantName)}
                chartData={topTenants.map(d => d.avgStudyHoursPerDay)}
              />
            </Grid>
          )}

          {topSchools.length > 0 && (
            <Grid item xs={12} md={6}>
              <AdminDashboardTopTenantsChart 
                title="Top trường học (TB giờ học/ngày)"
                chartLabels={topSchools.map(d => [
                  d.schoolName.length > 22 ? d.schoolName.substring(0, 22) + '...' : d.schoolName,
                  `(${d.region || 'Chưa xác định'})`
                ])}
                chartData={topSchools.map(d => d.avgStudyHoursPerDay)}
              />
            </Grid>
          )}

          {trendChartOptions.chartOptions.labels?.length === 0 && topTenants.length === 0 && topSchools.length === 0 && (
            <Grid item xs={12}>
              <Card sx={{ p: 5, textAlign: 'center' }}>
                <Typography variant="h6" sx={{ color: 'text.secondary' }}>
                  Không có trường nào hoạt động ở vùng này
                </Typography>
              </Card>
            </Grid>
          )}
        </Grid>
      </Container>
    </Page>
  );
}
