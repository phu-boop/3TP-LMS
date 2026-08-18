import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  IconButton,
  Tooltip,
  TablePagination,
} from '@mui/material';
import { useSnackbar } from 'notistack';
import Iconify from '../../../components/Iconify';
import Scrollbar from '../../../components/Scrollbar';
import Label from '../../../components/Label';
import { sanitizeUiMessage } from '../../../utils/sanitizeUiMessage';
import type { ProgramTenant, ProgramTenantStatus } from '../../../@types/programTenant';
import { useDispatch, useSelector } from '../../../redux/store';
import {
  fetchProgramTenants,
  toggleProgramTenantStatus,
  upsertProgramTenant,
} from '../../../redux/slices/programTenant';

type FormState = {
  name: string;
  code: string;
  subdomain: string;
  status: ProgramTenantStatus;
  logoUrl: string;
  color: string;
};

const DEFAULT_PROGRAM_COLOR = '#1976d2';

const emptyForm: FormState = {
  name: '',
  code: '',
  subdomain: '',
  status: 'active',
  logoUrl: '',
  color: DEFAULT_PROGRAM_COLOR,
};

function cleanSubdomainInput(raw: string): string {
  return raw.toLowerCase().replace(/[^a-z0-9-]/g, '');
}

export default function ProgramTenantManagement() {
  const { enqueueSnackbar } = useSnackbar();
  const dispatch = useDispatch();
  const { items, isLoading, loaded, error } = useSelector((state) => state.programTenant);

  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | ProgramTenantStatus>('all');
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);

  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  useEffect(() => {
    setPage(0);
  }, [search, statusFilter]);

  useEffect(() => {
    dispatch(fetchProgramTenants());
  }, [dispatch]);

  useEffect(() => {
    if (!error) return;
    enqueueSnackbar(sanitizeUiMessage(error), { variant: 'error' });
  }, [error, enqueueSnackbar]);

  const filteredItems = useMemo(() => {
    return items.filter((item) => {
      if (statusFilter !== 'all' && item.status !== statusFilter) return false;
      const q = search.trim().toLowerCase();
      if (!q) return true;
      return (
        item.name.toLowerCase().includes(q) ||
        item.code.toLowerCase().includes(q) ||
        item.subdomain.toLowerCase().includes(q)
      );
    });
  }, [items, search, statusFilter]);

  const dataInPage = useMemo(() => {
    return filteredItems.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
  }, [filteredItems, page, rowsPerPage]);

  const openCreate = () => {
    setEditingId(null);
    setForm({ ...emptyForm });
    setDialogOpen(true);
  };

  const openEdit = (item: ProgramTenant) => {
    setEditingId(item.id);
    setForm({
      name: item.name,
      code: item.code,
      subdomain: item.subdomain,
      status: item.status,
      logoUrl: item.logoUrl || '',
      color: item.color || DEFAULT_PROGRAM_COLOR,
    });
    setDialogOpen(true);
  };

  const onSave = async () => {
    const name = form.name.trim();
    const code = form.code.trim().toUpperCase();
    const subdomain = form.subdomain.trim().replace(/^-+|-+$/g, '');
    if (!name) {
      enqueueSnackbar('Tên chương trình là bắt buộc', { variant: 'warning' });
      return;
    }
    if (!code) {
      enqueueSnackbar('Mã chương trình là bắt buộc', { variant: 'warning' });
      return;
    }
    if (!subdomain) {
      enqueueSnackbar('Subdomain là bắt buộc', { variant: 'warning' });
      return;
    }

    try {
      await dispatch(
        upsertProgramTenant({
          id: editingId || undefined,
          name,
          code,
          subdomain,
          status: form.status,
          logoUrl: form.logoUrl.trim() || null,
          color: form.color.trim() || DEFAULT_PROGRAM_COLOR,
        })
      );
      enqueueSnackbar(editingId ? 'Đã cập nhật chương trình' : 'Đã tạo chương trình', {
        variant: 'success',
      });
      setDialogOpen(false);
    } catch (e) {
      // Error already handled in slice
    }
  };

  return (
    <Stack spacing={{ xs: 2, md: 3 }}>
      <Card sx={{ p: { xs: 1.5, md: 2.5 } }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} justifyContent="space-between">
          <Typography variant="h6">Khởi tạo chương trình</Typography>
          <Button variant="contained" startIcon={<Iconify icon="eva:plus-fill" />} onClick={openCreate}>
            Tạo chương trình
          </Button>
        </Stack>
        {!loaded && isLoading && (
          <Alert sx={{ mt: 2 }} severity="info">
            Đang tải danh sách chương trình...
          </Alert>
        )}
      </Card>

      <Card sx={{ p: { xs: 1.5, md: 2.5 } }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
          <TextField
            fullWidth
            label="Tìm kiếm"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tên, mã, subdomain..."
          />
          <FormControl sx={{ minWidth: { xs: '100%', md: 220 } }}>
            <InputLabel id="tenant-status-filter">Trạng thái</InputLabel>
            <Select
              labelId="tenant-status-filter"
              label="Trạng thái"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as 'all' | ProgramTenantStatus)}
            >
              <MenuItem value="all">Tất cả</MenuItem>
              <MenuItem value="active">Hoạt động</MenuItem>
              <MenuItem value="inactive">Ngưng</MenuItem>
            </Select>
          </FormControl>
        </Stack>
      </Card>

      <Card>
        <TableContainer>
          <Scrollbar>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Logo</TableCell>
                  <TableCell>Tên chương trình</TableCell>
                  <TableCell>Mã</TableCell>
                  <TableCell>Tên miền phụ</TableCell>
                  <TableCell>Màu</TableCell>
                  <TableCell>Trạng thái</TableCell>
                  <TableCell align="right">Hành động</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {dataInPage.map((item) => (
                  <TableRow key={item.id} hover>
                    <TableCell>
                      {item.logoUrl ? (
                        <Box
                          component="img"
                          src={item.logoUrl}
                          alt={item.name}
                          sx={{ width: 40, height: 40, objectFit: 'cover', borderRadius: 1, border: '1px solid', borderColor: 'divider' }}
                        />
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          —
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>{item.name}</TableCell>
                    <TableCell>{item.code}</TableCell>
                    <TableCell>{item.subdomain}</TableCell>
                    <TableCell>
                      {item.color ? (
                        <Box
                          sx={{ width: 20, height: 20, borderRadius: '50%', bgcolor: item.color, border: '1px solid', borderColor: 'divider' }}
                        />
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          —
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>
                      {item.status === 'active' ? <Label color="success">Hoạt động</Label> : <Label color="warning">Ngưng</Label>}
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={1} justifyContent="flex-end">
                        <Tooltip title="Sửa">
                          <IconButton size="small" color="primary" onClick={() => openEdit(item)}>
                            <Iconify icon="eva:edit-2-fill" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title={item.status === 'active' ? 'Ngừng hoạt động' : 'Kích hoạt'}>
                          <IconButton size="small" color={item.status === 'active' ? 'warning' : 'success'} onClick={() => dispatch(toggleProgramTenantStatus(item.id))}>
                            <Iconify icon={item.status === 'active' ? 'eva:slash-fill' : 'eva:checkmark-circle-2-fill'} />
                          </IconButton>
                        </Tooltip>
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
                {!filteredItems.length && (
                  <TableRow>
                    <TableCell colSpan={7}>
                      <Typography align="center" sx={{ py: 3 }} color="text.secondary">
                        Chưa có chương trình
                      </Typography>
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </Scrollbar>
        </TableContainer>
        <TablePagination
          rowsPerPageOptions={[5, 10, 25]}
          component="div"
          count={filteredItems.length}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={(e, newPage) => setPage(newPage)}
          onRowsPerPageChange={(e) => {
            setRowsPerPage(parseInt(e.target.value, 10));
            setPage(0);
          }}
          labelRowsPerPage="Số hàng mỗi trang:"
        />
      </Card>

      <Dialog fullWidth maxWidth="sm" open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle sx={{ pb: 2 }}>{editingId ? 'Cập nhật chương trình' : 'Tạo chương trình'}</DialogTitle>
        <DialogContent sx={{ py: 3 }}>
          <Stack spacing={3}>
            <TextField
              fullWidth
              label="Tên chương trình"
              value={form.name}
              onChange={(e) => setForm((prev) => ({ ...prev, name: e.target.value }))}
            />
            <TextField
              fullWidth
              label="Mã chương trình"
              value={form.code}
              onChange={(e) => setForm((prev) => ({ ...prev, code: e.target.value.toUpperCase() }))}
            />
            <Stack spacing={0.5}>
              <TextField
                fullWidth
                label="Tên miền phụ"
                value={form.subdomain}
                onChange={(e) => setForm((prev) => ({ ...prev, subdomain: cleanSubdomainInput(e.target.value) }))}
              />
              <Typography variant="caption" sx={{ color: 'text.secondary', ml: 1 }}>
                Chỉ gồm chữ thường, số và dấu gạch ngang
              </Typography>
            </Stack>
            <Stack spacing={1}>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Logo chương trình
              </Typography>
              <Stack direction="row" alignItems="center" spacing={2}>
                <Box
                  component="img"
                  src={form.logoUrl || undefined}
                  alt={form.name || 'Logo chương trình'}
                  sx={{ width: 64, height: 64, objectFit: 'cover', borderRadius: 1, border: '1px solid', borderColor: 'divider', bgcolor: 'background.neutral' }}
                />
                <Stack spacing={0.5}>
                  <Button variant="outlined" component="label" size="small" startIcon={<Iconify icon="eva:image-fill" />} sx={{ width: 'fit-content' }}>
                    Chọn ảnh
                    <input
                      type="file"
                      accept="image/*"
                      hidden
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        if (!file) return;
                        const reader = new FileReader();
                        reader.onload = (event) => {
                          if (event.target?.result) {
                            setForm((prev) => ({ ...prev, logoUrl: event.target?.result as string }));
                          }
                        };
                        reader.readAsDataURL(file);
                      }}
                    />
                  </Button>
                  {form.logoUrl && (
                    <Button
                      variant="text"
                      color="error"
                      size="small"
                      onClick={() => setForm((prev) => ({ ...prev, logoUrl: '' }))}
                      sx={{ width: 'fit-content', p: 0, minWidth: 'auto' }}
                    >
                      Xóa ảnh
                    </Button>
                  )}
                </Stack>
              </Stack>
            </Stack>
            <Stack spacing={1}>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Màu chương trình
              </Typography>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ xs: 'flex-start', sm: 'center' }}>
                <Box
                  component="input"
                  type="color"
                  value={form.color}
                  onChange={(e) => setForm((prev) => ({ ...prev, color: e.target.value }))}
                  sx={{ width: 48, height: 48, p: 0, border: 'none', background: 'transparent', cursor: 'pointer' }}
                />
                <TextField
                  fullWidth
                  label="Mã màu"
                  value={form.color}
                  onChange={(e) => setForm((prev) => ({ ...prev, color: e.target.value }))}
                />
              </Stack>
            </Stack>
            <FormControl fullWidth disabled={!!editingId}>
              <InputLabel id="tenant-status-form">Trạng thái</InputLabel>
              <Select
                labelId="tenant-status-form"
                label="Trạng thái"
                value={form.status}
                onChange={(e) => setForm((prev) => ({ ...prev, status: e.target.value as ProgramTenantStatus }))}
              >
                <MenuItem value="active">Hoạt động</MenuItem>
                <MenuItem value="inactive">Ngưng</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button color="inherit" onClick={() => setDialogOpen(false)}>
            Hủy
          </Button>
          <Button variant="contained" onClick={onSave} sx={{ minWidth: 100 }}>
            {editingId ? 'Lưu' : 'Tạo'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
