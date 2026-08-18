import { useState, ReactElement, Component, ErrorInfo, ReactNode, useEffect } from 'react';
// @mui
import { Box, Stack, Container, Typography, TextField, InputAdornment, MenuItem } from '@mui/material';
// layouts
import Layout from '../../layouts';
// guards
import AuthGuard from '../../guards/AuthGuard';
import RoleBasedGuard from '../../guards/RoleBasedGuard';
// components
import Page from '../../components/Page';
import Iconify from '../../components/Iconify';
// sections
import TicketTable from '../../sections/tenant-admin/tickets/TicketTable';
import TicketDrawer from '../../sections/tenant-admin/tickets/TicketDrawer';
import ResolveTicketDialog from '../../sections/tenant-admin/tickets/ResolveTicketDialog';
// types
import { Ticket } from '../../@types/ticket';
import { useSnackbar } from 'notistack';
import axios from '../../utils/axios';
import { API_ENDPOINTS } from '../../constants/apiEndpoints';
import { fetchTickets } from '../../redux/slices/ticket';
import { useDispatch } from '../../redux/store';

// ----------------------------------------------------------------------

class ErrorBoundary extends Component<
  { children: ReactNode; fallback?: ReactNode },
  { hasError: boolean; error: Error | null }
> {
  constructor(props: { children: ReactNode; fallback?: ReactNode }) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('[TicketPage] Error caught:', error, errorInfo);
  }

  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback || (
          <Box sx={{ p: 4, textAlign: 'center' }}>
            <Typography variant="h6" color="error" gutterBottom>
              Đã xảy ra lỗi
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
              {this.state.error?.message || 'Không thể tải trang quản lý ticket.'}
            </Typography>
          </Box>
        )
      );
    }
    return this.props.children;
  }
}

// ----------------------------------------------------------------------

TenantAdminTickets.getLayout = function getLayout(page: ReactElement) {
  return <Layout variant="dashboard">{page}</Layout>;
};

// ----------------------------------------------------------------------

export default function TenantAdminTickets() {
  const dispatch = useDispatch();
  const [selectedTicketId, setSelectedTicketId] = useState<string | null>(null);
  const [searchText, setSearchText] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [status, setStatus] = useState('all');
  const [openResolveDialog, setOpenResolveDialog] = useState(false);
  const [ticketToResolveId, setTicketToResolveId] = useState<string | null>(null);
  const { enqueueSnackbar } = useSnackbar();

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchText);
    }, 500);
    return () => clearTimeout(handler);
  }, [searchText]);

  const handleReceiveTicket = async (ticketId: string) => {
    try {
      await axios.patch(API_ENDPOINTS.updateTicketStatus(ticketId), { status: 'IN_PROGRESS' });
      enqueueSnackbar('Đã nhận ticket', { variant: 'success' });
      dispatch(fetchTickets(debouncedSearch, status));
    } catch (e) {
      enqueueSnackbar('Không thể nhận ticket', { variant: 'error' });
    }
  };

  const handleCloseResolveDialog = async (resolved?: boolean, resolutionNote?: string) => {
    setOpenResolveDialog(false);
    setTicketToResolveId(null);
    if (resolved && ticketToResolveId) {
      try {
      await axios.patch(API_ENDPOINTS.updateTicketStatus(ticketToResolveId), { status: 'RESOLVED', note: resolutionNote });
        enqueueSnackbar('Đã hoàn thành ticket', { variant: 'success' });
        dispatch(fetchTickets(debouncedSearch, status));
      } catch (e) {
        enqueueSnackbar('Không thể hoàn thành ticket', { variant: 'error' });
      }
    }
  };

  return (
    <AuthGuard>
      <RoleBasedGuard roles={['SUPER_ADMIN', 'LMS_ADMIN', 'TENANT_ADMIN']} hasContent>
        <Page title="Quản lý Hỗ trợ">
          <Container maxWidth={false} sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              alignItems={{ sm: 'center' }}
              justifyContent="space-between"
              sx={{ mb: 3 }}
            >
              <Box>
                <Typography variant="h4" gutterBottom>
                  Quản lý Hỗ trợ
                </Typography>
                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                  Quản lý và xử lý các yêu cầu hỗ trợ từ người dùng
                </Typography>
              </Box>

              <Stack direction="row" spacing={2} sx={{ mt: { xs: 2, sm: 0 } }}>
                <TextField
                  select
                  size="small"
                  value={status}
                  onChange={(e) => setStatus(e.target.value)}
                  sx={{ width: 180 }}
                >
                  <MenuItem value="all">Tất cả</MenuItem>
                  <MenuItem value="NEW">Mới</MenuItem>
                  <MenuItem value="IN_PROGRESS">Đang xử lý</MenuItem>
                  <MenuItem value="RESOLVED">Đã giải quyết</MenuItem>
                  <MenuItem value="CLOSED">Đã đóng</MenuItem>
                </TextField>
                <TextField
                  size="small"
                  placeholder="Tìm kiếm ticket..."
                  value={searchText}
                  onChange={(e) => setSearchText(e.target.value)}
                  InputProps={{
                    startAdornment: (
                      <InputAdornment position="start">
                        <Iconify icon="eva:search-fill" sx={{ color: 'text.disabled', width: 20, height: 20 }} />
                      </InputAdornment>
                    ),
                  }}
                  sx={{ width: 250 }}
                />
              </Stack>
            </Stack>

            <Box sx={{ flexGrow: 1, bgcolor: 'background.paper', borderRadius: 2, border: '1px solid', borderColor: 'divider', overflow: 'hidden' }}>
              <ErrorBoundary>
                <TicketTable 
                  search={debouncedSearch}
                  status={status}
                  onViewDetails={(ticket) => setSelectedTicketId(ticket.id)} 
                  onReceiveTicket={handleReceiveTicket}
                  onCompleteTicket={(ticketId) => {
                    setTicketToResolveId(ticketId);
                    setOpenResolveDialog(true);
                  }}
                />
              </ErrorBoundary>
            </Box>
          </Container>

          <TicketDrawer
            ticketId={selectedTicketId}
            open={!!selectedTicketId}
            onClose={() => setSelectedTicketId(null)}
            onReceiveTicket={handleReceiveTicket}
            onCompleteTicket={(ticketId) => {
              setTicketToResolveId(ticketId);
              setOpenResolveDialog(true);
            }}
          />

          <ResolveTicketDialog
            open={openResolveDialog}
            onClose={handleCloseResolveDialog}
            ticketId={ticketToResolveId}
          />
        </Page>
      </RoleBasedGuard>
    </AuthGuard>
  );
}
