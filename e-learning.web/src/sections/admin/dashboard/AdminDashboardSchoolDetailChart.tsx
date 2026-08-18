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
  avgPerHour: number;
  avgPerDay: number;
  avgPerMonth: number;
};

export default function AdminDashboardSchoolDetailChart({ title, subheader, avgPerHour, avgPerDay, avgPerMonth }: Props) {
  const chartOptions = merge(BaseOptionChart(), {
    plotOptions: { bar: { columnWidth: '40%', borderRadius: 4 } },
    labels: ['Từng giờ', 'Từng ngày', 'Từng tháng'],
    xaxis: { type: 'category' },
    tooltip: {
      y: {
        formatter: (val: number) => `${val.toFixed(2)} người học`,
      },
    },
    colors: ['#ffc107'],
  });

  const chartData = [
    {
      name: 'Lượng người học trung bình',
      data: [avgPerHour, avgPerDay, avgPerMonth],
    },
  ];

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <CardHeader title={title} subheader={subheader} />
      <Box sx={{ p: 3, pb: 1, flexGrow: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }} dir="ltr">
        <Chart
          type="bar"
          series={chartData}
          options={chartOptions}
          height={320}
        />
      </Box>
    </Card>
  );
}
