import dynamic from 'next/dynamic';
// @mui
import { Card, CardHeader, Box } from '@mui/material';
import merge from 'lodash/merge';
// components
import BaseOptionChart from '../../../components/chart/BaseOptionChart';

const Chart = dynamic(() => import('react-apexcharts'), { ssr: false });

// ----------------------------------------------------------------------

type Props = {
  title?: string;
  subheader?: string;
  chartData: number[];
  chartLabels: (string | string[])[];
};

export default function AdminDashboardTopTenantsChart({ title, subheader, chartData, chartLabels }: Props) {
  if (chartLabels.length === 0) return null;

  const chartOptions = merge(BaseOptionChart(), {
    plotOptions: { bar: { columnWidth: '40%', borderRadius: 4 } },
    labels: chartLabels,
    xaxis: { type: 'category' },
    yaxis: { min: 0 },
    tooltip: {
      y: {
        formatter: (val: number) => `${Math.max(0, val).toFixed(2)} giờ/ngày`,
      },
    },
    colors: ['#1890ff'],
  });

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardHeader title={title} subheader={subheader} />
      <Box sx={{ p: 3, pb: 1, flexGrow: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }} dir="ltr">
        <Chart
          type="bar"
          series={[{ name: 'Thời gian học trung bình', data: chartData }]}
          options={chartOptions}
          height={320}
        />
      </Box>
    </Card>
  );
}
