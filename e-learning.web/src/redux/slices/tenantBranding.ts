import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { ResolveTenantResponse } from '@/types/identity';

type ResolveTenantState = {
  resolve: ResolveTenantResponse | null;
};

const initialState: ResolveTenantState = {
  resolve: null,
};

const slice = createSlice({
  // Change to use Resolve tenant to get tenant name
  name: 'resolveTenant',
  initialState,
  reducers: {
    setBranding(state, action: PayloadAction<ResolveTenantResponse | null>) {
      state.resolve = action.payload;
    },
    clearBranding(state) {
      state.resolve = null;
    },
  },
});

export const { setBranding, clearBranding } = slice.actions;
export default slice.reducer;
