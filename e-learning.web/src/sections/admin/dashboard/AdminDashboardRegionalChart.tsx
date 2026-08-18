import dynamic from 'next/dynamic';
// @mui
import { Card, CardHeader, Box, TextField, MenuItem } from '@mui/material';
import merge from 'lodash/merge';
// components
import BaseOptionChart from '../../../components/chart/BaseOptionChart';

const Chart = dynamic(() => import('react-apexcharts'), { ssr: false });

// ----------------------------------------------------------------------

type Props = {
  title?: string;
  subheader?: string;
  learnerData: number[];
  hoursData: number[];
  chartLabels: string[];
};

export default function AdminDashboardRegionalChart({ 
  title, 
  subheader, 
  learnerData, 
  hoursData, 
  chartLabels
}: Props) {
  if (chartLabels.length === 0) return null;

  const chartOptions = merge(BaseOptionChart(), {
    stroke: { show: true, curve: 'smooth', width: 2 },
    labels: chartLabels,
    xaxis: { type: 'category' },
    yaxis: [
      {
        min: 0,
        title: { text: 'Người học (người)' },
        labels: {
          formatter: (val: number) => `${val.toFixed(0)}`,
        },
      },
      {
        opposite: true,
        min: 0,
        title: { text: 'Giờ học trung bình (h)' },
        labels: {
          formatter: (val: number) => `${Math.max(0, val).toFixed(1)}`,
        },
      },
    ],
    tooltip: {
      shared: true,
      intersect: false,
      y: {
        formatter: (val: number) => `${Math.max(0, val).toFixed(2)}`,
      },
    },
    colors: ['#1890ff', '#00ab55'],
  });

  const chartSeries = [
    {
      name: 'Người học',
      type: 'line',
      data: learnerData,
    },
    {
      name: 'Giờ học trung bình',
      type: 'line',
      data: hoursData,
    },
  ];

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardHeader 
        title={title} 
        subheader={subheader}
      />
      <Box sx={{ p: 3, pb: 1, flexGrow: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }} dir="ltr">
        <Chart
          type="line"
          series={chartSeries}
          options={chartOptions}
          height={320}
        />
      </Box>
    </Card>
  );
}
