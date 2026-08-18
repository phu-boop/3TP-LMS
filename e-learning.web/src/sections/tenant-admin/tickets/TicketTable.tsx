// react
import { useEffect } from 'react';
// @mui
import {
  Box,
  Table,
  Avatar,
  TableRow,
  TableBody,
  TableCell,
  TableHead,
  Typography,
  TableContainer,
  Button,
  Stack,
  alpha,
  CircularProgress,
  IconButton,
  Tooltip,
} from '@mui/material';
// components
import Scrollbar from '../../../components/Scrollbar';
import Label from '../../../components/Label';
import Iconify from '../../../components/Iconify';
// redux
import { useDispatch, useSelector } from '../../../redux/store';
import { fetchTickets } from '../../../redux/slices/ticket';
// types
import { Ticket } from '../../../@types/ticket';

// ----------------------------------------------------------------------

type Props = {
  search?: string;
  status?: string;
  onViewDetails: (ticket: Ticket) => void;
  onReceiveTicket: (ticketId: string) => void;
  onCompleteTicket: (ticketId: string) => void;
  onCloseTicket?: (ticketId: string) => void;
};

export default function TicketTable({ search, status, onViewDetails, onReceiveTicket, onCompleteTicket, onCloseTicket }: Props) {
  const dispatch = useDispatch();
  const { items: tickets, isLoading } = useSelector((state) => state.ticket);

  useEffect(() => {
    const apiStatus = status === 'all' ? '' : status;
    dispatch(fetchTickets(search, apiStatus));
  }, [dispatch, search, status]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'NEW': return 'info';
      case 'IN_PROGRESS': return 'warning';
      case 'RESOLVED': return 'success';
      case 'CLOSED': return 'default';
      default: return 'default';
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'NEW': return 'Mới';
      case 'IN_PROGRESS': return 'Đang xử lý';
      case 'RESOLVED': return 'Đã giải quyết';
      case 'CLOSED': return 'Đã đóng';
      default: return status;
    }
  };

  if (isLoading) {
    return (
      <Stack alignItems="center" justifyContent="center" sx={{ height: 300 }}>
        <CircularProgress />
      </Stack>
    );
  }

  return (
    <TableContainer sx={{ height: '100%', overflow: 'auto' }}>
      <Table stickyHeader>
        <TableHead>
          <TableRow>
            <TableCell>Tiêu đề</TableCell>
            <TableCell>Người báo cáo</TableCell>
            <TableCell>Trạng thái</TableCell>
            <TableCell>Ngày tạo</TableCell>
            <TableCell align="right">Thao tác</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {tickets.map((ticket) => (
            <TableRow key={ticket.id} hover>
              <TableCell sx={{ maxWidth: 220 }}>
                <Typography variant="subtitle2" noWrap sx={{ overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {ticket.title}
                </Typography>
                <Typography variant="body2" sx={{ color: 'text.secondary', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {ticket.description}
                </Typography>
              </TableCell>
              <TableCell>
                <Stack direction="row" alignItems="center" spacing={2}>
                  <Avatar alt={ticket.reporter.name} src={ticket.reporter.avatarUrl} />
                  <Typography variant="subtitle2" noWrap>
                    {ticket.reporter.name}
                  </Typography>
                </Stack>
              </TableCell>
              <TableCell>
                <Label color={getStatusColor(ticket.status)}>
                  {getStatusLabel(ticket.status)}
                </Label>
              </TableCell>
              <TableCell>
                {new Date(ticket.createdAt).toLocaleDateString('vi-VN')}
              </TableCell>
              <TableCell align="right">
                {ticket.status === 'NEW' && (
                  <Tooltip title="Nhận xử lý">
                    <IconButton onClick={() => onReceiveTicket(ticket.id)}>
                      <Iconify icon="pajamas:work-item-task" />
                    </IconButton>
                  </Tooltip>
                )}
                {ticket.status === 'IN_PROGRESS' && (
                  <Tooltip title="Hoàn thành">
                    <IconButton onClick={() => onCompleteTicket(ticket.id)}>
                      <Iconify icon="icon-park-outline:done-all" />
                    </IconButton>
                  </Tooltip>
                )}
                <Tooltip title="Xem chi tiết">
                  <IconButton onClick={() => onViewDetails(ticket)}>
                    <Iconify icon="eva:eye-fill" />
                  </IconButton>
                </Tooltip>
              </TableCell>
            </TableRow>
          ))}

          {tickets.length === 0 && (
            <TableRow>
              <TableCell colSpan={7} align="center" sx={{ py: 5, color: 'text.disabled' }}>
                Chưa có ticket nào
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
