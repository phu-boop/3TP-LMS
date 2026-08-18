import { useState } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Typography,
} from '@mui/material';
import { useSnackbar } from 'notistack';

type Props = {
  open: boolean;
  onClose: (resolved?: boolean, resolutionNote?: string) => void;
  ticketId: string | null;
};

export default function ResolveTicketDialog({ open, onClose, ticketId }: Props) {
  const [resolutionNote, setResolutionNote] = useState('');
  const { enqueueSnackbar } = useSnackbar();

  const handleClose = () => {
    setResolutionNote('');
    onClose();
  };

  const handleSubmit = async () => {
    if (!ticketId) return;
    if (!resolutionNote.trim()) {
      enqueueSnackbar('Vui lòng nhập câu trả lời', { variant: 'warning' });
      return;
    }

    onClose(true, resolutionNote);
    setResolutionNote('');
  };

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>Hoàn thành Ticket</DialogTitle>
      <DialogContent>
        <Typography variant="body2" sx={{ mb: 2, mt: 1 }}>
          Vui lòng nhập câu trả lời hoặc cách giải quyết cho lỗi này.
        </Typography>
        <TextField
          fullWidth
          multiline
          rows={4}
          label="Câu trả lời / Cách giải quyết"
          value={resolutionNote}
          onChange={(e) => setResolutionNote(e.target.value)}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} color="inherit">
          Hủy
        </Button>
        <Button onClick={handleSubmit} variant="contained">
          Xác nhận
        </Button>
      </DialogActions>
    </Dialog>
  );
}