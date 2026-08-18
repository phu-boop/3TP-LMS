// @mui
import {
  Box,
  Stack,
  Drawer,
  Button,
  Divider,
  Typography,
  Avatar,
  Grid,
  CircularProgress,
} from '@mui/material';
// components
import Iconify from '../../../components/Iconify';
import Scrollbar from '../../../components/Scrollbar';
import Label from '../../../components/Label';
import Image from '../../../components/Image';
// types
import { Ticket } from '../../../@types/ticket';
// redux
import { useDispatch, useSelector } from '../../../redux/store';
import { fetchTicketById } from '../../../redux/slices/ticket';
import { useEffect, useState } from 'react';
import LightboxModal from '../../../components/LightboxModal';

// ----------------------------------------------------------------------

type Props = {
  ticketId: string | null;
  open: boolean;
  onClose: VoidFunction;
  onReceiveTicket?: (ticketId: string) => void;
  onCompleteTicket?: (ticketId: string) => void;
  onCloseTicket?: (ticketId: string) => void;
};

export default function TicketDrawer({ ticketId, open, onClose, onReceiveTicket, onCompleteTicket, onCloseTicket }: Props) {
  const dispatch = useDispatch();
  const { items: tickets, isLoading } = useSelector((state) => state.ticket);
  const [currentTicket, setCurrentTicket] = useState<Ticket | null>(null);
  const [openLightbox, setOpenLightbox] = useState(false);
  const [selectedImage, setSelectedImage] = useState(0);

  useEffect(() => {
    if (ticketId && open) {
      dispatch(fetchTicketById(ticketId)).then((res: any) => {
        setCurrentTicket(res);
      }).catch(() => {
        // Handle error if ticket not found or API fails
        onClose();
      });
    } else {
      setCurrentTicket(null);
    }
  }, [ticketId, open, dispatch, onClose]);

  if (!open || !currentTicket) return null;

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'NEW': return 'info';
      case 'IN_PROGRESS': return 'warning';
      case 'RESOLVED': return 'success';
      case 'CLOSED': return 'default';
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'NEW': return 'Mới';
      case 'IN_PROGRESS': return 'Đang xử lý';
      case 'RESOLVED': return 'Đã giải quyết';
      case 'CLOSED': return 'Đã đóng';
    }
  };

  const renderContent = (
    <Scrollbar sx={{ flexGrow: 1 }}>
      <Stack spacing={4} sx={{ p: 3 }}>
        <Box>
          <Typography variant="h6" sx={{ mb: 1 }}>{currentTicket.title}</Typography>
          <Stack direction="row" spacing={1} sx={{ mb: 3 }}>
            <Label color={getStatusColor(currentTicket.status)}>{getStatusLabel(currentTicket.status)}</Label>
          </Stack>

          <Typography variant="body2" sx={{ color: 'text.secondary', whiteSpace: 'pre-wrap', mb: 4 }}>
            {currentTicket.description}
          </Typography>

          {(currentTicket.status === 'RESOLVED' || currentTicket.status === 'CLOSED') && currentTicket.resolutionNote && (
            <Box sx={{ mb: 4 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>Ghi chú giải quyết</Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary', whiteSpace: 'pre-wrap' }}>
                {currentTicket.resolutionNote}
              </Typography>
            </Box>
          )}

          <Grid container spacing={3} sx={{ mb: 4 }}>
            <Grid item xs={12}>
              <Typography variant="caption" sx={{ color: 'text.disabled', display: 'block', mb: 1 }}>
                Người báo cáo
              </Typography>
              <Stack direction="row" alignItems="center" spacing={2}>
                <Avatar alt={currentTicket.reporter.name} src={currentTicket.reporter.avatarUrl} />
                <Typography variant="subtitle2">{currentTicket.reporter.name}</Typography>
              </Stack>
            </Grid>
          </Grid>

          {currentTicket.attachments && currentTicket.attachments.length > 0 ? (
            <Box>
              <Typography variant="subtitle2" sx={{ mb: 2 }}>Hình ảnh đính kèm</Typography>
              <Stack spacing={2}>
                {currentTicket.attachments.map((url, index) => (
                  <Box 
                    key={index} 
                    sx={{ borderRadius: 1, overflow: 'hidden', border: '1px solid', borderColor: 'divider', cursor: 'pointer' }}
                    onClick={() => {
                      setSelectedImage(index);
                      setOpenLightbox(true);
                    }}
                  >
                    <Image src={url} alt={`Attachment ${index + 1}`} ratio="16/9" />
                  </Box>
                ))}
              </Stack>
            </Box>
          ) : (
            <Box>
              <Typography variant="subtitle2" sx={{ color: 'text.disabled', mb: 2 }}>Không có hình ảnh đính kèm</Typography>
            </Box>
          )}
        </Box>
      </Stack>
    </Scrollbar>
  );

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      PaperProps={{
        sx: { width: { xs: 1, sm: 480 } },
      }}
    >
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ p: 2.5, borderBottom: '1px solid', borderColor: 'divider' }}>
        <Typography variant="h6"> Chi tiết Ticket </Typography>
        <Button
          color="inherit"
          onClick={onClose}
          startIcon={<Iconify icon="eva:close-fill" />}
        >
          Đóng
        </Button>
      </Stack>

      {isLoading ? (
        <Stack alignItems="center" justifyContent="center" sx={{ height: '100%' }}>
          <CircularProgress />
        </Stack>
      ) : (
        <>
          {renderContent}
          <Divider />
          <Stack direction="row" spacing={2} sx={{ p: 2.5, justifyContent: 'flex-end' }}>
            <Button variant="outlined" color="inherit" onClick={onClose}>
              Quay lại
            </Button>
            {currentTicket.status === 'NEW' && onReceiveTicket && (
              <Button variant="contained" color="info" onClick={() => onReceiveTicket(currentTicket.id)} startIcon={<Iconify icon="pajamas:work-item-task" />}>
                Nhận xử lý
              </Button>
            )}
            {currentTicket.status === 'IN_PROGRESS' && onCompleteTicket && (
              <Button variant="contained" color="warning" onClick={() => onCompleteTicket(currentTicket.id)} startIcon={<Iconify icon="icon-park-outline:done-all" />}>
                Hoàn thành
              </Button>
            )}
          </Stack>
        </>
      )}

      {currentTicket.attachments && (
        <LightboxModal
          images={currentTicket.attachments}
          mainSrc={currentTicket.attachments[selectedImage]}
          photoIndex={selectedImage}
          setPhotoIndex={setSelectedImage}
          isOpen={openLightbox}
          onCloseRequest={() => setOpenLightbox(false)}
        />
      )}
    </Drawer>
  );
}
