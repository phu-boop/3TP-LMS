import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import axios from '@/utils/axios';
import { API_ENDPOINTS } from '@/constants/apiEndpoints';
import type { Ticket } from '@/@types/ticket';
import { dispatch } from '../store';
import { sanitizeUiMessage } from '@/utils/sanitizeUiMessage';

type TicketState = {
  isLoading: boolean;
  loaded: boolean;
  error: string | null;
  items: Ticket[];
};

const initialState: TicketState = {
  isLoading: false,
  loaded: false,
  error: null,
  items: [],
};

const slice = createSlice({
  name: 'ticket',
  initialState,
  reducers: {
    startLoading(state) {
      state.isLoading = true;
      state.error = null;
    },
    hasError(state, action: PayloadAction<string>) {
      state.isLoading = false;
      state.loaded = true;
      state.error = action.payload;
    },
    loadSuccess(state, action: PayloadAction<Ticket[]>) {
      state.isLoading = false;
      state.loaded = true;
      state.error = null;
      state.items = action.payload;
    },
    upsertSuccess(state, action: PayloadAction<Ticket>) {
      const idx = state.items.findIndex((x) => x.id === action.payload.id);
      if (idx >= 0) state.items[idx] = action.payload;
      else state.items = [action.payload, ...state.items];
      state.isLoading = false;
      state.error = null;
    },
  },
});

export const { startLoading, hasError, loadSuccess, upsertSuccess } = slice.actions;
export default slice.reducer;

export function fetchTickets(search?: string, status?: string) {
  return async () => {
    dispatch(startLoading());
    try {
      const params: Record<string, string> = {};
      if (search) params.search = search;
      if (status && status !== 'all') params.status = status;
      const res = await axios.get(API_ENDPOINTS.ticketsList, { params });
      dispatch(loadSuccess(res.data.items || res.data));
    } catch (e) {
      dispatch(hasError(sanitizeUiMessage((e as Error)?.message || 'Không thể tải danh sách ticket')));
    }
  };
}

export function fetchTicketById(id: string) {
  return async () => {
    dispatch(startLoading());
    try {
      const res = await axios.get(API_ENDPOINTS.ticketById(id));
      dispatch(upsertSuccess(res.data));
      return res.data;
    } catch (e) {
      dispatch(hasError(sanitizeUiMessage((e as Error)?.message || 'Không thể tải chi tiết ticket')));
      throw e;
    }
  };
}
